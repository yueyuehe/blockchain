using QMBlockSDK.TX;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.FiniteStateMachine;
using QMRaftCore.Infrastructure;
using System.Threading.Tasks;

namespace QMRaftCore.Concensus.States
{

    public interface IState
    {
        CurrentState CurrentState { get; }

        /// <summary>
        /// 添加日志
        /// </summary>
        /// <param name="appendEntries"></param>
        /// <returns></returns>
        Task<AppendEntriesResponse> Handle(AppendEntries appendEntries);

        /// <summary>
        /// 选举
        /// </summary>
        /// <param name="requestVote"></param>
        /// <returns></returns>
        Task<RequestVoteResponse> Handle(RequestVote requestVote);

        /// <summary>
        /// 心跳
        /// </summary>
        /// <param name="heartBeat"></param>
        /// <returns></returns>
        Task<HeartBeatResponse> Handle(HeartBeat heartBeat);

        /// <summary>
        /// 接收指令
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<Response<T>> Accept<T>(T command) where T : ICommand;

        void Stop();

        /// <summary>
        /// 交易
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<TxResponse> TranspondTx(TxRequest request);

        Task<TxResponse> Transaction(TxRequest request);

        Task<EndorseResponse> Endorse(TxRequest request);

        Task<HandOutResponse> BlockHandOut(HandOutRequest request);
    }
}