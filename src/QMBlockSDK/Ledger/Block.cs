using QMBlockSDK.Idn;

namespace QMBlockSDK.Ledger
{
    public class Block
    {
        public Block()
        {
            Data = new BlockData();
            Header = new BlockHeader();
            Signer = new Signer();
        }
        public BlockHeader Header { get; set; }
        public BlockData Data { get; set; }
        public Signer Signer { get; set; }

    }
}
