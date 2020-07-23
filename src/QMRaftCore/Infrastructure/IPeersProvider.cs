using QMRaftCore.Concensus.Peers;
using System.Collections.Generic;
namespace QMRaftCore.Infrastructure
{

    public interface IPeersProvider
    {

        List<IPeer> Get(string channelId);

        IPeer GetById(string channelid, string peerId);

        IPeer GetByIp(string ip);
    }
}