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

            //����ִ����
            _chainCodeExecutor = new ChainCodeExecutor(assemblyProvider, identityProvider, _dataManager);
            //�ڵ�ͨѶ�ṩ��
            //var peerProvider = new GrpcPeerProvider(loggerFactory, dataManager);
            var peerProvider = new PeersProvider(loggerFactory, dataManager, clientFactory);
            //��������ṩ��
            var policeProvider = new PolicyProvider(loggerFactory, identityProvider, peerProvider, dataManager);
            //�����ṩ
            _configProvider = new ConfigProvider(assemblyProvider, policeProvider, identityProvider, peerProvider, _mq);
            //���׳�
            TxPool = new TxPool(loggerFactory, _configProvider, _dataManager, this, _memoryCache);
            //�ڵ�״̬
            _stateprovider = new StateProvider(_configProvider, this, _loggerFactory, _dataManager);
        }

        public IState State { get; private set; }

        public TxPool TxPool { get; }

        #region �ڵ�״̬�任

        public void Start(NodeId id)
        {
            var currentstate = new CurrentState(id.Id, 0, default(string), 0, 0, default(string));
            BecomeFollower(currentstate);
        }

        /// <summary>
        /// ��Ϊ��ѡ��
        /// </summary>
        /// <param name="state"></param>
        public void BecomeCandidate(CurrentState state)
        {
            TxPool.StopCacheTx();
            _logger.LogInformation($"{state.Id} became candidate");
            Candidate candidata = null;
            candidata = _stateprovider.GetCandidate(state);// new Candidate(state, _fsm, _getPeers(state), _log, _random, this, _settings, _rules, _loggerFactory);
            State = candidata;
            //��ʼѡ��
            candidata.BeginElectionAsync().Wait();
        }

        /// <summary>
        /// ���leader�ڵ�
        /// </summary>
        /// <param name="state"></param>
        public void BecomeLeader(CurrentState state)
        {
            TxPool.StartCacheTx();
            _logger.LogInformation($"{state.Id} became leader");
            State = _stateprovider.GetLeader(state);
        }

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="state"></param>
        public void BecomeFollower(CurrentState state)
        {
            TxPool.StopCacheTx();
            _logger.LogInformation($"{state.Id} became follower");
            State = _stateprovider.GetFollower(state);
        }

        #endregion

        #region raft ����

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

        #region �������

        /// <summary>
        /// ����ǲ�ѯ���� ֱ�ӷ��ر��ز�ѯ���
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<TxResponse> TransactionCommit(TxRequest request)
        {
            ///����ǲ�ѯ��ȡ���صĽ�����в��Ҳ��ñ���
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
            //ת������
            return await State.TranspondTx(request);
        }

        public async Task<TxResponse> Transaction(TxRequest request)
        {
            _logger.LogInformation($"�ڵ�ת������ ͨ��{_channelId}");
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

        #region �ڱ�������ͨ��

        /// <summary>
        /// ������ͨ�� Ĭ��ֻ���ر��صĽڵ�
        /// </summary>
        /// <param name="channelName"></param>
        public async Task<TxResponse> CreateNewChannel(string channelId)
        {
            //������������
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
            response.Msg = "�������";
            response.Endorsement.Identity = _configProvider.GetPublicIndentity();
            var str = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            var privatekey = _configProvider.GetPrivateKey();
            //�ڵ�Ա�������ǩ��
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
            //����hash��
            block.Header.DataHash = RSAHelper.GenerateMD5(Newtonsoft.Json.JsonConvert.SerializeObject(block));
            //�������������ǩ��
            block.Signer.Signature = RSAHelper.SignData(_configProvider.GetPrivateKey(), block);
            //����
            await _dataManager.PutOnChainBlockAsync(block);

            return new TxResponse() { Status = true };
        }

        #endregion

        #region �������

        /// <summary>
        /// �ڵ㽫������ɢ
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
            //�������leader�ڵ㲻�ַܷ�����
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