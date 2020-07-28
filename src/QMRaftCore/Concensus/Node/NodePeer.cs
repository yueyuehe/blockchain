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
        /// ����node
        /// </summary>
        public async Task StartNodesAsync()
        {
            _memoryCache.Set(CacheKeys.NodePeer, this);
            try
            {
                //����ͨ������
                //��ȡ�ڵ������  
                //��ȡ����ͨ�� 
                var channels = DataManager.GetChannels(_blockDatabaseSetting.ConnectionString, _blockDatabaseSetting.DatabaseName);
                //����ͨ�� ��ͨ���е��������Ӧ�� 
                foreach (var item in channels)
                {
                    //����ͨ��
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
        /// Ӧ��ͨ������������� ����ͨ��
        /// </summary>
        /// <param name="channelid"></param>
        /// <returns></returns>
        public async Task<TxResponse> StartChannel(string channelid)
        {
            //�ж�ͨ���Ƿ����
            if (_nodes.ContainsKey(channelid))
            {
                throw new Exception($"ͨ��{channelid}�Ѿ�����");
            }
            //�жϽڵ�����Ƿ�����
            var identity = _identityProvider.GetPeerIdentity();
            var checkRs = identity.Valid();
            if (!checkRs)
            {
                throw new Exception("֤��У��ʧ��");
            }

            var nodeDataManager = new DataManager(channelid, _memoryCache, _blockDatabaseSetting, _historyDatabaseSettings, _statusDatabaseSettings);
            var node = new Node(channelid, _loggerFactory, _assemblyProvider, _identityProvider, nodeDataManager, _mq);
            var url = _identityProvider.GetPeerIdentity().Address;
            node.Start(new NodeId(url));
            _nodes.Add(channelid, node);
            //��ȡ��������
            var lastblock = nodeDataManager.GetLastBlock(channelid);
            //���û������ �����
            if (lastblock != null)
            {
                //�ӵ�һ������������������� Ӧ����������ݵ�״̬��
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
        /// ����һ����ͨ�� �ڱ������ݿ�
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
            ///�����µ�ͨ��
            return rs;
        }

        /// <summary>
        /// �ڵ����ĳһ��ͨ��
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public async Task<TxResponse> JoinChannel(string channelId)
        {
            return await StartChannel(channelId);
        }
    }
}


