using Microsoft.Extensions.Logging;
using QMBlockSDK.Ledger;
using QMBlockSDK.TX;
using QMBlockUtils;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.Node;
using QMRaftCore.Data;
using QMRaftCore.FiniteStateMachine;
using QMRaftCore.Infrastructure;
using QMRaftCore.QMProvider;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMRaftCore.Concensus.States
{
    public sealed class Follower : IState
    {
        private Timer _electionTimer;
        private readonly IConfigProvider _configProviders;
        private readonly INode _node;
        private readonly IBlockDataManager _blockDataManager;
        private readonly ILogger<Follower> _logger;

        public Follower(
            IConfigProvider config,
            INode node,
            ILoggerFactory loggerFactory,
            IBlockDataManager dataManager)
        {
            _configProviders = config;
            _blockDataManager = dataManager;
            _logger = loggerFactory.CreateLogger<Follower>();
            _node = node;
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        /// <param name="state"></param>
        internal void Reset(CurrentState state)
        {
            this.CurrentState = state;
            ResetElectionTimer();
        }

        public CurrentState CurrentState { get; private set; }

        public void Stop()
        {
            if (_electionTimer != null)
            {
                _electionTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        /// <summary>
        /// 重置选举计时器
        /// </summary>
        private void ResetElectionTimer()
        {
            var timeout = _configProviders.GetToCandidateTimeOut();
            if (_electionTimer == null)
            {
                _electionTimer = new Timer(x =>
                {
                    try
                    {
                        //停止计时
                        _electionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        BecomeCandidate();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                }, null, Convert.ToInt32(timeout.TotalMilliseconds), Convert.ToInt32(timeout.TotalMilliseconds));
            }
            else
            {
                _electionTimer.Change(Convert.ToInt32(timeout.TotalMilliseconds), Convert.ToInt32(timeout.TotalMilliseconds));
            }
        }

        private void BecomeCandidate()
        {
            Stop();
            _node.BecomeCandidate(CurrentState);
        }

        #region 状态请求方法
        public async Task<RequestVoteResponse> Handle(RequestVote requestVote)
        {
            //重置时间
            try
            {
                //判断任期 任期大于当前任期
                if (requestVote.Term <= CurrentState.CurrentTerm || !string.IsNullOrEmpty(CurrentState.VotedFor))
                {
                    return new RequestVoteResponse(false, CurrentState.CurrentTerm);
                }
                //判断日志 当前日志存在
                var lastLog = _blockDataManager.GetLastBlockEntity(_node.GetChannelId());
                if (lastLog != null)
                {
                    //本地日志大于投票日志 返回false
                    if (lastLog.Header.Number > requestVote.LastLogHeight || lastLog.Header.Term >= requestVote.Term)
                    {
                        return new RequestVoteResponse(false, CurrentState.CurrentTerm);
                    }
                    else
                    {
                        CurrentState.VotedFor = requestVote.CandidateId;
                        return new RequestVoteResponse(true, CurrentState.CurrentTerm);
                    }
                }
                //本地没有日志同意
                else
                {
                    CurrentState.VotedFor = requestVote.CandidateId;
                    return new RequestVoteResponse(true, CurrentState.CurrentTerm);
                }
            }
            finally
            {
                //表示成功接受到了投票信息 重新计时
                ResetElectionTimer();
            }
        }

        public async Task<HeartBeatResponse> Handle(HeartBeat heartBeat)
        {
            try
            {
                ///接受到了消息  停止计时 等待处理
                _electionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _logger.LogInformation($"接收到来自{heartBeat.LeaderId} 任期 {heartBeat.Term} 的心跳");
                var rs = new HeartBeatResponse();
                ///获取当前节点最新的区块
                var currentBlock = _blockDataManager.GetLastBlockEntity(_node.GetChannelId());
                //如果区块为0则初始化区块对象
                if (currentBlock == null)
                {
                    currentBlock = new QMBlockSDK.MongoModel.MongoBlock();
                    currentBlock.Header.Number = -1;
                    currentBlock.Header.Term = -1;
                }
                rs.Term = CurrentState.CurrentTerm;
                rs.LeaderId = CurrentState.LeaderId;
                rs.Height = currentBlock.Header.Number;
                rs.BlockCurrentHash = currentBlock.Header.DataHash;
                rs.BlockPreviousHash = currentBlock.Header.PreviousHash;
                rs.LeaderId = CurrentState.LeaderId;
                rs.Success = false;
                //当前任期大于leader任期 返回当前任期，leaderID和最新日志信息
                if (CurrentState.CurrentTerm > heartBeat.Term)
                {
                    return rs;
                }
                //任期小于等于leader任期，
                //则返回最新日志,改变自身状态(觉得自身状态和自己最新的日志的任期一致)
                else
                {
                    CurrentState.LeaderId = heartBeat.LeaderId;
                    CurrentState.CurrentTerm = heartBeat.Term;
                    rs.LeaderId = heartBeat.LeaderId;
                    rs.Success = true;
                    _logger.LogInformation("请求合法，刷新选举计时器");
                    //同步指定高度的区块
                    await SynchronizedBlockAsync(heartBeat.Height);
                    //接收成功的信息才刷新计时器
                    //ResetElectionTimer();
                    return rs;
                }
            }
            finally
            {
                //接收成功的信息才刷新计时器
                ResetElectionTimer();
            }

        }

        public Task<AppendEntriesResponse> Handle(AppendEntries appendEntries)
        {
            //follower不对外输出日志
            return Task.FromResult(new AppendEntriesResponse()
            {
                Success = false,
                Term = CurrentState.CurrentTerm
            });
        }

        public async Task<Response<T>> Accept<T>(T command) where T : ICommand
        {
            //获取leader节点
            var leader = _configProviders.GetPeer(_node.GetChannelId(), CurrentState.LeaderId);
            if (leader != null)
            {
                _logger.LogInformation($"follower id: {CurrentState.Id} forward to leader id: {leader.Id}");
                return await leader.Request(command);
            }
            return new ErrorResponse<T>("Please retry command later. Unable to find leader.", command);
        }

        #endregion

        /// <summary>
        /// 接受来自leader节点的请求时，会接受 同步区块
        /// </summary>
        /// <returns></returns>
        private async Task SynchronizedBlockAsync(long number)
        {
            var lastBlock = _blockDataManager.GetLastBlockEntity(_node.GetChannelId());
            if (lastBlock == null)
            {
                lastBlock = new QMBlockSDK.MongoModel.MongoBlock();
                lastBlock.Header.Number = -1;
                lastBlock.Header.Term = -1;
            }
            if (lastBlock.Header.Number >= number)
            {
                return;
            }
            var peer = _configProviders.GetPeer(_node.GetChannelId(), CurrentState.LeaderId);
            //当前block的信息
            var append = new AppendEntries();
            append.ChannelId = _node.GetChannelId();
            append.Term = CurrentState.CurrentTerm;
            append.LeaderId = CurrentState.LeaderId;
            append.BlockHeight = lastBlock.Header.Number;
            append.BlockHash = lastBlock.Header.DataHash;
            append.BlockTerm = lastBlock.Header.Term;

            var rs = await peer.Request(append);
            if (!rs.Success)
            {
                _logger.LogWarning("请求区块错误", Newtonsoft.Json.JsonConvert.SerializeObject(append));
                return;
            }
            if (rs.BlockEntity == null)
            {
                return;
            }
            //如果区块不等于空 则 验证区块
            //区块保存
            _blockDataManager.PutOnChainBlockAsync(rs.BlockEntity.ToBlock());
        }

        public Task<TxResponse> TranspondTx(TxRequest request)
        {
            //签名
            request = _configProviders.SignatureForTx(request);
            var peer = _configProviders.GetPeer(_node.GetChannelId(), CurrentState.LeaderId);
            return peer.Transaction(request);
        }

        public Task<TxResponse> Transaction(TxRequest request)
        {
            var response = new TxResponse() { Msg = "follower拒绝服务", Status = false };
            return Task.FromResult(response);
        }

        public async Task<EndorseResponse> Endorse(TxRequest request)
        {
            EndorseResponse response = new EndorseResponse();
            //背书
            var result = await _node.ChainCodeSubmit(request);
            response.TxReadWriteSet = result.TxReadWriteSet;
            response.Status = true;
            response.Msg = "背书完成";
            response.Endorsement.Identity = _configProviders.GetPublicIndentity();

            //var str = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            var privatekey = _configProviders.GetPrivateKey();
            //节点对背书数据签名
            //response.Endorsement.Signature = DataHelper.RSASignature(str, privatekey);
            response.Endorsement.Signature = RSAHelper.SignData(privatekey, response);

            return response;
        }

        public async Task<HandOutResponse> BlockHandOut(HandOutRequest request)
        {
            //follower节点是接受区块
            switch (request.Type)
            {
                case HandOutType.CheckSave:
                    return await CheckBlockAndSave(request.Block);
                case HandOutType.Commit:
                    return await CommitBlock(request.Block);
                default:
                    return new HandOutResponse() { Success = false, Message = "" };
            }
        }

        private async Task<HandOutResponse> CheckBlockAndSave(Block block)
        {
            var rs = _blockDataManager.CacheBlock(block);
            return new HandOutResponse()
            {
                Success = rs
            };
        }

        private async Task<HandOutResponse> CommitBlock(Block block)
        {
            var rs = _blockDataManager.PutOnChainBlock(block.Header.DataHash);
            return new HandOutResponse()
            {
                Success = rs
            };
        }

    }
}