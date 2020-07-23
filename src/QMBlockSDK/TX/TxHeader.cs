using QMBlockSDK.Idn;

namespace QMBlockSDK.TX
{
    public class TxHeader
    {
        public string ChannelId { get; set; }

        public string ChaincodeName { get; set; }

        public string FuncName { get; set; }

        public string[] Args { get; set; }

        public TxType Type { get; set; }

        /// <summary>
        /// 交易发起人 （用户)
        /// </summary>
        public Signer Signer { get; set; }

    }
}
