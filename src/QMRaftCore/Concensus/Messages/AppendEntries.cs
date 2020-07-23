
using QMRaftCore.Infrastructure;
using System;

namespace QMRaftCore.Concensus.Messages
{
    public sealed class AppendEntries : Message
    {
        public AppendEntries() : base(Guid.NewGuid())
        {

        }

        public string ChannelId { get; set; }
        public long Term { get; set; }
        public string LeaderId { get; set; }
        public long BlockHeight { get; set; }
        public long BlockTerm { get; set; }
        public string BlockHash { get; set; }

    }
}