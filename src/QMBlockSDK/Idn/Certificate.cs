using System;
using System.Collections.Generic;
using System.Text;

namespace QMBlockSDK.Idn
{
    public class Certificate
    {
        public TBSCertificate TBSCertificate { get; set; } = new TBSCertificate();
        //签名
        public string SignatureValue { get; set; }

        //检查证书数据的完整性
        public bool Check()
        {
            if (string.IsNullOrEmpty(TBSCertificate.Version))
            {
                return false;
            }
            if (string.IsNullOrEmpty(TBSCertificate.SerialNumber))
            {
                return false;
            }
            if (string.IsNullOrEmpty(TBSCertificate.Signature))
            {
                return false;
            }
            if (string.IsNullOrEmpty(TBSCertificate.Issuer))
            {
                return false;
            }
            if (TBSCertificate.NotBefore == 0)
            {
                return false;
            }
            if (TBSCertificate.NotAfter == 0)
            {
                return false;
            }
            if (string.IsNullOrEmpty(TBSCertificate.Subject))
            {
                return false;
            }
            if (string.IsNullOrEmpty(TBSCertificate.PublicKey))
            {
                return false;
            }
            return true;
        }
    }


    public class TBSCertificate
    {
        //证书版本号
        public string Version { get; set; }

        //证书序列号，对同一CA所颁发的证书，序列号唯一标识证书
        public string SerialNumber { get; set; }

        //证书签名算法标识
        public string Signature { get; set; }

        //证书颁发者，在区块链中身份(组织)
        public string Issuer { get; set; }

        //有效期开始
        public long NotBefore { get; set; }

        //有效期截止
        public long NotAfter { get; set; }

        //证书主体 主体在区块链中唯一 域名 www.org1.com
        public string Subject { get; set; }
        public CAType CAType { get; set; }
        //主体的公钥
        public string PublicKey { get; set; }
    }

    public enum CAType
    {
        //节点 用于自签名，并且raft网络中通讯
        Peer,
        //管理者，client端
        Admin,
        //一般用户，client端，tx交易
        User,
        //查询人员，只能进行查询操作
        Reader
    }
}

