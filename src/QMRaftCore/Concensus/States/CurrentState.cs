namespace QMRaftCore.Concensus.States
{

    /// <summary>
    /// 当前状态
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
        /// 当前任期
        /// </summary>
        public long CurrentTerm { get; set; }
        /// <summary>
        /// 投票给XX
        /// </summary>
        public string VotedFor { get; set; }
        public int CommitIndex { get; set; }
        public int LastApplied { get; set; }

        /// <summary>
        /// 当前ID
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// leaderID
        /// </summary>
        public string LeaderId { get; set; }
    }
}