using Microsoft.Extensions.Logging;
using QMBlockSDK.CC;
using QMBlockSDK.Ledger;
using QMBlockSDK.TX;
using QMBlockUtils;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.Node;
using QMRaftCore.Concensus.Peers;
using QMRaftCore.Data;
using QMRaftCore.FiniteStateMachine;
using QMRaftCore.Infrastructure;
using QMRaftCore.QMProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QMRaftCore.Concensus.States
{

    public sealed class Leader : IState
    {
        private readonly IConfigProvider _configProviders;
        private readonly INode _node;
        private readonly IBlockDataManager _blockDataManager;
        private readonly ILogger<Leader> _logger;
        private Timer _electionTimer;
        public List<PeerState> PeerStates { get; private set; }
        public CurrentState CurrentState { get; private set; }
        public Leader(
             IConfigProvider config,
            INode node,
            ILoggerFactory loggerFactory,
            IBlockDataManager dataManager)
        {
            _configProviders = config;
            _blockDataManager = dataManager;
            _logger = loggerFactory.CreateLogger<Leader>();
            _node = node;
        }


        #region 状态切换

        /// <summary>
        /// 状态切换
        /// </summary>
        /// <param name="state"></param>
        internal void Reset(CurrentState state)
        {
            this.CurrentState = state;
            InitialisePeerStates();
            ResetElectionTimer();
        }

        public void Stop()
        {
            if (_electionTimer != null)
            {
                _electionTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private void BecomeFollower()
        {
            Stop();
            _logger.LogInformation("*****************leader to follower ");
            _node.BecomeFollower(CurrentState);
        }

        #endregion

        #region 心跳机制

        private void ResetElectionTimer()
        {
            if (_electionTimer == null)
            {
                _electionTimer = new Timer(async x =>
                {
                    try
                    {
                        _electionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        await SendHeartbatAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                    finally
                    {
                        _electionTimer.Change(Convert.ToInt32(_configProviders.GetHeartbeatTimeout()), 0);
                    }

                }, null, 0, Convert.ToInt32(_configProviders.GetHeartbeatTimeout()));
            }
            else
            {
                _electionTimer.Change(0, Convert.ToInt32(_configProviders.GetHeartbeatTimeout()));
            }

        }

        /// <summary>
        /// 单独给每一个节点发送心跳信息 和日志信息， 
        /// 如果收到有的节点返回的任期大于当前的任期则节点变为follower
        /// </summary>
        /// <returns></returns>
        private async Task SendHeartbatAsync()
        {
            var peers = _configProviders.GetPeersExcludeSelf(_node.GetChannelId());
            if (peers.Count() == 0)
            {
                return;
            }
            //根据提供的节点进行数据访问
            PeerStates = PeerStates.Where(p => peers.Select(x => x.Id).Contains(p.Peer.Id)).ToList();
            if (PeerStates.Count != peers.Count)
            {
                var peersNotInPeerStates = peers.Where(p => !PeerStates.Select(x => x.Peer.Id).Contains(p.Id)).ToList();
                peersNotInPeerStates.ForEach(p =>
                {
                    var model = new PeerState(p);
                    PeerStates.Add(new PeerState(p));
                });
            }
            var lastlog = _blockDataManager.GetLastBlockEntity(_node.GetChannelId());

            var heartbeat = new HeartBeat();
            heartbeat.Term = CurrentState.CurrentTerm;
            heartbeat.LeaderId = CurrentState.Id;
            heartbeat.Height = lastlog.Header.Number;
            heartbeat.LogCurrentHash = lastlog.Header.DataHash;
            heartbeat.LogPreviousHash = lastlog.Header.PreviousHash;
            heartbeat.LogTerm = lastlog.Header.Term;
            heartbeat.ChannelId = lastlog.Header.ChannelId;
            //获取所需要的区块数据
            var needBecomeFollower = false;
            HeartBeatResponse heartResponse = null;
            foreach (var item in PeerStates)
            {
                if (item.HeartBeatTask == null)
                {
                    item.HeartBeatTask = HeartBeatRequest(item.Peer, heartbeat);
                }
                else
                {
                    _logger.LogInformation($"{item.Peer.Id}  {item.HeartBeatTask.IsCompleted}");
                    //请求完毕就可以重新再发生请求
                    if (item.HeartBeatTask != null && item.HeartBeatTask.IsCompleted)
                    {
                        try
                        {
                            var rs = await item.HeartBeatTask;
                            //如果返回节点任期大于当前节点任期 则需要变更节点状态
                            if (rs.Term > CurrentState.CurrentTerm)
                            {
                                needBecomeFollower = true;
                                heartResponse = rs;
                                break;
                            }
                            item.CurrentHash = rs.BlockCurrentHash;
                            item.PreviousHash = rs.BlockPreviousHash;
                            item.Height = rs.Height;
                            item.LogTerm = rs.LogTerm;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation($"心跳请求失败{ex.Message}");

                        }
                        finally
                        {
                            item.HeartBeatTask = null;
                        }
                    }
                }
            }

            if (needBecomeFollower)
            {
                CurrentState.CurrentTerm = heartbeat.Term;
                BecomeFollower();
            }
        }

        private Task<HeartBeatResponse> HeartBeatRequest(IPeer peer, HeartBeat heartBeat)
        {
            _logger.LogInformation($"leader节点向{peer.Id}发送心跳信息,${heartBeat.Term}");
            return peer.Request(heartBeat);
        }
        #endregion

        /// <summary>
        /// 初始化其他节点状态
        /// </summary>
        private void InitialisePeerStates()
        {
            PeerStates = new List<PeerState>();
            var peers = _configProviders.GetPeersExcludeSelf(_node.GetChannelId());
            peers.ForEach(p =>
           {
               PeerStates.Add(new PeerState(p));
           });
        }

        #region 节点状态请求

        /// <summary>
        /// 接受来自leader节点的心跳请求
        /// </summary>
        /// <param name="heartBeat"></param>
        /// <returns></returns>
        public async Task<HeartBeatResponse> Handle(HeartBeat heartBeat)
        {
            _logger.LogInformation($"leader 接收{heartBeat.LeaderId}的心跳");
            //如果当前任期大于心跳任期
            var rs = new HeartBeatResponse();
            var currentLog = _blockDataManager.GetLastBlockEntity(_node.GetChannelId());
            rs.Term = CurrentState.CurrentTerm;
            rs.Height = currentLog.Header.Number;
            rs.BlockCurrentHash = currentLog.Header.DataHash;
            rs.BlockPreviousHash = currentLog.Header.PreviousHash;
            rs.LeaderId = CurrentState.LeaderId;
            rs.Success = false;
            //当前任期大于leader任期 返回当前任期，leaderID和最新日志信息
            if (CurrentState.CurrentTerm > heartBeat.Term)
            {
                return rs;
            }
            else
            //任期小于等于leader任期，则返回最新日志,改变自身状态(觉得自身状态和自己最新的日志的任期一致)
            {
                CurrentState.LeaderId = heartBeat.LeaderId;
                rs.LeaderId = heartBeat.LeaderId;
                rs.Success = true;
                return rs;
            }
        }

        /// <summary>
        /// 接受来自leader节点的区块数据请求
        /// </summary>
        /// <param name="appendEntries"></param>
        /// <returns></returns>
        public async Task<AppendEntriesResponse> Handle(AppendEntries appendEntries)
        {
            var response = new AppendEntriesResponse
            {
                Term = CurrentState.CurrentTerm,
                Success = false
            };

            if (CurrentState.Id == appendEntries.LeaderId
               && CurrentState.CurrentTerm == appendEntries.Term
               )
            {
                var log = _blockDataManager.GetBlockEntity(appendEntries.ChannelId, appendEntries.BlockHeight + 1);
                if (log != null)
                {
                    response.Height = log.Header.Number;
                    response.CurrentHash = log.Header.DataHash;
                    response.BlockEntity = log;
                    response.PreviousHash = log.Header.PreviousHash;
                    response.Success = true;
                }
            }
            return response;
        }

        /// <summary>
        /// 接受来自候选人的选举投票请求
        /// </summary>
        /// <param name="requestVote"></param>
        /// <returns></returns>
        public Task<RequestVoteResponse> Handle(RequestVote requestVote)
        {
            _logger.LogInformation($"leader节点投票给{requestVote.CandidateId} leader任期{CurrentState.CurrentTerm} 候选人任期{requestVote.Term}");
            if (requestVote.Term > CurrentState.CurrentTerm)
            {
                CurrentState.VotedFor = requestVote.CandidateId;
                return Task.FromResult(new RequestVoteResponse()
                {
                    Term = requestVote.Term,
                    VoteGranted = true
                });
            }
            else
            {
                return Task.FromResult(new RequestVoteResponse()
                {
                    Term = CurrentState.CurrentTerm,
                    VoteGranted = false
                });
            }
        }

        #endregion
        public Task<Response<T>> Accept<T>(T command) where T : ICommand
        {
            //获取节点
            var peers = _configProviders.GetPeersExcludeSelf(_node.GetChannelId());

            var taskList = new List<Task<Response<TestCommand>>>();
            foreach (var item in peers)
            {
                taskList.Add(item.Request(new TestCommand()));
            }
            return null;

        }


        #region 交易

        /// <summary>
        /// 转发交易
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<TxResponse> TranspondTx(TxRequest request)
        {
            //对交易进行签名
            request = _configProviders.SignatureForTx(request);
            return _node.Transaction(request);
        }

        /// <summary>
        /// 将交易发给需要背书的背书节点
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<TxResponse> Transaction(TxRequest request)
        {
            var requestData = request.Data;

            var txResponse = new TxResponse();
            //获取链码配置中的背书节点,
            var peers = _configProviders.GetEndorsePeer(requestData.Channel.Chaincode);
            var taskList = new Dictionary<string, Task<EndorseResponse>>();
            var endorseDir = new Dictionary<string, EndorseResponse>();
            var endorseRequest = new EndorseRequest
            {
                ChannelId = request.Header.ChannelId,
                Request = request
            };
            //添加背书task
            foreach (var item in peers)
            {
                if (item.Id == CurrentState.Id)
                {
                    taskList.Add(item.Id, Endorse(request));
                }
                else
                {
                    taskList.Add(item.Id, item.Endorse(endorseRequest));
                }
            }
            var txResult = false;
            var count = 0;
            while (true)
            {
                taskList = taskList.Where(p => !endorseDir.ContainsKey(p.Key)).ToDictionary(p => p.Key, p => p.Value);
                //如果没有进行过背书 则返回false
                if (taskList.Count() == 0)
                {
                    txResponse.Msg = "不满足背书策略";
                    break;
                }
                foreach (var item in taskList)
                {
                    if (item.Value.IsCompleted)
                    {
                        endorseDir.Add(item.Key, await item.Value);
                    }
                }
                ///验证背书策略
                var rs = _configProviders.ValidateEndorse(requestData.Channel.Chaincode, endorseDir);
                if (rs)
                {
                    txResult = true;
                    break;
                }
                Thread.Sleep(20);
                if (count * 20 > _configProviders.GetEndorseTimeOut())
                {
                    txResponse.Msg = "背书超时";
                    break;
                }
            }
            //如果背书策略通过 
            if (txResult)
            {
                //交易封装对象 交易头 背书结果 背书签名
                var envelopr = new Envelope
                {
                    TxReqeust = request
                };
                var endorses = endorseDir.Select(p => p.Value).ToList();
                foreach (var item in endorses)
                {
                    //将背书结果赋值给交易信封  只需要赋值一次
                    if (envelopr.PayloadReponse.Status == false)
                    {
                        envelopr.PayloadReponse.Status = item.Status;
                        envelopr.PayloadReponse.Message = item.Msg;
                        envelopr.PayloadReponse.TxReadWriteSet = item.TxReadWriteSet;
                    }
                    envelopr.Endorsements.Add(item.Endorsement);
                }

                //交易加入交易池中
                _node.TxPool.Add(envelopr);
                return new TxResponse() { Status = true, Msg = "等待上链", TxId = request.Data.TxId };
                /*
                //var statusRs = await _node.TxPool.TxStatus(envelopr.TxReqeust.Data.TxId);
                if (statusRs == "0")
                {
                    txResponse.Status = true;
                    txResponse.Msg = "上链成功";
                }
                if (statusRs == "1")
                {
                    txResponse.Status = false;
                    txResponse.Msg = "上链失败";
                }
                if (statusRs == "2")
                {
                    txResponse.Status = true;
                    txResponse.Msg = "服务器忙";
                }
                */
                //return txResponse;
            }
            else
            {
                //失败的
                var errorTx = endorseDir.Where(p => p.Value.Status == false).Select(p => p.Key + ":" + p.Value.Msg).ToList();
                txResponse.Msg = "背书失败";
                txResponse.Data = errorTx;
                txResponse.Status = false;
                txResponse.TxId = request.Data.TxId;
                return txResponse;
            }
        }

        /// <summary>
        /// 节点对交易进行背书
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<EndorseResponse> Endorse(TxRequest request)
        {
            EndorseResponse response = new EndorseResponse();
            //背书
            var result = await _node.ChainCodeSubmit(request);
            if (result.StatusCode != StatusCode.Successful)
            {
                response.Status = false;
                response.Msg = result.StatusCode.ToString() + ":" + result.Message;
                return response;
            }
            response.TxReadWriteSet = result.TxReadWriteSet;
            response.Status = true;
            response.Msg = "背书完成";
            response.Endorsement.Identity = _configProviders.GetPublicIndentity();

            //var str = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            var privatekey = _configProviders.GetPrivateKey();
            //节点对背书数据签名
            // response.Endorsement.Signature = DataHelper.RSASignature(str, privatekey);
            response.Endorsement.Signature = RSAHelper.SignData(privatekey, response);

            return response;
        }

        /// <summary>
        /// leader节点分发区块
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HandOutResponse> BlockHandOut(HandOutRequest request)
        {
            var response = new HandOutResponse();
            //获取所有的节点
            var peers = _configProviders.GetAllPeer();
            //获取需求远程请求服务的节点
            var notself = peers.Where(p => p.Id != CurrentState.Id).ToList();
            //如果没有其他节点 本地提交区块 则返回结果
            if (notself.Count() == 0)
            {
                await _blockDataManager.PutOnChainBlockAsync(request.Block);
                response.Success = true;
                return response;
            }
            //分发区块
            List<Task<HandOutResponse>> taskList = new List<Task<HandOutResponse>>();
            var handOutRequest = new HandOutRequest
            {
                Block = request.Block,
                Type = HandOutType.CheckSave,
                ChannelId = request.ChannelId
            };
            foreach (var item in notself)
            {
                taskList.Add(item.BlockHandOut(handOutRequest));
            }
            var rightcount = 1;
            var badcount = 0;
            //区块分发的校验成功的数量是否大于二分之一
            var checkback = false;
            while (true)
            {
                for (var i = 0; i < taskList.Count; i++)
                {
                    if (taskList[i].IsCompleted)
                    {
                        var rs = await taskList[i];
                        taskList.RemoveAt(i);
                        i--;
                        if (rs.Success)
                        {
                            rightcount++;
                            //如果投票数量大于一半
                            //这里获取的peer不包括本节点
                            if (rightcount >= ((peers.Count / 2) + 1))
                            {
                                checkback = true;
                                break;
                            }
                        }
                        else
                        {
                            badcount++;
                            if (badcount >= ((peers.Count / 2) + 1))
                            {
                                checkback = false;
                                break;
                            }
                        }
                    }
                }
                if (taskList.Count == 0)
                {
                    break;
                }
                System.Threading.Thread.Sleep(10);
            }

            //如过校验区块成功
            if (checkback)
            {
                //本地保存区块 
                //通知节点本地保存区块
                var commitHandOutRequest = new HandOutRequest
                {
                    Block = request.Block,
                    ChannelId = _node.GetChannelId(),
                    Type = HandOutType.Commit
                };
                foreach (var item in notself)
                {
                    taskList.Add(item.BlockHandOut(commitHandOutRequest));
                }
                //返回
                var putonRS = await _blockDataManager.PutOnChainBlockAsync(request.Block);
                if (putonRS)
                {
                    response.Success = true;
                    response.Message = "block save successful";
                }
                else
                {
                    response.Success = false;
                    response.Message = "上链失败";
                }
                return response;

            }
            else
            {
                response.Success = false;
                response.Message = "follower check block err";
                return response;
            }
        }

        #endregion

    }
}