using QMBlockSDK.TX;

namespace QMBlockSDK.Ledger
{
    public class ChannelHeader
    {
        public TxType Type { get; set; }
        public string Version { get; set; }
        public long Timestamp { get; set; }
        public string ChannelId { get; set; }
        public string TxId { get; set; }
    }
}
