using QMBlockSDK.Idn;
using QMBlockSDK.TX;

namespace QMBlockSDK.CC
{
    public class ChaincodeProposalPayload
    {
        /// <summary>
        /// 交易提案请求头
        /// </summary>
        public TxRequest TxRequest { get; set; }

        /// <summary>
        /// 提案结果
        /// </summary>
        public TxReadWriteSet TxReadWriteSet { get; set; }
        public Signer Signer { get; set; }
    }
}
