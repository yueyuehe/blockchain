using QMRaftCore.Concensus.Peers;
using System.Collections.Generic;
namespace QMRaftCore.Infrastructure
{

    public interface IPeersProvider
    {

        List<IPeer> Get();

        IPeer Get(string ip);

    }
}