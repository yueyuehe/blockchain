using QMBlockSDK.Idn;
using QMBlockSDK.TX;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.Peers;
using System;
using System.Collections.Generic;

namespace QMRaftCore.QMProvider
{
    public interface IConfigProvider
    {
        PeerIdentity GetPeerIdentity();
        long GetMinTimeout();
        List<IPeer> GetPeersExcludeSelf(string id);
        TimeSpan GetToCandidateTimeOut();
        IPeer GetPeer(string channelId, string leaderId);
        long GetHeartbeatTimeout();
        List<IPeer> GetEndorsePeer(string channelId, Chaincode chainCode);
        bool ValidateEndorse(string channelId, Chaincode chainCode, Dictionary<string, EndorseResponse> endorseDir);
        long GetEndorseTimeOut();
        List<IPeer> GetAllPeer(string channelId);
        /// <summary>
        /// 获取可公开的身份
        /// </summary>
        /// <returns></returns>
        PubliclyIdentity GetPublicIndentity();

        /// <summary>
        /// 获取私钥
        /// </summary>
        /// <returns></returns>
        string GetPrivateKey();

        TxRequest SignatureForTx(TxRequest tx);

        #region 区块生成配置

        int GetMaxTxCount();
        int GetBatchTimeout();
        #endregion


    }
}
