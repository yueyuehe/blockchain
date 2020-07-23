using QMRaftCore.Concensus.Peers;

namespace QMRaftCore.Concensus.States
{
    public class MatchIndex
    {
        public MatchIndex(IPeer peer, int indexOfHighestKnownReplicatedLog)
        {
            Peer = peer;
            IndexOfHighestKnownReplicatedLog = indexOfHighestKnownReplicatedLog;
        }

        public IPeer Peer { get; private set; }
        public int IndexOfHighestKnownReplicatedLog { get; private set; }
    }
}