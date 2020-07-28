using QMBlockSDK.Idn;
using QMBlockSDK.TX;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.Peers;
using QMRaftCore.Msg.Model;
using System;
using System.Collections.Generic;

namespace QMRaftCore.QMProvider
{
    public interface IConfigProvider
    {
        MQSetting GetMQSetting();
        PeerIdentity GetPeerIdentity();
        long GetMinTimeout();
        List<IPeer> GetPeersExcludeSelf(string id);
        TimeSpan GetToCandidateTimeOut();
        IPeer GetPeer(string leaderId);
        long GetHeartbeatTimeout();
        List<IPeer> GetEndorsePeer(Chaincode chainCode);
        bool ValidateEndorse(Chaincode chainCode, Dictionary<string, EndorseResponse> endorseDir);
        long GetEndorseTimeOut();
        List<IPeer> GetAllPeer();
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
