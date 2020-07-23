using QMRaftCore.Concensus.Messages;
using System.Threading.Tasks;

/**
 * 区块链Raft网络底层服务
 * 
 * 响应投票选举
 * 
 * 接收心跳请求
 * 
 * 接收日志请求
 * 
 * 
 */

namespace QMBlockServer.Service.Intetface
{
    public interface IRaftNetService
    {
        Task<AppendEntriesResponse> Handle(AppendEntries appendEntries);

        Task<HeartBeatResponse> Handle(HeartBeat request);

        Task<RequestVoteResponse> Handle(RequestVote requestVote);

    }
}
