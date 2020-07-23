namespace QMBlockSDK.Ledger
{
    public class BlockHeader
    {
        public long Number { get; set; }
        public long Term { get; set; }
        public string PreviousHash { get; set; }
        public string DataHash { get; set; }
        public long Timestamp { get; set; }
        public string ChannelId { get; set; }

    }
}
