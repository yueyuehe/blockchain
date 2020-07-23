using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.Peers;
using System.Threading.Tasks;

namespace QMRaftCore.Concensus.States
{

    public class PeerState
    {
        public PeerState(IPeer peer)
        {
            Peer = peer;
        }
        public IPeer Peer { get; private set; }
        public string CurrentHash { get; set; }
        public string PreviousHash { get; set; }
        public long Height { get; set; } = -1;
        public long LogTerm { get; set; }

        public Task<HeartBeatResponse> HeartBeatTask;
    }
}