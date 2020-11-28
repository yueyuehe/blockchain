using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QMBlockSDK.CC;
using QMBlockSDK.Helper;
using QMBlockSDK.Ledger;
using QMBlockSDK.TX;
using QMBlockUtils;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.States;
using QMRaftCore.Data.Imp;
using QMRaftCore.Data.Model;
using QMRaftCore.FiniteStateMachine;
using QMRaftCore.Infrastructure;
using QMRaftCore.Msg.Model;
using QMRaftCore.QMProvider;
using QMRaftCore.QMProvider.Imp;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace QMRaftCore.Concensus.Node
{
    public class Node : INode
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<Node> _logger;
        private readonly IChainCodeExecutor _chainCodeExecutor;
        private readonly IConfigProvider _configProvider;
        private readonly StateProvider _stateprovider;
        private readonly string _channelId;
        private readonly DataManager _dataManager;
        private readonly IMemoryCache _memoryCache;

        private readonly MQSetting _mq;

        public Node(
            string channelId,
            ILoggerFactory loggerFactory,
            IAssemblyProvider assemblyProvider,
            IIdentityProvider identityProvider,
            DataManager dataManager,
            MQSetting mQSetting,
            IHttpClientFactory clientFactory,
            IMemoryCache memoryCache
            )
        {
            _memoryCache = memoryCache;
            _mq = mQSetting;
            _channelId = channelId;
            _dataManager = dataManager;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<Node>();

            //链码执行器
            _chainCodeExecutor = new ChainCodeExecutor(assemblyProvider, identityProvider, _dataManager);
            //节点通讯提供者
            //var peerProvider = new GrpcPeerProvider(loggerFactory, dataManager);
            var peerProvider = new PeersProvider(loggerFactory, dataManager, clientFactory);
            //背书策略提供者
            var policeProvider = new PolicyProvider(loggerFactory, identityProvider, peerProvider, dataManager);
            //配置提供
            _configProvider = new ConfigProvider(assemblyProvider, policeProvider, identityProvider, peerProvider, _mq);
            //交易池
            TxPool = new TxPool(loggerFactory, _configProvider, _dataManager, this, _memoryCache);
            //节点状态
            _stateprovider = new StateProvider(_configProvider, this, _loggerFactory, _dataManager);
        }

        public IState State { get; private set; }

        public TxPool TxPool { get; }

        #region 节点状态变换

        public void Start(NodeId id)
        {
            var currentstate = new CurrentState(id.Id, 0, default(string), 0, 0, default(string));
            BecomeFollower(currentstate);
        }

        /// <summary>
        /// 成为候选人
        /// </summary>
        /// <param name="state"></param>
        public void BecomeCandidate(CurrentState state)
        {
            TxPool.StopCacheTx();
            _logger.LogInformation($"{state.Id} became candidate");
            Candidate candidata = null;
            candidata = _stateprovider.GetCandidate(state);// new Candidate(state, _fsm, _getPeers(state), _log, _random, this, _settings, _rules, _loggerFactory);
            State = candidata;
            //开始选举
            candidata.BeginElectionAsync().Wait();
        }

        /// <summary>
        /// 变成leader节点
        /// </summary>
        /// <param name="state"></param>
        public void BecomeLeader(CurrentState state)
        {
            TxPool.StartCacheTx();
            _logger.LogInformation($"{state.Id} became leader");
            State = _stateprovider.GetLeader(state);
        }

        /// <summary>
        /// 变成随从
        /// </summary>
        /// <param name="state"></param>
        public void BecomeFollower(CurrentState state)
        {
            TxPool.StopCacheTx();
            _logger.LogInformation($"{state.Id} became follower");
            State = _stateprovider.GetFollower(state);
        }

        #endregion

        #region raft 网络

        public async Task<AppendEntriesResponse> Handle(AppendEntries appendEntries)
        {
            var response = await State.Handle(appendEntries);
            if (response.BlockEntity != null)
            {
                _logger.LogInformation($"{State.GetType().Name} id: {State.CurrentState.Id} responded to appendentries with success: {response.Success} and term: {response.Term}");
            }
            return response;
        }
        public async Task<RequestVoteResponse> Handle(RequestVote requestVote)
        {
            return await State.Handle(requestVote);
        }
        public async Task<Response<T>> Accept<T>(T command) where T : ICommand
        {
            return await State.Accept(command);
        }
        public async Task<HeartBeatResponse> Handle(HeartBeat request)
        {
            return await State.Handle(request);
        }

        #endregion

        public void Stop()
        {
            State.Stop();
            State = null;
        }

        #region 交易相关

        /// <summary>
        /// 如果是查询交易 直接返回本地查询结果
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<TxResponse> TransactionCommit(TxRequest request)
        {
            ///如果是查询获取本地的结果就行并且不用背书
            if (request.Data.Type == TxType.Query)
            {
                var rs = await ChainCodeSubmit(request);
                var response = new TxResponse();
                response.Msg = rs.Message;
                response.Status = rs.StatusCode == StatusCode.Successful;
                if (!response.Status)
                {
                    response.Msg += "||" + rs.StatusCode.ToString();
                }
                response.Data = rs.Data;
                return response;
            }
            //转发交易
            return await State.TranspondTx(request);
        }

        public async Task<TxResponse> Transaction(TxRequest request)
        {
            _logger.LogInformation($"节点转发交易 通道{_channelId}");
            return await State.Transaction(request);
        }

        public async Task<EndorseResponse> Endorse(TxRequest request)
        {
            var response = await State.Endorse(request);
            return response;
        }

        public async Task<ChainCodeInvokeResponse> ChainCodeSubmit(TxRequest request)
        {
            return await _chainCodeExecutor.Submit(request);
        }

        #endregion

        public string GetChannelId()
        {
            return this._channelId;
        }

        #region 在本地新增通道

        /// <summary>
        /// 创建新通道 默认只加载本地的节点
        /// </summary>
        /// <param name="channelName"></param>
        public async Task<TxResponse> CreateNewChannel(string channelId)
        {
            //创建交易请求
            var txHeader = new TxHeader();
            txHeader.ChannelId = channelId;
            txHeader.Type = TxType.Invoke;
            txHeader.ChaincodeName = ConfigKey.SysNetConfigChaincode;
            txHeader.FuncName = ConfigKey.InitChannelFunc;
            txHeader.Args = new string[] { channelId };

            var tx = ModelHelper.ToTxRequest(txHeader);

            EndorseResponse response = new EndorseResponse();
            var result = ChainCodeSubmit(tx).Result;
            if (result.StatusCode != StatusCode.Successful)
            {
                return new TxResponse() { Status = false, Msg = result.Message };
            }
            response.TxReadWriteSet = result.TxReadWriteSet;
            response.Status = true;
            response.Msg = "背书完成";
            response.Endorsement.Identity = _configProvider.GetPublicIndentity();
            var str = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            var privatekey = _configProvider.GetPrivateKey();
            //节点对背书数据签名
            response.Endorsement.Signature = RSAHelper.SignData(privatekey, response);

            Envelope envelope = new Envelope();
            envelope.TxReqeust = tx;
            envelope.Endorsements.Add(response.Endorsement);
            envelope.PayloadReponse = new ProposaResponsePayload();
            envelope.PayloadReponse.Message = response.Msg;
            envelope.PayloadReponse.Status = response.Status;
            envelope.PayloadReponse.TxReadWriteSet = response.TxReadWriteSet;

            var block = new Block();
            block.Header.ChannelId = channelId;
            block.Header.Number = 0;
            block.Header.Term = 0;
            block.Header.Timestamp = DateTime.Now.Ticks;
            block.Data.Envelopes.Add(envelope);
            block.Signer.Identity = _configProvider.GetPublicIndentity();
            //计算hash·
            block.Header.DataHash = RSAHelper.GenerateMD5(Newtonsoft.Json.JsonConvert.SerializeObject(block));
            //对整个区块进行签名
            block.Signer.Signature = RSAHelper.SignData(_configProvider.GetPrivateKey(), block);
            //上链
            await _dataManager.PutOnChainBlockAsync(block);

            return new TxResponse() { Status = true };
        }

        #endregion

        #region 区块添加

        /// <summary>
        /// 节点将区块扩散
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public async Task<HandOutResponse> Handle(HandOutRequest request)
        {
            var rs = await State.BlockHandOut(request);
            return rs;
        }

        public async Task<HandOutResponse> BlockHandOut(Block block)
        {
            //如果不是leader节点不能分发区块
            var request = new HandOutRequest();
            request.Block = block;
            request.ChannelId = GetChannelId();
            request.Type = HandOutType.CheckSave;
            var rs = await Handle(request);
            return rs;
        }

        #endregion


    }
}