using QMBlockSDK.Idn;
using QMBlockSDK.Ledger;

namespace QMBlockSDK.DAL
{
    public class BlockEntity
    {
        public string Id { get; set; }
        public string ChannelId { get; set; }
        public long Number { get; set; }
        public long Term { get; set; }
        public string PreviousHash { get; set; }
        public string DataHash { get; set; }
        public long Timestamp { get; set; }
        public string Data { get; set; }
        public string Signer { get; set; }


        public Block ToBlock()
        {
            var block = new Block();
            block.Data = Newtonsoft.Json.JsonConvert.DeserializeObject<BlockData>(Data);
            block.Signer = Newtonsoft.Json.JsonConvert.DeserializeObject<Signer>(Signer);
            block.Header.DataHash = DataHash;
            block.Header.Number = Number;
            block.Header.Term = Term;
            block.Header.PreviousHash = PreviousHash;
            block.Header.Timestamp = Timestamp;
            return block;
        }
    }
}
