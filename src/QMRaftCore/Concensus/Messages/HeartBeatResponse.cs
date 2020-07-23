namespace QMRaftCore.Concensus.Messages
{
    public class HeartBeatResponse
    {
        public long Term { get; set; }
        public string LeaderId { get; set; }
        public bool Success { get; set; }

        #region last日志信息

        public long Height { get; set; }
        public string BlockCurrentHash { get; set; }
        public string BlockPreviousHash { get; set; }
        public long LogTerm { get; set; }

        #endregion

    }

}
