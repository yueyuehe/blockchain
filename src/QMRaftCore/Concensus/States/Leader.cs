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


        #region ״̬�л�

        /// <summary>
        /// ״̬�л�
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

        #region ��������

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
        /// ������ÿһ���ڵ㷢��������Ϣ ����־��Ϣ�� 
        /// ����յ��еĽڵ㷵�ص����ڴ��ڵ�ǰ��������ڵ��Ϊfollower
        /// </summary>
        /// <returns></returns>
        private async Task SendHeartbatAsync()
        {
            var peers = _configProviders.GetPeersExcludeSelf(_node.GetChannelId());
            if (peers.Count() == 0)
            {
                return;
            }
            //�����ṩ�Ľڵ�������ݷ���
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
            //��ȡ����Ҫ����������
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
                    //������ϾͿ��������ٷ�������
                    if (item.HeartBeatTask != null && item.HeartBeatTask.IsCompleted)
                    {
                        try
                        {
                            var rs = await item.HeartBeatTask;
                            //������ؽڵ����ڴ��ڵ�ǰ�ڵ����� ����Ҫ����ڵ�״̬
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
                            _logger.LogInformation($"��������ʧ��{ex.Message}");

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
            _logger.LogInformation($"leader�ڵ���{peer.Id}����������Ϣ,${heartBeat.Term}");
            return peer.Request(heartBeat);
        }
        #endregion

        /// <summary>
        /// ��ʼ�������ڵ�״̬
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

        #region �ڵ�״̬����

        /// <summary>
        /// ��������leader�ڵ����������
        /// </summary>
        /// <param name="heartBeat"></param>
        /// <returns></returns>
        public async Task<HeartBeatResponse> Handle(HeartBeat heartBeat)
        {
            _logger.LogInformation($"leader ����{heartBeat.LeaderId}������");
            //�����ǰ���ڴ�����������
            var rs = new HeartBeatResponse();
            var currentLog = _blockDataManager.GetLastBlockEntity(_node.GetChannelId());
            rs.Term = CurrentState.CurrentTerm;
            rs.Height = currentLog.Header.Number;
            rs.BlockCurrentHash = currentLog.Header.DataHash;
            rs.BlockPreviousHash = currentLog.Header.PreviousHash;
            rs.LeaderId = CurrentState.LeaderId;
            rs.Success = false;
            //��ǰ���ڴ���leader���� ���ص�ǰ���ڣ�leaderID��������־��Ϣ
            if (CurrentState.CurrentTerm > heartBeat.Term)
            {
                return rs;
            }
            else
            //����С�ڵ���leader���ڣ��򷵻�������־,�ı�����״̬(��������״̬���Լ����µ���־������һ��)
            {
                CurrentState.LeaderId = heartBeat.LeaderId;
                rs.LeaderId = heartBeat.LeaderId;
                rs.Success = true;
                return rs;
            }
        }

        /// <summary>
        /// ��������leader�ڵ��������������
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
        /// �������Ժ�ѡ�˵�ѡ��ͶƱ����
        /// </summary>
        /// <param name="requestVote"></param>
        /// <returns></returns>
        public Task<RequestVoteResponse> Handle(RequestVote requestVote)
        {
            _logger.LogInformation($"leader�ڵ�ͶƱ��{requestVote.CandidateId} leader����{CurrentState.CurrentTerm} ��ѡ������{requestVote.Term}");
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
            //��ȡ�ڵ�
            var peers = _configProviders.GetPeersExcludeSelf(_node.GetChannelId());

            var taskList = new List<Task<Response<TestCommand>>>();
            foreach (var item in peers)
            {
                taskList.Add(item.Request(new TestCommand()));
            }
            return null;

        }


        #region ����

        /// <summary>
        /// ת������
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<TxResponse> TranspondTx(TxRequest request)
        {
            //�Խ��׽���ǩ��
            request = _configProviders.SignatureForTx(request);
            return _node.Transaction(request);
        }

        /// <summary>
        /// �����׷�����Ҫ����ı���ڵ�
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<TxResponse> Transaction(TxRequest request)
        {
            var requestData = request.Data;

            var txResponse = new TxResponse();
            //��ȡ���������еı���ڵ�,
            var peers = _configProviders.GetEndorsePeer(requestData.Channel.Chaincode);
            var taskList = new Dictionary<string, Task<EndorseResponse>>();
            var endorseDir = new Dictionary<string, EndorseResponse>();
            var endorseRequest = new EndorseRequest
            {
                ChannelId = request.Header.ChannelId,
                Request = request
            };
            //��ӱ���task
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
                //���û�н��й����� �򷵻�false
                if (taskList.Count() == 0)
                {
                    txResponse.Msg = "�����㱳�����";
                    break;
                }
                foreach (var item in taskList)
                {
                    if (item.Value.IsCompleted)
                    {
                        endorseDir.Add(item.Key, await item.Value);
                    }
                }
                ///��֤�������
                var rs = _configProviders.ValidateEndorse(requestData.Channel.Chaincode, endorseDir);
                if (rs)
                {
                    txResult = true;
                    break;
                }
                Thread.Sleep(20);
                if (count * 20 > _configProviders.GetEndorseTimeOut())
                {
                    txResponse.Msg = "���鳬ʱ";
                    break;
                }
            }
            //����������ͨ�� 
            if (txResult)
            {
                //���׷�װ���� ����ͷ ������ ����ǩ��
                var envelopr = new Envelope
                {
                    TxReqeust = request
                };
                var endorses = endorseDir.Select(p => p.Value).ToList();
                foreach (var item in endorses)
                {
                    //����������ֵ�������ŷ�  ֻ��Ҫ��ֵһ��
                    if (envelopr.PayloadReponse.Status == false)
                    {
                        envelopr.PayloadReponse.Status = item.Status;
                        envelopr.PayloadReponse.Message = item.Msg;
                        envelopr.PayloadReponse.TxReadWriteSet = item.TxReadWriteSet;
                    }
                    envelopr.Endorsements.Add(item.Endorsement);
                }

                //���׼��뽻�׳���
                _node.TxPool.Add(envelopr);
                return new TxResponse() { Status = true, Msg = "�ȴ�����", TxId = request.Data.TxId };
                /*
                //var statusRs = await _node.TxPool.TxStatus(envelopr.TxReqeust.Data.TxId);
                if (statusRs == "0")
                {
                    txResponse.Status = true;
                    txResponse.Msg = "�����ɹ�";
                }
                if (statusRs == "1")
                {
                    txResponse.Status = false;
                    txResponse.Msg = "����ʧ��";
                }
                if (statusRs == "2")
                {
                    txResponse.Status = true;
                    txResponse.Msg = "������æ";
                }
                */
                //return txResponse;
            }
            else
            {
                //ʧ�ܵ�
                var errorTx = endorseDir.Where(p => p.Value.Status == false).Select(p => p.Key + ":" + p.Value.Msg).ToList();
                txResponse.Msg = "����ʧ��";
                txResponse.Data = errorTx;
                txResponse.Status = false;
                txResponse.TxId = request.Data.TxId;
                return txResponse;
            }
        }

        /// <summary>
        /// �ڵ�Խ��׽��б���
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<EndorseResponse> Endorse(TxRequest request)
        {
            EndorseResponse response = new EndorseResponse();
            //����
            var result = await _node.ChainCodeSubmit(request);
            if (result.StatusCode != StatusCode.Successful)
            {
                response.Status = false;
                response.Msg = result.StatusCode.ToString() + ":" + result.Message;
                return response;
            }
            response.TxReadWriteSet = result.TxReadWriteSet;
            response.Status = true;
            response.Msg = "�������";
            response.Endorsement.Identity = _configProviders.GetPublicIndentity();

            //var str = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            var privatekey = _configProviders.GetPrivateKey();
            //�ڵ�Ա�������ǩ��
            // response.Endorsement.Signature = DataHelper.RSASignature(str, privatekey);
            response.Endorsement.Signature = RSAHelper.SignData(privatekey, response);

            return response;
        }

        /// <summary>
        /// leader�ڵ�ַ�����
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HandOutResponse> BlockHandOut(HandOutRequest request)
        {
            var response = new HandOutResponse();
            //��ȡ���еĽڵ�
            var peers = _configProviders.GetAllPeer();
            //��ȡ����Զ���������Ľڵ�
            var notself = peers.Where(p => p.Id != CurrentState.Id).ToList();
            //���û�������ڵ� �����ύ���� �򷵻ؽ��
            if (notself.Count() == 0)
            {
                await _blockDataManager.PutOnChainBlockAsync(request.Block);
                response.Success = true;
                return response;
            }
            //�ַ�����
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
            //����ַ���У��ɹ��������Ƿ���ڶ���֮һ
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
                            //���ͶƱ��������һ��
                            //�����ȡ��peer���������ڵ�
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

            //���У������ɹ�
            if (checkback)
            {
                //���ر������� 
                //֪ͨ�ڵ㱾�ر�������
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
                //����
                var putonRS = await _blockDataManager.PutOnChainBlockAsync(request.Block);
                if (putonRS)
                {
                    response.Success = true;
                    response.Message = "block save successful";
                }
                else
                {
                    response.Success = false;
                    response.Message = "����ʧ��";
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