using Microsoft.Extensions.Logging;
using QMBlockSDK.TX;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.Node;
using QMRaftCore.Data;
using QMRaftCore.FiniteStateMachine;
using QMRaftCore.Infrastructure;
using QMRaftCore.QMProvider;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QMRaftCore.Concensus.States
{
    public sealed class Candidate : IState
    {
        private readonly IConfigProvider _configProviders;
        private readonly INode _node;
        private readonly IBlockDataManager _blockDataManager;
        private readonly ILogger<Candidate> _logger;
        internal void Reset(CurrentState state)
        {
            this.CurrentState = state;
        }


        public Candidate(
            IConfigProvider config,
            INode node,
            ILoggerFactory loggerFactory,
            IBlockDataManager dataManager
             )
        {
            _configProviders = config;
            _blockDataManager = dataManager;
            _logger = loggerFactory.CreateLogger<Candidate>();
            _node = node;
        }

        public CurrentState CurrentState { get; private set; }

        #region ״̬�仯
        private void BecomeLeader()
        {
            _node.BecomeLeader(CurrentState);
        }
        private void BecomeFollower()
        {
            //���ڼ�һ
            var newState = new CurrentState(CurrentState.Id, CurrentState.CurrentTerm - 1, "", CurrentState.CommitIndex, CurrentState.LastApplied, "");
            _node.BecomeFollower(newState);
        }

        #endregion

        #region ͶƱѡ�ٻ���

        /// <summary>
        /// ����ѡ��
        /// </summary>
        /// <returns></returns>
        public async Task<bool> QMElectionAsync()
        {
            var _votesThisElection = 1;
            var taskList = RequestVote();
            var count = 0;
            var badvote = 0;

            var allpeer = _configProviders.GetAllPeer(_node.GetChannelId());

            var winVotes = (allpeer.Count / 2) + 1;// (allpeer.Count % 2);

            while (true)
            {
                count++;
                for (var i = 0; i < taskList.Count; i++)
                {
                    if (taskList[i].IsCompleted)
                    {
                        var rs = await taskList[i];
                        if (rs.VoteGranted)
                        {
                            _votesThisElection++;
                        }
                        else
                        {
                            badvote++;
                        }
                        //���ͶƱ��������һ��
                        //�����ȡ��peer���������ڵ�
                        taskList.RemoveAt(i);
                        i--;
                    }

                    if (_votesThisElection >= winVotes)
                    {
                        return true;
                    }
                    if (badvote > winVotes)
                    {
                        return false;
                    }
                }
                if (taskList.Count == 0)
                {
                    return false;
                }
                if (10 * count > _configProviders.GetMinTimeout())
                {
                    return false;
                }
                System.Threading.Thread.Sleep(10);
            }
        }

        /// <summary>
        /// ����ͶƱ
        /// </summary>
        /// <returns>��������ͶƱ��Task</returns>
        private List<Task<RequestVoteResponse>> RequestVote()
        {
            var list = new List<Task<RequestVoteResponse>>();
            var peers = _configProviders.GetPeersExcludeSelf(_node.GetChannelId());
            var lastlog = _blockDataManager.GetLastBlockEntity(_node.GetChannelId());
            peers.ForEach(p =>
           {
               var ts = p.Request(new RequestVote()
               {
                   Term = CurrentState.CurrentTerm,
                   CandidateId = CurrentState.Id,
                   LastLogHeight = lastlog.Header.Number,
                   LastLogTerm = lastlog.Header.Term,
                   ChannelId = _node.GetChannelId()
               });
               list.Add(ts);
           });
            return list;
        }

        /// <summary>
        /// ��������
        /// </summary>
        private void SetUpElection()
        {
            //��ǰ�ǵڼ���ѡ��
            var nextTerm = CurrentState.CurrentTerm + 1;
            var votedFor = CurrentState.Id;
            CurrentState = new CurrentState(CurrentState.Id, nextTerm, votedFor,
                CurrentState.CommitIndex, CurrentState.LastApplied, CurrentState.LeaderId);
        }
        public async Task BeginElectionAsync()
        {
            SetUpElection();
            //���ֻ��һ���ڵ�
            if (_configProviders.GetPeersExcludeSelf(_node.GetChannelId()).Count() == 0)
            {
                BecomeLeader();
                return;
            }
            //��ʼѡ��
            var rs = await QMElectionAsync();
            if (rs)
            {
                BecomeLeader();
            }
            else
            {
                BecomeFollower();
            }
        }


        #endregion

        #region �ڵ�״̬���󷽷�

        public Task<AppendEntriesResponse> Handle(AppendEntries appendEntries)
        {
            return Task.FromResult(new AppendEntriesResponse()
            {
                Term = CurrentState.CurrentTerm,
                Success = false
            });
        }

        /// <summary>
        /// ����Ҫ
        /// </summary>
        /// <param name="requestVote"></param>
        /// <returns></returns>
        public Task<RequestVoteResponse> Handle(RequestVote requestVote)
        {
            _logger.LogWarning("candidate ��ѡ�˲����������˵�ͶƱ����");
            return Task.FromResult(new RequestVoteResponse()
            {
                VoteGranted = false,
                Term = 0
            });
        }

        public Task<HeartBeatResponse> Handle(HeartBeat heartBeat)
        {
            _logger.LogWarning("candidate ��ѡ�˲�������������");
            return Task.FromResult(new HeartBeatResponse()
            {
                Height = 0,
                Term = 0,
                Success = false
            });
        }

        #endregion

        /// <summary>
        /// �շ���
        /// </summary>
        public void Stop()
        {

        }



        public async Task<Response<T>> Accept<T>(T command) where T : ICommand
        {
            _logger.LogInformation("candidate dont forward to leader");
            return new ErrorResponse<T>("Please retry command later. Currently electing new a new leader.", command);
        }


        #region ����
        public Task<TxResponse> TranspondTx(TxRequest request)
        {
            _logger.LogWarning("candidate�ڵ㲻���ս���ת������");
            var response = new TxResponse() { Msg = "candidate�ܾ�����", Status = false };
            return Task.FromResult(response);
        }

        public Task<TxResponse> Transaction(TxRequest request)
        {
            _logger.LogWarning("candidate�ڵ㲻�ӽ�������");
            var response = new TxResponse() { Msg = "candidate�ܾ�����", Status = false };
            return Task.FromResult(response);
        }

        public Task<EndorseResponse> Endorse(TxRequest request)
        {
            _logger.LogWarning("candidate�ڵ㲻���ձ�������");

            var response = new EndorseResponse() { Msg = "candidate�ܾ�����", Status = false };
            return Task.FromResult(response);
        }

        public Task<HandOutResponse> BlockHandOut(HandOutRequest request)
        {
            _logger.LogWarning("candidate�ڵ㲻���շַ�������");

            var rs = new HandOutResponse()
            {
                Success = false,
                Message = "Candidate node can not hand out block"
            };
            return Task.FromResult(rs);
        }

        #endregion

    }
}