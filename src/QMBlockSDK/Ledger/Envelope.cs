using QMBlockSDK.Idn;
using QMBlockSDK.TX;
using System.Collections.Generic;

namespace QMBlockSDK.Ledger
{
    public class Envelope
    {
        public Envelope()
        {
            TxReqeust = new TxRequest();
            PayloadReponse = new ProposaResponsePayload();
            Endorsements = new List<Signer>();
        }

        /// <summary>
        /// 交易提案
        /// </summary>
        public TxRequest TxReqeust { get; set; }

        public ProposaResponsePayload PayloadReponse { get; set; }

        /// <summary>
        /// 背书的签名
        /// </summary>
        public List<Signer> Endorsements { get; set; }

    }
}
