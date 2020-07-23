using QMBlockSDK.Idn;

namespace QMBlockSDK.Ledger
{
    public class PayloadHeader
    {
        public ChannelHeader ChannelHeader { get; set; }
        public ChainCodeHeader ChainCodeHeader { get; set; }

        /// <summary>
        /// 对ChannelHeader 和 ChainCodeHeader 进行签名 由交易发起方签名
        /// </summary>
        public Signer Signer { get; set; }
    }
}
