using QMBlockSDK.MongoModel;

namespace QMRaftCore.Concensus.Messages
{
    public sealed class AppendEntriesResponse
    {

        /// <summary>
        /// CurrentTerm, for leader to update itself.
        /// </summary>
        public long Term { get; set; }
        /// <summary>
        /// True if follower contained entry matching prevLogIndex and prevLogTerm.
        /// </summary>
        public bool Success { get; set; }
        public string LeaderId { get; set; }
        public string CurrentHash { get; set; }
        public string PreviousHash { get; set; }
        public long BlockTerm { get; set; }
        public long Height { get; set; }
        public MongoBlock BlockEntity { get; set; }
    }
}