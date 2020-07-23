using QMBlockSDK.CC;
using QMBlockSDK.Config;
using QMBlockSDK.Idn;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace QMRaftCore.BlockChain.Chaincode.Imp
{


    public class IdentityChaincode : IChaincode
    {
        public ChainCodeInvokeResponse Init(IChaincodeStub stub)
        {
            throw new NotImplementedException();
        }

        public ChainCodeInvokeResponse Invoke(IChaincodeStub stub)
        {
            try
            {
                var funcname = stub.GetFunction();
                switch (funcname)
                {
                    //将身份信息注册到区块链中
                    case ConfigKey.RegistMember:
                        return RegistMember(stub);
                    //初始化链码 把init链码配置
                    case ConfigKey.InitChaincodeFunc:
                        return InitChaincode(stub);
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                return stub.Response(ex.Message, StatusCode.BAD_OTHERS);
            }
        }



        public ChainCodeInvokeResponse Query(IChaincodeStub stub)
        {
            throw new NotImplementedException();
        }


        #region 具体实现类

        //将身份信息注册到通道中
        private ChainCodeInvokeResponse RegistMember(IChaincodeStub stub)
        {
            var args = stub.GetArgs();
            if (args.Length != 1)
            {
                return stub.Response("", StatusCode.BAD_ARGS_NUMBER);
            }
            //获取ca
            var ca = Newtonsoft.Json.JsonConvert.DeserializeObject<Certificate>(args[0]);
            //获取配置
            var chainconfig = stub.GetState<ChannelConfig>(ConfigKey.Channel);
            if (!ca.Check())
            {
                return stub.Response("CA证书校验失败", StatusCode.BAD_OTHERS);
            }
            //获取组织
            var org = chainconfig.OrgConfigs.Where(p => p.OrgId == ca.TBSCertificate.Issuer).FirstOrDefault();
            if (org == null)
            {
                return stub.Response("通道不存在" + ca.TBSCertificate.Issuer, StatusCode.BAD_OTHERS);
            }
            //判断组织中是否已经注册了该账号
            //org.OrgMember.Where(p=>p.Certificate.TBSCertificate)


            throw new NotImplementedException();
        }
        #endregion
    }
}
