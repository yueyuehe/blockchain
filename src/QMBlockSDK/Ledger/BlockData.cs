using System.Collections.Generic;

namespace QMBlockSDK.Ledger
{
    public class BlockData
    {
        public BlockData()
        {
            Envelopes = new List<Envelope>();
        }
        public List<Envelope> Envelopes { get; set; }
    }
}
