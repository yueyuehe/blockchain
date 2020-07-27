using QMBlockSDK.TX;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.Peers;
using System.Collections.Generic;

namespace QMRaftCore.QMProvider
{
    public interface IPolicyProvider
    {
        /// <summary>
        /// 获取链码的背书策略
        /// </summary>
        /// <param name="chainCode"></param>
        /// <returns></returns>
        List<IPeer> GetEndorsePeer(Chaincode chainCode);
        bool ValidateEndorse(Chaincode chainCode, Dictionary<string, EndorseResponse> endorseDir);
    }
}
