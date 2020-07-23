namespace QMRaftCore.Concensus.States
{

    /// <summary>
    /// ��ǰ״̬
    /// </summary>
    public class CurrentState
    {
        public CurrentState(string id, long currentTerm, string votedFor, int commitIndex, int lastApplied, string leaderId)
        {
            Id = id;
            CurrentTerm = currentTerm;
            VotedFor = votedFor;
            CommitIndex = commitIndex;
            LastApplied = lastApplied;
            LeaderId = leaderId;
        }

        /// <summary>
        /// ��ǰ����
        /// </summary>
        public long CurrentTerm { get; set; }
        /// <summary>
        /// ͶƱ��XX
        /// </summary>
        public string VotedFor { get; set; }
        public int CommitIndex { get; set; }
        public int LastApplied { get; set; }

        /// <summary>
        /// ��ǰID
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// leaderID
        /// </summary>
        public string LeaderId { get; set; }
    }
}