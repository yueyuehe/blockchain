using Microsoft.Extensions.Configuration;
using QMBlockSDK.Idn;

namespace QMRaftCore.QMProvider.Imp
{
    public class IdentityProvider : IIdentityProvider
    {
        private readonly IConfiguration _configuration;
        private readonly UserAccount _userAccount;
        private readonly CaAccount _caAccount;
        private readonly PeerIdentity _peerIdentity;


        public IdentityProvider(IConfiguration configuration,
            UserAccount userAccount,
            CaAccount caAccount,
            PeerIdentity peerIdentity)
        {
            _peerIdentity = peerIdentity;
            _caAccount = caAccount;
            _userAccount = userAccount;
            _configuration = configuration;
        }
        public PeerIdentity GetPeerIdentity()
        {
            return _peerIdentity;
        }

        public string GetPrivateKey()
        {
            return _peerIdentity.PrivateKey;
        }

        public string GetPublicKey()
        {
            return _peerIdentity.GetPublic().Certificate?.TBSCertificate?.PublicKey;
        }

        public string GetAddress()
        {
            return _peerIdentity.Address;
        }

        public string GetCAUserName()
        {
            return _caAccount.Username;
        }

        public string GetCAPassword()
        {
            return _caAccount.Password;
        }
    }
}
