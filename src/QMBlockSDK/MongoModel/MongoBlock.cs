using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using QMBlockSDK.Idn;
using QMBlockSDK.Ledger;
using System;
using System.Collections.Generic;
using System.Text;

namespace QMBlockSDK.MongoModel
{
    public class MongoBlock : Block
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        //public string ChannelId { get; set; }
        //public long Number { get; set; }
        //public long Term { get; set; }
        //public string PreviousHash { get; set; }
        //public string DataHash { get; set; }
        //public long Timestamp { get; set; }
        //public string Data { get; set; }
        //public string Signer { get; set; }


        public Block ToBlock()
        {
            return this as Block;
            var block = new Block();
            //block.Data = Newtonsoft.Json.JsonConvert.DeserializeObject<BlockData>(Data);
            //block.Signer = Newtonsoft.Json.JsonConvert.DeserializeObject<Signer>(Signer);
            //block.Header.DataHash = DataHash;
            //block.Header.Number = Number;
            //block.Header.Term = Term;
            //block.Header.PreviousHash = PreviousHash;
            //block.Header.Timestamp = Timestamp;
            return block;
        }

        public void BindBlock(Block block)
        {
            this.Data = block.Data;
            this.Header = block.Header;
            this.Signer = block.Signer;
        }
    }
}
