namespace QMRaftCore.Concensus.Messages
{
    /// <summary>
    /// 选举请求对象
    /// </summary>
    public sealed class RequestVote
    {
        /// <summary>
        /// Term candidate’s term.
        /// </summary>
        public long Term { get; set; }

        /// <summary>
        /// CandidateId candidate requesting vote.
        /// 要求投票的候选人ID
        /// </summary>
        public string CandidateId { get; set; }

        /// <summary>
        /// LastLogIndex index of candidate’s last log entry (§5.4).
        /// 候选人最后一个日志条目的索引
        /// </summary>
        public long LastLogHeight { get; set; }

        /// <summary>
        /// LastLogTerm term of candidate’s last log entry (§5.4).   
        /// 候选人最后一个日志条目的term
        /// </summary>
        public long LastLogTerm { get; set; }

        public string ChannelId { get; set; }
    }

}