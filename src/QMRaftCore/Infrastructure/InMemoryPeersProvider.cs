using System.Collections.Generic;
using QMRaftCore.Concensus.Peers;
using System.Linq;
namespace QMRaftCore.Infrastructure
{

    public class InMemoryPeersProvider : IPeersProvider
    {
        private List<IPeer> _peers;

        public InMemoryPeersProvider(List<IPeer> peers)
        {
            _peers = peers;
        }

        public List<IPeer> Get()
        {
            return _peers;
        }

        public List<IPeer> Get(string channelId)
        {
            throw new System.NotImplementedException();
        }

        public IPeer GetById(string peerId)
        {
            return _peers.Where(p => p.Id == peerId).FirstOrDefault();
        }

        public IPeer GetById(string channelid, string peerId)
        {
            throw new System.NotImplementedException();
        }
    }
}