namespace QMBlockSDK.TX
{
    public class TxData
    {
        public TxData()
        {
            Channel = new Channel();
        }
        public string TxId { get; set; }
        public long Timestamp { get; set; }
        /// <summary>
        /// 交易类型
        /// </summary>
        public TxType Type { get; set; }
        public Channel Channel { get; set; }
    }
}
