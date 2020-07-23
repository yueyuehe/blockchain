
using QMBlockSDK.CC;
using QMBlockSDK.Ledger;
using QMBlockSDK.TX;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.States;
using QMRaftCore.FiniteStateMachine;
using QMRaftCore.Infrastructure;
using QMRaftCore.QMProvider.Imp;
using System.Threading.Tasks;

namespace QMRaftCore.Concensus.Node
{
    public interface INode
    {

        string GetChannelId();

        #region 节点状态
        IState State { get; }

        TxPool TxPool { get; }

        void BecomeLeader(CurrentState state);
        void BecomeFollower(CurrentState state);
        void BecomeCandidate(CurrentState state);

        #endregion

        #region 节点基本请求 心跳 选举 日志追加

        Task<AppendEntriesResponse> Handle(AppendEntries appendEntries);
        Task<RequestVoteResponse> Handle(RequestVote requestVote);
        Task<HeartBeatResponse> Handle(HeartBeat requestVote);

        #endregion

        #region 节点启动与停止

        void Start(NodeId id);
        void Stop();

        #endregion

        Task<Response<T>> Accept<T>(T command) where T : ICommand;

        #region 节点交易方法

        /// <summary>
        /// 转发交易 将交易转发到leader节点
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<TxResponse> TransactionCommit(TxRequest request);

        /// <summary>
        /// leader节点处理交易
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<TxResponse> Transaction(TxRequest request);

        Task<EndorseResponse> Endorse(TxRequest request);

        #endregion

        #region 提交链码

        Task<ChainCodeInvokeResponse> ChainCodeSubmit(TxRequest request);

        #endregion

        /// <summary>
        /// 创建新的通道
        /// </summary>
        /// <param name="channelid"></param>
        Task<TxResponse> CreateNewChannel(string channelid);

        #region 区块扩散

        Task<HandOutResponse> Handle(HandOutRequest requeset);

        Task<HandOutResponse> BlockHandOut(Block block);
        #endregion

    }
}