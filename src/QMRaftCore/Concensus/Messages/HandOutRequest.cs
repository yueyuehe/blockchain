using QMBlockSDK.Ledger;

namespace QMRaftCore.Concensus.Messages
{
    public class HandOutRequest
    {
        public string ChannelId { get; set; }
        public Block Block { get; set; }
        public HandOutType Type { get; set; }

    }
    public enum HandOutType
    {
        CheckSave, Commit
    }
}
