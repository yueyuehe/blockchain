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
        /// �����־
        /// </summary>
        /// <param name="appendEntries"></param>
        /// <returns></returns>
        Task<AppendEntriesResponse> Handle(AppendEntries appendEntries);

        /// <summary>
        /// ѡ��
        /// </summary>
        /// <param name="requestVote"></param>
        /// <returns></returns>
        Task<RequestVoteResponse> Handle(RequestVote requestVote);

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="heartBeat"></param>
        /// <returns></returns>
        Task<HeartBeatResponse> Handle(HeartBeat heartBeat);

        /// <summary>
        /// ����ָ��
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<Response<T>> Accept<T>(T command) where T : ICommand;

        void Stop();

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<TxResponse> TranspondTx(TxRequest request);

        Task<TxResponse> Transaction(TxRequest request);

        Task<EndorseResponse> Endorse(TxRequest request);

        Task<HandOutResponse> BlockHandOut(HandOutRequest request);
    }
}