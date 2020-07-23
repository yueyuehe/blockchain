using QMBlockSDK.Idn;

namespace QMBlockSDK.TX
{
    public class Endorsement
    {
        public PubliclyIdentity Endorser { get; set; }
        public string Signature { get; set; }
    }
}
