using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QMBlockSDK.CC;
using QMBlockSDK.DAL;
using QMBlockSDK.Ledger;
using QMRaftCore.FiniteStateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QMRaftCore.Data.Imp
{
    public class BlockDataManager : IBlockDataManager
    {
        //应该是每一个 通道有一个专属的  Blockmanager
        //blockmanager需要使用key-value 数据库提高性能

        private readonly IFiniteStateMachine _finiteStateMachine;
        private readonly DbContextOptions<BlockContext> _options;
        private readonly IMemoryCache _cache;

        public BlockDataManager(DbContextOptions<BlockContext> options,
            IFiniteStateMachine finiteStateMachine, IMemoryCache cache)
        {
            _options = options;
            //_db = new BlockContext(option);
            _finiteStateMachine = finiteStateMachine;
            _cache = cache;
        }

        public async Task ApplyBlock(Block block)
        {
            await _finiteStateMachine.Execute(block);
        }

        /// <summary>
        /// 缓存block
        /// </summary>
        /// <param name="block"></param>
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

        private bool CheckBlock(Block block)
        {
            var lastblock = GetLastBlock(block.Data.Envelopes.First().TxReqeust.Data.Channel.ChannelId);
            //如果为空表示是 创世区块
            if (lastblock == null)
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
            //{
            //    return false;
            //}
            //检查 签名
            return true;
        }

        private bool CheckBlock(BlockEntity block)
        {
            return CheckBlock(block.ToBlock());
        }


        public async Task<bool> PutOnChainBlock(Block block)
        {
            if (CacheBlock(block))
            {
                var cacheblock = _cache.Get<Block>(ConfigKey.CahceBlock);
                if (block.Header.DataHash == cacheblock.Header.DataHash)
                {
                    await CommitBlock(block);
                    _cache.Remove(ConfigKey.CahceBlock);
                    return true;
                }
                return false;
            }
            return false;
        }

        public async Task<bool> PutOnChainBlock(string blockhash)
        {
            var cacheblock = _cache.Get<Block>(ConfigKey.CahceBlock);
            if (blockhash == cacheblock.Header.DataHash)
            {
                await CommitBlock(cacheblock);
                _cache.Remove(ConfigKey.CahceBlock);
                return true;
            }
            return false;
        }

        private async Task CommitBlock(Block block)
        {
            BlockEntity entity = new BlockEntity();
            entity.Signer = Newtonsoft.Json.JsonConvert.SerializeObject(block.Signer);
            entity.Data = Newtonsoft.Json.JsonConvert.SerializeObject(block.Data);
            entity.DataHash = block.Header.DataHash;
            entity.Number = block.Header.Number;
            entity.Term = block.Header.Term;
            entity.PreviousHash = block.Header.PreviousHash;
            entity.Timestamp = block.Header.Timestamp;
            entity.Id = Guid.NewGuid().ToString();
            entity.ChannelId = block.Data.Envelopes.First().TxReqeust.Data.Channel.ChannelId;

            using (var _db = new BlockContext(_options))
            {
                _db.BlockEntity.Add(entity);
                await _finiteStateMachine.Execute(block, _db);
            }
        }

        private async Task CommitBlock(BlockEntity block)
        {
            using (var _db = new BlockContext(_options))
            {
                _db.BlockEntity.Add(block);
                await _finiteStateMachine.Execute(block.ToBlock(), _db);
            }
        }

        public List<string> GetChannels()
        {
            using (var _db = new BlockContext(_options))
            {
                return _db.BlockEntity.Select(p => p.ChannelId).Distinct().ToList();
            }
        }

        public Block GetLastBlock(string channelId)
        {
            using (var _db = new BlockContext(_options))
            {
                if (_db.BlockEntity.Where(p => p.ChannelId == channelId).Any())
                {
                    var number = _db.BlockEntity.Where(p => p.ChannelId == channelId).Max(p => p.Number);
                    var entity = _db.BlockEntity.Where(p => p.ChannelId == channelId && p.Number == number).FirstOrDefault();
                    return entity.ToBlock();
                }
                else
                {
                    return null;
                }
            }
        }

        public Block GetBlock(string channelId, long height)
        {
            using (var _db = new BlockContext(_options))
            {
                var entity = _db.BlockEntity.Where(p => p.ChannelId == channelId && p.Number == height).FirstOrDefault();
                return entity == null ? null : entity.ToBlock();
            }
        }

        public BlockEntity GetLastBlockEntity(string channelID)
        {
            using (var _db = new BlockContext(_options))
            {
                if (_db.BlockEntity.Any(p => p.ChannelId == channelID))
                {
                    var number = _db.BlockEntity.Where(p => p.ChannelId == channelID).Max(p => p.Number);
                    return _db.BlockEntity.Where(p => p.ChannelId == channelID && p.Number == number).FirstOrDefault();
                }
                return null;
            }
        }

        /// <summary>
        /// 获取指定高度的区块
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public BlockEntity GetBlockEntity(string channelID, long number)
        {
            using (var _db = new BlockContext(_options))
            {
                return _db.BlockEntity.Where(p => p.ChannelId == channelID && p.Number == number).FirstOrDefault();
            }
        }
    }
}
