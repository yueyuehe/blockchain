using QMBlockSDK.Idn;

namespace QMBlockSDK.TX
{
    public class TxRequest
    {
        public TxRequest()
        {
            Data = new TxData();
            Header = new TxHeader();
            Signer = new Signer();
        }

        public TxHeader Header { get; set; }

        public TxData Data { get; set; }
        /// <summary>
        /// 交易首次接受者 节点(由哪个节点率先接受消息对消息进行转发)
        /// </summary>
        public Signer Signer { get; set; }


    }
}
