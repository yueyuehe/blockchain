using MongoDB.Driver;
using QMBlockSDK.Config;
using QMBlockSDK.Ledger;
using QMRaftCore.Data.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using QMBlockSDK.MongoModel;
using MongoDB.Bson;
using QMBlockSDK.CC;
using Microsoft.Extensions.Caching.Memory;

namespace QMRaftCore.Data.Imp
{

    /// <summary>
    /// 数据管理者
    /// </summary>
    public class DataManager : IBlockDataManager, IFiniteStateMachine
    {
        private readonly string _channelId;
        //区块数据库
        private readonly IMongoDatabase _blockDatabase;
        //状态数据库
        private readonly IMongoDatabase _statusDatabase;
        //历史数据库
        private readonly IMongoDatabase _historyDatabase;
        //区块缓存
        private readonly IMemoryCache _cache;

        public DataManager(string channelId,
            IMemoryCache cache,
            IBlockDatabaseSettings blockSetting,
            IHistoryDatabaseSettings historySetting,
            IStatusDatabaseSettings statusSetting)
        {
            _cache = cache;
            _channelId = channelId;
            _blockDatabase = new MongoClient(blockSetting.ConnectionString).GetDatabase(blockSetting.DatabaseName + "_" + _channelId);
            _statusDatabase = new MongoClient(statusSetting.ConnectionString).GetDatabase(statusSetting.DatabaseName + "_" + _channelId);
            _historyDatabase = new MongoClient(historySetting.ConnectionString).GetDatabase(historySetting.DatabaseName + "_" + _channelId);
        }

        public IMongoDatabase GetStatusDB()
        {
            return this._statusDatabase;
        }


        public void ApplyBlock(Block block)
        {
            Execute(block);
        }

        public bool CacheBlock(Block block)
        {
            if (CheckBlock(block))
            {
                _cache.Set(ConfigKey.CahceBlock, block);
                return true;
            }
            else
            {
                return false;
            }
        }

        #region 校验区块 只有通过校验的区块才能加入缓存中

        private bool CheckBlock(Block block)
        {
            var lastblock = GetLastBlock(block.Data.Envelopes.First().TxReqeust.Data.Channel.ChannelId);
            //如果为空表示是 创世区块
            if (lastblock == null && block.Header.Number == 0)
            {
                return true;
            }
            //如果不等于null
            if (lastblock.Header.DataHash != block.Header.PreviousHash)
            {
                return false;
            }
            if (lastblock.Header.Number + 1 != block.Header.Number)
            {
                return false;
            }
            //if (lastblock.Header.Term > block.Header.Term)
            //    return false;
            //}
            //检查 签名
            return true;
        }

        #endregion

        public Block GetBlock(string channelId, long height)
        {
            var rs = GetBlockEntity(channelId, height);
            return rs?.ToBlock();

        }

        public MongoBlock GetBlockEntity(string channelId, long number)
        {
            if (channelId != _channelId)
            {
                return null;
            }
            var collenction = _blockDatabase.GetCollection<MongoBlock>(ConfigKey.BlockDocument);
            var filter = new BsonDocument("ChannelId", _channelId);
            return collenction.Find(p => p.Header.ChannelId == _channelId && p.Header.Number == number).FirstOrDefault();

        }


        public Block GetLastBlock(string channelId)
        {
            var rs = GetLastBlockEntity(channelId);
            return rs?.ToBlock();
        }

        public MongoBlock GetLastBlockEntity(string channelId)
        {
            if (channelId != _channelId)
            {
                return null;
            }
            var collection = _blockDatabase.GetCollection<MongoBlock>(ConfigKey.BlockDocument);
            return collection.AsQueryable().OrderByDescending(p => p.Header.Number).FirstOrDefault();
        }

        /// <summary>
        /// 上链 用于区块同步
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public async Task<bool> PutOnChainBlockAsync(Block block)
        {
            if (CacheBlock(block))
            {
                var cacheblock = _cache.Get<Block>(ConfigKey.CahceBlock);
                if (block.Header.DataHash == cacheblock.Header.DataHash)
                {
                    await CommitBlockAsync(block);
                    _cache.Remove(ConfigKey.CahceBlock);
                    return true;
                }
                return false;
            }
            return false;
        }

        private async Task CommitBlockAsync(Block block)
        {
            MongoBlock entity = new MongoBlock();
            //entity.Signer = Newtonsoft.Json.JsonConvert.SerializeObject(block.Signer);
            //entity.Data = Newtonsoft.Json.JsonConvert.SerializeObject(block.Data);
            //entity.DataHash = block.Header.DataHash;
            //entity.Number = block.Header.Number;
            //entity.Term = block.Header.Term;
            //entity.PreviousHash = block.Header.PreviousHash;
            //entity.Timestamp = block.Header.Timestamp;
            ////entity.Id = Guid.NewGuid().ToString();
            //entity.ChannelId = block.Data.Envelopes.First().TxReqeust.Data.Channel.ChannelId;
            entity.BindBlock(block);

            var collection = _blockDatabase.GetCollection<MongoBlock>(ConfigKey.BlockDocument);

            //var collectionTest = _blockDatabase.GetCollection<Block>(ConfigKey.BlockDocument);

            using (var session = _blockDatabase.Client.StartSession())
            {
                session.StartTransaction();
                //collection.InsertOne(session, entity);
                //Execute(block, session);
                await collection.InsertOneAsync(entity);
                Execute(block);
                //collectionTest.InsertOne(block);
                session.CommitTransaction();
            }
        }

        public bool PutOnChainBlock(string blockHash)
        {
            var cacheblock = _cache.Get<Block>(ConfigKey.CahceBlock);
            if (blockHash == cacheblock.Header.DataHash)
            {
                CommitBlockAsync(cacheblock);
                _cache.Remove(ConfigKey.CahceBlock);
                return true;
            }
            return false;
        }

        #region 状态机

        public void Execute(Block block, IClientSessionHandle session = null)
        {
            //把区块数据重新应用到 状态机
            /** 区分 document  按chaincodename进行document进行分类
            var dic = new Dictionary<string, List<DataStatus>>();

            foreach (var envelope in block.Data.Envelopes)
            {

                var list = new List<DataStatus>();
                if (dic.ContainsKey(envelope.TxReqeust.Header.ChaincodeName))
                {
                    list = dic[envelope.TxReqeust.Header.ChaincodeName];
                }
                else
                {
                    dic.Add(envelope.TxReqeust.Header.ChaincodeName, list);
                }



                var txid = envelope.TxReqeust.Data.TxId;

                //写集
                var writeSet = envelope.PayloadReponse.TxReadWriteSet.WriteSet;
                foreach (var set in writeSet)
                {
                    var model = new DataStatus();
                    model.Key = set.Key;
                    model.Value = set.Value;
                    model.Version = block.Header.Number + "_" + envelope.TxReqeust.Data.TxId;
                    model.Deleted = false;
                    list.Add(model);
                }
                //删除集
                var del = envelope.PayloadReponse.TxReadWriteSet.DeletedSet;
                foreach (var set in del)
                {
                    var model = new DataStatus();
                    model.Key = set.Key;
                    model.Version = block.Header.Number + "_" + envelope.TxReqeust.Data.TxId;
                    model.Deleted = true;
                    list.Add(model);
                }
            }

            foreach (var readWrite in dic)
            {
                var doc = _statusDatabase.GetCollection<DataStatus>(readWrite.Key);
                foreach (var item in readWrite.Value)
                {
                    var filter = new BsonDocument("key", item.Key);
                    var elements = new List<BsonElement>() {
                        new BsonElement(nameof(item.Value),item.Value) ,
                        new BsonElement(nameof(item.Version),item.Version),
                        new BsonElement(nameof(item.Deleted),item.Deleted),
                    };
                    var update = new BsonDocument("$set", new BsonDocument(elements));
                    //获取 链码的doc文档
                    await doc.UpdateOneAsync(filter, update, new UpdateOptions() { IsUpsert = true });
                }
            }
        */

            #region 区块数据重新应用到 状态机 不区分chaincodename的document

            var list = new List<DataStatus>();
            foreach (var envelope in block.Data.Envelopes)
            {
                var txid = envelope.TxReqeust.Data.TxId;
                //写集
                var writeSet = envelope.PayloadReponse.TxReadWriteSet.WriteSet;
                foreach (var set in writeSet)
                {
                    var model = new DataStatus();
                    model.Key = set.Key;
                    model.Value = set.Value;
                    model.Version = block.Header.Number + "_" + envelope.TxReqeust.Data.TxId;
                    model.Deleted = false;
                    list.Add(model);
                }
                //删除集
                var del = envelope.PayloadReponse.TxReadWriteSet.DeletedSet;
                foreach (var set in del)
                {
                    var model = new DataStatus();
                    model.Key = set.Key;
                    model.Version = block.Header.Number + "_" + envelope.TxReqeust.Data.TxId;
                    model.Deleted = true;
                    list.Add(model);
                }
            }

            var doc = _statusDatabase.GetCollection<DataStatus>(ConfigKey.DataStatusDocument);
            if (session == null)
            {
                using (session = _statusDatabase.Client.StartSession())
                {
                    session.StartTransaction();
                    foreach (var item in list)
                    {
                        var filter = new BsonDocument("Key", item.Key);
                        var elements = new List<BsonElement>() {
                        new BsonElement(nameof(item.Value),item.Value) ,
                        new BsonElement(nameof(item.Version),item.Version),
                        new BsonElement(nameof(item.Deleted),item.Deleted),
                    };
                        var update = new BsonDocument("$set", new BsonDocument(elements));
                        //获取 链码的doc文档
                        //doc.UpdateOne(session, filter, update, new UpdateOptions() { IsUpsert = true });
                        //mongodb 需要搭建集群才支持事务 先测试
                        doc.UpdateOne(filter, update, new UpdateOptions() { IsUpsert = true });

                    }
                    // if an exception is thrown before reaching here the transaction will be implicitly aborted
                    session.CommitTransaction();
                }
            }
            else
            {
                foreach (var item in list)
                {
                    var filter = new BsonDocument("Key", item.Key);
                    var elements = new List<BsonElement>() {
                        new BsonElement(nameof(item.Value),item.Value) ,
                        new BsonElement(nameof(item.Version),item.Version),
                        new BsonElement(nameof(item.Deleted),item.Deleted),
                    };
                    var update = new BsonDocument("$set", new BsonDocument(elements));
                    //获取 链码的doc文档
                    //doc.UpdateOne(session, filter, update, new UpdateOptions() { IsUpsert = true });
                    doc.UpdateOne(filter, update, new UpdateOptions() { IsUpsert = true });
                }
            }

            #endregion

        }

        public ChannelConfig GetChannelConfig(string channelId)
        {
            var rs = GetConfig(_channelId, "", ConfigKey.Channel);
            if (rs != null)
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<ChannelConfig>(rs.Value);
            }
            return null;
        }

        public DataStatus GetConfig(string channelId, string ChancodeName, string key)
        {
            var collection = _statusDatabase.GetCollection<DataStatus>(ConfigKey.DataStatusDocument);

            var all = collection.Find(p => true).ToList();


            return collection.Find(p => p.Key == key).FirstOrDefault();
        }

        #endregion

        /// <summary>
        /// 获取所有的通道
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static List<string> GetChannels(String connectionString, string prveblockDBName)
        {
            var client = new MongoClient(connectionString);
            using (var cursor = client.ListDatabaseNames())
            {
                var list = cursor.ToList();
                return list.Where(p => p.Split('_')[0] == prveblockDBName).Select(p => p.Substring(p.IndexOf("_") + 1)).ToList();
            }
        }

    }
}