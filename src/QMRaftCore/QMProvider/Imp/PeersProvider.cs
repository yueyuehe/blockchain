using Microsoft.EntityFrameworkCore;
using QMBlockSDK.CC;
using QMBlockSDK.Config;
using QMRaftCore.Concensus.Peers;
using QMRaftCore.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace QMRaftCore.QMProvider.Imp
{
    public class PeersProvider : IPeersProvider
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly DbContextOptions<BlockContext> _options;

        public PeersProvider(DbContextOptions<BlockContext> options, IHttpClientFactory clientFactory)
        {
            //_db = new BlockContext(option);
            _options = options;
            _clientFactory = clientFactory;
        }

        public List<IPeer> Get(string channelId)
        {
            var peers = new List<IPeer>();
            using (var _db = new BlockContext(_options))
            {
                var model = _db.KeyValueData.Where(p => p.ChannelId == channelId && p.Key == ConfigKey.Channel).SingleOrDefault();
                var channelConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<ChannelConfig>(model.Value);
                var list = new List<string>();
                foreach (var item in channelConfig.OrgConfigs)
                {
                    var url = item.OrgPeer.Where(p => p.Anchor == true).Select(p => p.Url).FirstOrDefault();
                    if (url != null)
                    {
                        list.Add(url);
                    }
                }

                foreach (var item in list)
                {
                    peers.Add(new Peer(_clientFactory) { Id = item });
                }
                return peers;
            }

        }

        public IPeer GetById(string channelid, string peerId)
        {
            return Get(channelid).Where(p => p.Id == peerId).SingleOrDefault();
        }

        public IPeer GetByIp(string ip)
        {
            return new Peer(_clientFactory) { Id = ip };
        }
    }
}
