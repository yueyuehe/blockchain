namespace QMBlockSDK.TX
{
    public class TxResponse
    {
        public string TxId { get; set; }
        public bool Status { get; set; }
        public string Msg { get; set; }
        //查询的数据
        public object Data { get; set; }
        public long BlockNumber { get; set; }
        public string BlockDataHash { get; set; }
        public long BlockTimestamp { get; set; }
        public string ChannelId { get; set; }
    }
}
