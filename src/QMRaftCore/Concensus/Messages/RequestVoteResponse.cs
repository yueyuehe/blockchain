namespace QMRaftCore.Concensus.Messages
{
    /// <summary>
    /// ѡ�ٽ������
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
        /// true ��ʾ�����ѡƱ
        /// </summary>
        public bool VoteGranted { get; set; }

        /// <summary>
        /// CurrentTerm, for candidate to update itself.
        /// </summary>
        public long Term { get; set; }
    }
}