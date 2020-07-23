using Microsoft.EntityFrameworkCore;
using QMBlockSDK.CC;
using QMBlockSDK.Config;
using QMBlockSDK.DAL;
using QMBlockSDK.Ledger;
using QMRaftCore.FiniteStateMachine;
using QMRaftCore.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QMRaftCore.Data.Imp
{
    public class DBStateMachine : IFiniteStateMachine
    {
        //private readonly BlockContext _db;
        private readonly DbContextOptions<BlockContext> _options;
        public DBStateMachine(DbContextOptions<BlockContext> options)
        {
            _options = options;
        }

        public async Task Execute(Block block, BlockContext db = null)
        {
            using (var _db = new BlockContext(_options))
            {
                BlockContext currentdb = null;
                currentdb = _db;
                if (db != null)
                {
                    currentdb = db;
                }

                var list = new List<KeyValueData>();
                foreach (var envelope in block.Data.Envelopes)
                {
                    var txid = envelope.TxReqeust.Data.TxId;
                    var writeSet = envelope.PayloadReponse.TxReadWriteSet.WriteSet;
                    foreach (var set in writeSet)
                    {
                        var model = new KeyValueData();
                        model.Key = set.Key;
                        model.Value = set.Value;
                        model.Version = block.Header.Number + "_" + envelope.TxReqeust.Data.TxId;
                        model.ChannelId = envelope.TxReqeust.Data.Channel.ChannelId;
                        model.Deleted = false;
                        list.Add(model);
                    }
                    var del = envelope.PayloadReponse.TxReadWriteSet.DeletedSet;
                    foreach (var set in del)
                    {
                        var model = new KeyValueData();
                        model.Key = set.Key;
                        model.Version = block.Header.Number + "_" + envelope.TxReqeust.Data.TxId;
                        model.ChannelId = envelope.TxReqeust.Data.Channel.ChannelId;
                        model.Deleted = true;
                        list.Add(model);
                    }
                }

                foreach (var item in list)
                {
                    var model = currentdb.KeyValueData.Where(p => p.ChannelId == item.ChannelId && p.Key == item.Key).FirstOrDefault();
                    if (model == null)
                    {
                        currentdb.Add(item);
                    }
                    else
                    {
                        model.Value = item.Value;
                        model.Version = item.Version;
                        currentdb.Update(model);
                    }
                }

                await currentdb.SaveChangesAsync();

            }

        }

        public Task<KeyValueData> GetConfig(string channelId, string chaincodeName, string key)
        {
            using (var _db = new BlockContext(_options))
            {
                var rs = _db.KeyValueData.Where(p => p.Key == key && p.ChannelId == channelId).FirstOrDefault();
                return Task.FromResult(rs);
            }
        }

        public ChannelConfig GetChannelConfig(string channelId)
        {
            using (var _db = new BlockContext(_options))
            {
                var model = _db.KeyValueData.Where(p => p.ChannelId == channelId && p.Key == ConfigKey.Channel).FirstOrDefault();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<ChannelConfig>(model.Value);
            }

        }


    }
}
