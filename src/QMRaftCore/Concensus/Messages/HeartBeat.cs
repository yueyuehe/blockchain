using QMRaftCore.Infrastructure;
using System;

namespace QMRaftCore.Concensus.Messages
{
    public class HeartBeat : Message
    {
        public HeartBeat() : base(Guid.NewGuid())
        {

        }

        public string ChannelId { get; set; }
        public long Term { get; set; }
        public string LeaderId { get; set; }

        #region 最后的日志信息

        public long Height { get; set; }
        public string LogCurrentHash { get; set; }
        public string LogPreviousHash { get; set; }
        public long LogTerm { get; set; }

        #endregion

    }
}
