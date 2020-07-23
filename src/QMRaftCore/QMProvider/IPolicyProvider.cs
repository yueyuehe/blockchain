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
        /// <param name="ChannelId"></param>
        /// <param name="chainCode"></param>
        /// <returns></returns>
        List<IPeer> GetEndorsePeer(string ChannelId, Chaincode chainCode);
        bool ValidateEndorse(string channelId, Chaincode chainCode, Dictionary<string, EndorseResponse> endorseDir);
    }
}
