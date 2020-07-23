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
        /// ����״̬
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
        /// ����ѡ�ټ�ʱ��
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
                        //ֹͣ��ʱ
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

        #region ״̬���󷽷�
        public async Task<RequestVoteResponse> Handle(RequestVote requestVote)
        {
            //����ʱ��
            try
            {
                //�ж����� ���ڴ��ڵ�ǰ����
                if (requestVote.Term <= CurrentState.CurrentTerm || !string.IsNullOrEmpty(CurrentState.VotedFor))
                {
                    return new RequestVoteResponse(false, CurrentState.CurrentTerm);
                }
                //�ж���־ ��ǰ��־����
                var lastLog = _blockDataManager.GetLastBlockEntity(_node.GetChannelId());
                if (lastLog != null)
                {
                    //������־����ͶƱ��־ ����false
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
                //����û����־ͬ��
                else
                {
                    CurrentState.VotedFor = requestVote.CandidateId;
                    return new RequestVoteResponse(true, CurrentState.CurrentTerm);
                }
            }
            finally
            {
                //��ʾ�ɹ����ܵ���ͶƱ��Ϣ ���¼�ʱ
                ResetElectionTimer();
            }
        }

        public async Task<HeartBeatResponse> Handle(HeartBeat heartBeat)
        {
            try
            {
                ///���ܵ�����Ϣ  ֹͣ��ʱ �ȴ�����
                _electionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _logger.LogInformation($"���յ�����{heartBeat.LeaderId} ���� {heartBeat.Term} ������");
                var rs = new HeartBeatResponse();
                ///��ȡ��ǰ�ڵ����µ�����
                var currentBlock = _blockDataManager.GetLastBlockEntity(_node.GetChannelId());
                //�������Ϊ0���ʼ���������
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
                //��ǰ���ڴ���leader���� ���ص�ǰ���ڣ�leaderID��������־��Ϣ
                if (CurrentState.CurrentTerm > heartBeat.Term)
                {
                    return rs;
                }
                //����С�ڵ���leader���ڣ�
                //�򷵻�������־,�ı�����״̬(��������״̬���Լ����µ���־������һ��)
                else
                {
                    CurrentState.LeaderId = heartBeat.LeaderId;
                    CurrentState.CurrentTerm = heartBeat.Term;
                    rs.LeaderId = heartBeat.LeaderId;
                    rs.Success = true;
                    _logger.LogInformation("����Ϸ���ˢ��ѡ�ټ�ʱ��");
                    //ͬ��ָ���߶ȵ�����
                    await SynchronizedBlockAsync(heartBeat.Height);
                    //���ճɹ�����Ϣ��ˢ�¼�ʱ��
                    //ResetElectionTimer();
                    return rs;
                }
            }
            finally
            {
                //���ճɹ�����Ϣ��ˢ�¼�ʱ��
                ResetElectionTimer();
            }

        }

        public Task<AppendEntriesResponse> Handle(AppendEntries appendEntries)
        {
            //follower�����������־
            return Task.FromResult(new AppendEntriesResponse()
            {
                Success = false,
                Term = CurrentState.CurrentTerm
            });
        }

        public async Task<Response<T>> Accept<T>(T command) where T : ICommand
        {
            //��ȡleader�ڵ�
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
        /// ��������leader�ڵ������ʱ������� ͬ������
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
            //��ǰblock����Ϣ
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
                _logger.LogWarning("�����������", Newtonsoft.Json.JsonConvert.SerializeObject(append));
                return;
            }
            if (rs.BlockEntity == null)
            {
                return;
            }
            //������鲻���ڿ� �� ��֤����
            //���鱣��
            _blockDataManager.PutOnChainBlockAsync(rs.BlockEntity.ToBlock());
        }

        public Task<TxResponse> TranspondTx(TxRequest request)
        {
            //ǩ��
            request = _configProviders.SignatureForTx(request);
            var peer = _configProviders.GetPeer(_node.GetChannelId(), CurrentState.LeaderId);
            return peer.Transaction(request);
        }

        public Task<TxResponse> Transaction(TxRequest request)
        {
            var response = new TxResponse() { Msg = "follower�ܾ�����", Status = false };
            return Task.FromResult(response);
        }

        public async Task<EndorseResponse> Endorse(TxRequest request)
        {
            EndorseResponse response = new EndorseResponse();
            //����
            var result = await _node.ChainCodeSubmit(request);
            response.TxReadWriteSet = result.TxReadWriteSet;
            response.Status = true;
            response.Msg = "�������";
            response.Endorsement.Identity = _configProviders.GetPublicIndentity();

            //var str = Newtonsoft.Json.JsonConvert.SerializeObject(response);
            var privatekey = _configProviders.GetPrivateKey();
            //�ڵ�Ա�������ǩ��
            //response.Endorsement.Signature = DataHelper.RSASignature(str, privatekey);
            response.Endorsement.Signature = RSAHelper.SignData(privatekey, response);

            return response;
        }

        public async Task<HandOutResponse> BlockHandOut(HandOutRequest request)
        {
            //follower�ڵ��ǽ�������
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