using QMBlockSDK.Idn;

namespace QMRaftCore.QMProvider
{
    public interface IIdentityProvider
    {
        //节点身份信息提供
        PeerIdentity GetPeerIdentity();
        string GetPublicKey();
        string GetPrivateKey();
        string GetAddress();

        string GetCAUserName();

        string GetCAPassword();


    }
}
