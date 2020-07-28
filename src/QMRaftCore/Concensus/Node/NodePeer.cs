using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QMBlockSDK.CC;
using QMBlockSDK.Idn;
using QMBlockSDK.TX;
using QMRaftCore.Data.Imp;
using QMRaftCore.Data.Model;
using QMRaftCore.Infrastructure;
using QMRaftCore.Msg.Model;
using QMRaftCore.QMProvider;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QMRaftCore.Concensus.Node
{
    public class NodePeer : INodeProvider
    {
        private readonly IDictionary<string, INode> _nodes = new Dictionary<string, INode>();
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<NodePeer> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly IBlockDatabaseSettings _blockDatabaseSetting;
        private readonly IHistoryDatabaseSettings _historyDatabaseSettings;
        private readonly IStatusDatabaseSettings _statusDatabaseSettings;
        private readonly IIdentityProvider _identityProvider;
        private readonly IAssemblyProvider _assemblyProvider;

        private readonly MQSetting _mq;


        public NodePeer(
             ILoggerFactory loggerFactory,
             IMemoryCache memoryCache,
             IBlockDatabaseSettings blockSetting,
             IHistoryDatabaseSettings historySetting,
             IStatusDatabaseSettings statusSetting,
             IAssemblyProvider assemblyProvider,
             IIdentityProvider identityProvider,
             MQSetting mq
            )
        {
            _mq = mq;
            _identityProvider = identityProvider;
            _assemblyProvider = assemblyProvider;
            _blockDatabaseSetting = blockSetting;
            _historyDatabaseSettings = historySetting;
            _statusDatabaseSettings = statusSetting;
            _memoryCache = memoryCache;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<NodePeer>();
        }
        public INode GetNode(String channelID)
        {
            if (_nodes.ContainsKey(channelID))
            {
                return _nodes[channelID];
            }
            return null;
        }

        public bool AppendNode(string channelId, INode node)
        {
            if (_nodes.ContainsKey(channelId))
            {
                return false;
            }
            _nodes.Add(channelId, node);
            return true;
        }

        /// <summary>
        /// 启动node
        /// </summary>
        public async Task StartNodesAsync()
        {
            _memoryCache.Set(CacheKeys.NodePeer, this);
            try
            {
                //网络通道配置
                //获取节点的区块  
                //获取所有通道 
                var channels = DataManager.GetChannels(_blockDatabaseSetting.ConnectionString, _blockDatabaseSetting.DatabaseName);
                //遍历通道 对通道中的区块进行应用 
                foreach (var item in channels)
                {
                    //启动通道
                    await StartChannel(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw ex;
            }
        }

        /// <summary>
        /// 应用通道中区块的数据 启动通道
        /// </summary>
        /// <param name="channelid"></param>
        /// <returns></returns>
        public async Task<TxResponse> StartChannel(string channelid)
        {
            //判断通道是否存在
            if (_nodes.ContainsKey(channelid))
            {
                throw new Exception($"通道{channelid}已经存在");
            }
            //判断节点身份是否完整
            var identity = _identityProvider.GetPeerIdentity();
            var checkRs = identity.Valid();
            if (!checkRs)
            {
                throw new Exception("证书校验失败");
            }

            var nodeDataManager = new DataManager(channelid, _memoryCache, _blockDatabaseSetting, _historyDatabaseSettings, _statusDatabaseSettings);
            var node = new Node(channelid, _loggerFactory, _assemblyProvider, _identityProvider, nodeDataManager, _mq);
            var url = _identityProvider.GetPeerIdentity().Address;
            node.Start(new NodeId(url));
            _nodes.Add(channelid, node);
            //获取最新区块
            var lastblock = nodeDataManager.GetLastBlock(channelid);
            //如果没有区块 则结束
            if (lastblock != null)
            {
                //从第一个区块遍历到最后的区块 应用区块的数据到状态机
                for (var i = 0; i <= lastblock.Header.Number; i++)
                {
                    var block = nodeDataManager.GetBlock(channelid, i);
                    nodeDataManager.ApplyBlock(block);
                }
            }

            return new TxResponse()
            {
                Status = true
            };
        }

        /// <summary>
        /// 创建一个新通道 在本地数据库
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public async Task<TxResponse> InitChannel(string channelId)
        {
            var nodeDataManager = new DataManager(channelId, _memoryCache, _blockDatabaseSetting, _historyDatabaseSettings, _statusDatabaseSettings);
            var node = new Node(channelId, _loggerFactory, _assemblyProvider, _identityProvider, nodeDataManager, _mq);
            var rs = await node.CreateNewChannel(channelId);
            if (rs.Status)
            {
                await StartChannel(channelId);
            }
            ///创建新的通道
            return rs;
        }

        /// <summary>
        /// 节点加入某一个通道
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public async Task<TxResponse> JoinChannel(string channelId)
        {
            return await StartChannel(channelId);
        }
    }
}


