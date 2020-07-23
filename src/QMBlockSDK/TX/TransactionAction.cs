namespace QMBlockSDK.TX
{
    public class TransactionAction
    {
        /// <summary>
        /// 交易提案签名后的hash
        /// </summary>
        public byte[] Header { get; set; }

        /// <summary>
        /// 交易提案的返回结果，理论上所有节点返回的结果应该一致
        /// </summary>
        public ProposaResponsePayload PayloadReponse { get; set; }

        /// <summary>
        /// 背书的签名
        /// </summary>
        public Endorsement[] Endorsements { get; set; }

    }
}
