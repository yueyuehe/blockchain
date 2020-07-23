using MongoDB.Driver;
using QMBlockSDK.Config;
using QMBlockSDK.Ledger;
using QMBlockSDK.MongoModel;
using QMRaftCore.Log;
using System.Threading.Tasks;

namespace QMRaftCore.Data
{
    /// <summary>
    /// 状态机  键值对查询  
    /// </summary>
    public interface IFiniteStateMachine
    {

        void Execute(Block block, IClientSessionHandle db = null);

        DataStatus GetConfig(string channelId, string ChancodeName, string key);

        ChannelConfig GetChannelConfig(string channelId);
    }
}