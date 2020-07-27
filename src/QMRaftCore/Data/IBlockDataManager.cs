using QMBlockSDK.Ledger;
using QMBlockSDK.MongoModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QMRaftCore.Data
{

    public interface IBlockDataManager
    {
        /// <summary>
        /// 获取最新的区块
        /// </summary>
        /// <returns></returns>
        Block GetLastBlock(string channelId);

        Task<bool> PutOnChainBlockAsync(Block block);

        Task<bool> PutOnChainBlockAsync(string blockHash);

        bool CacheBlock(Block block);

        /// <summary>
        /// 状态机运用区块数据
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        void ApplyBlock(Block block);

        Block GetBlock(string channelId, long height);

        MongoBlock GetLastBlockEntity(string ChannelID);

        MongoBlock GetBlockEntity(string channelID, long number);
    }
}
