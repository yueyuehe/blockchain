namespace QMRaftCore.Concensus.Messages
{
    /// <summary>
    /// 选举结果对象
    /// </summary>
    public sealed class RequestVoteResponse
    {
        public RequestVoteResponse(bool voteGranted, long term)
        {
            VoteGranted = voteGranted;
            Term = term;
        }
        public RequestVoteResponse() { }
        /// <summary>
        /// True means candidate received vote.
        /// true 表示获得了选票
        /// </summary>
        public bool VoteGranted { get; set; }

        /// <summary>
        /// CurrentTerm, for candidate to update itself.
        /// </summary>
        public long Term { get; set; }
    }
}