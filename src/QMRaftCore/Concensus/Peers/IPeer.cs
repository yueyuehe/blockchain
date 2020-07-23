using QMBlockSDK.TX;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.FiniteStateMachine;
using QMRaftCore.Infrastructure;
using System.Threading.Tasks;

namespace QMRaftCore.Concensus.Peers
{
    public interface IPeer
    {
        /// <summary>
        /// This will return the peers ID.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// This will make a requestvote request to the given peer. You must implement the transport.
        /// 这将向给定的对等点发出requestvote请求。您必须实现传输。
        /// </summary>
        Task<RequestVoteResponse> Request(RequestVote requestVote);

        Task<HeartBeatResponse> Request(HeartBeat request);

        /// <summary>
        /// This will make a appendentries request to the given peer. You must implement the transport.
        /// 这将向给定的对等点发出一个附录条目请求。您必须实现传输。
        /// </summary>
        Task<AppendEntriesResponse> Request(AppendEntries appendEntries);

        /// <summary>
        /// This will make a command request ot the given peer. You must implement the transport.
        /// 这将向给定的对等点发出一个命令请求。您必须实现传输
        /// </summary>
        Task<Response<T>> Request<T>(T command) where T : ICommand;
        Task<TxResponse> Transaction(TxRequest request);
        Task<EndorseResponse> Endorse(EndorseRequest endorseRequest);

        /// <summary>
        /// 分发区块
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        Task<HandOutResponse> BlockHandOut(HandOutRequest request);

    }
}