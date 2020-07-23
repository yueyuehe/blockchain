using QMBlockSDK.TX;

namespace QMBlockSDK.Ledger
{
    /// <summary>
    /// 交易提案
    /// </summary>
    public class Payload
    {
        public PayloadHeader Header { get; set; }
        public TransactionAction Action { get; set; }
    }
}
