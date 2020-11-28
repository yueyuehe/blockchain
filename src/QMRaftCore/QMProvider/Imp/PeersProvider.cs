using Microsoft.Extensions.Logging;
using QMBlockSDK.CC;
using QMBlockSDK.Config;
using QMRaftCore.Concensus.Peers;
using QMRaftCore.Data.Imp;
using QMRaftCore.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace QMRaftCore.QMProvider.Imp
{
    public class PeersProvider : IPeersProvider
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly DataManager _dataManager;
        private readonly ILogger<PeersProvider> _logger;
        private Dictionary<string, IPeer> _peerList;
        public PeersProvider(ILoggerFactory factory, DataManager dataManager, IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
            _dataManager = dataManager;
            _logger = factory.CreateLogger<PeersProvider>();
            _peerList = new Dictionary<string, IPeer>();
        }

        //获取通道中所有节点
        public List<IPeer> Get()
        {
            var config = _dataManager.GetChannelConfig();
            var list = config.OrgConfigs.Select(p => p.Address).ToList();
            var rsList = new List<IPeer>();
            foreach (var item in list)
            {
                //如果不存在
                if (!_peerList.ContainsKey(item))
                {
                    var client = new Peer(_clientFactory, item);
                    _peerList.Add(item, client);
                }
                rsList.AddRange(_peerList.Where(p => p.Key == item).Select(p => p.Value));
            }
            return rsList;
        }

        public IPeer Get(string peerId)
        {
            if (_peerList.ContainsKey(peerId))
            {
                return _peerList[peerId];
            }
            else
            {
                var client = new Peer(_clientFactory, peerId);
                _peerList.Add(peerId, client);
                return _peerList[peerId];
            }
        }
    }
}
