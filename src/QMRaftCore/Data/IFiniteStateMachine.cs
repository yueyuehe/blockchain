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
        Task ExecuteAsync(Block block, IClientSessionHandle db = null);

        ChannelConfig GetChannelConfig();
    }
}