using QMBlockSDK.Idn;
using QMBlockSDK.TX;

namespace QMRaftCore.Concensus.Messages
{
    public class EndorseResponse
    {
        public EndorseResponse()
        {
            Endorsement = new Signer();
            TxReadWriteSet = new TxReadWriteSet();
        }
        public bool Status { get; set; }
        public string Msg { get; set; }
        public TxReadWriteSet TxReadWriteSet { get; set; }
        public Signer Endorsement { get; set; }
    }
}
