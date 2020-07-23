using QMBlockUtils;
using System;

namespace QMBlockSDK.Idn
{
    /// <summary>
    /// 节点身份 用于签名的
    /// </summary>
    public class PeerIdentity : PubliclyIdentity
    {
        public string PrivateKey { get; set; }
        public PubliclyIdentity GetPublic()
        {
            var model = new PubliclyIdentity();
            model.Certificate = this.Certificate;
            model.OrgId = this.OrgId;
            model.Address = this.Address;
            return model;
        }

        public bool Valid()
        {
            try
            {
                var str = RSAHelper.SignData(this.PrivateKey, GetPublic().Certificate.TBSCertificate);
                if (str == GetPublic().Certificate.SignatureValue)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
