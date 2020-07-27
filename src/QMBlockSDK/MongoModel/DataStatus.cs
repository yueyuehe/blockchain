using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace QMBlockSDK.MongoModel
{
    public class DataStatus
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Chaincode { get; set; }
        public string Key { get; set; }

        public string Data { get; set; }

        public long BlockNumber { get; set; }
        public string TxId { get; set; }
    }
}
