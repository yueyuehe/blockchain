using QMBlockSDK.CC;
using QMBlockSDK.Config;
using QMBlockSDK.Idn;
using QMBlockUtils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QMRaftCore.BlockChain.Chaincode.Imp
{
    public class NetConfigChaincode : IChaincode
    {
        public ChainCodeInvokeResponse Invoke(IChaincodeStub stub)
        {
            try
            {
                var funcname = stub.GetFunction();
                switch (funcname)
                {
                    case ConfigKey.InitChannelFunc:
                        return CreateNewChannel(stub);
                    case ConfigKey.AddOrgFunc:
                        return AddOrg(stub);
                    case ConfigKey.UpdateOrgFunc:
                        return UpdateOrg(stub);

                    case ConfigKey.AddOrgMemberFunc:
                        return AddOrgMember(stub);

                    case ConfigKey.UpdateOrgMemberFunc:
                        return UpdateOrgMember(stub);

                    default:
                        return null;

                }
            }
            catch (Exception ex)
            {
                return stub.Response(ex.Message, StatusCode.BAD_OTHERS);
            }

        }

        #region Channel配置

        /// <summary>
        /// 在本地创建新的通道(特殊 等于是在本地创建创世区块)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private ChainCodeInvokeResponse CreateNewChannel(IChaincodeStub stub)
        {
            var identity = stub.GetPeerIdentity();
            var args = stub.GetArgs();
            if (args.Length != 1)
            {
                return stub.Response("参数个数不正确", StatusCode.BAD_ARGS_NUMBER);
            }

            //组织
            var OrgConfig = new OrgConfig();
            OrgConfig.OrgId = identity.OrgId;
            OrgConfig.Name = identity.Name;
            OrgConfig.Certificate = identity.Certificate;
            OrgConfig.Address = identity.Address;

            //通道
            var channel = new ChannelConfig();
            channel.ChannelID = args[0];
            channel.OrgConfigs.Add(OrgConfig);
            //初始化系统链码
            InitSystemChainCode(channel);
            stub.PutState(ConfigKey.Channel, Newtonsoft.Json.JsonConvert.SerializeObject(channel));
            return stub.Response("", StatusCode.Successful);
        }

        /// <summary>
        /// 初始化系统链码配置
        /// </summary>
        /// <param name="channel"></param>
        private void InitSystemChainCode(ChannelConfig channel)
        {
            ///系统链码 区块查询链码 链码生命周期管理 net网络配置
            var syschaincodeList = new List<string>();
            syschaincodeList.Add(ConfigKey.SysBlockQueryChaincode);
            syschaincodeList.Add(ConfigKey.SysCodeLifeChaincode);
            syschaincodeList.Add(ConfigKey.SysNetConfigChaincode);
            foreach (var item in syschaincodeList)
            {
                var cfg = new ChaincodeConfig();
                cfg.Name = item;
                cfg.Policy.OrgIds = channel.OrgConfigs.Select(p => p.OrgId).Distinct().ToList();
                cfg.Policy.Number = cfg.Policy.OrgIds.Count;
                channel.ChainCodeConfigs.Add(cfg);
            }
        }

        //无用 是用于  链码生命周期管理的链码
        public ChainCodeInvokeResponse Init(IChaincodeStub stub)
        {
            throw new NotImplementedException();
        }

        #endregion


        #region 组织配置相关操作


        /// <summary>
        /// 添加组织
        /// </summary>
        /// <param name="stub"></param>
        /// <returns></returns>
        private ChainCodeInvokeResponse AddOrg(IChaincodeStub stub)
        {
            if (stub.GetArgs().Count() != 1)
            {
                return stub.Response("", StatusCode.BAD_ARGS_NUMBER);
            }
            var arg = stub.GetArgs()[0];

            OrgConfig org = Newtonsoft.Json.JsonConvert.DeserializeObject<OrgConfig>(arg);

            #region 数据完整性校验
            if (string.IsNullOrEmpty(org.OrgId))
            {
                return stub.Response("组织ID不能为空", StatusCode.BAD_OTHERS);
            }
            if (string.IsNullOrEmpty(org.Name))
            {
                return stub.Response("组织名称不能为空", StatusCode.BAD_OTHERS);
            }
            if (string.IsNullOrEmpty(org.Address))
            {
                return stub.Response("组织链接地址不能为空", StatusCode.BAD_OTHERS);
            }
            if (org.Certificate == null)
            {
                return stub.Response("组织证书不能为空", StatusCode.BAD_OTHERS);
            }
            if (!org.Certificate.Check())
            {
                return stub.Response("证书数据不完整", StatusCode.BAD_OTHERS);
            }
            if (org.Certificate.TBSCertificate.CAType != CAType.Peer)
            {
                return stub.Response("证书类型不正确", StatusCode.BAD_OTHERS);
            }
            //证书自签名校验
            var checkRs = RSAHelper.VerifyData(org.Certificate.TBSCertificate.PublicKey, Newtonsoft.Json.JsonConvert.SerializeObject(org.Certificate.TBSCertificate), org.Certificate.SignatureValue);
            if (!checkRs)
            {
                throw new Exception("证书校验非自签名证书");
            }

            #endregion

            var channelconfig = stub.GetState<ChannelConfig>(ConfigKey.Channel);
            //组织重复检验
            if (channelconfig.OrgConfigs.Any(p => p.OrgId == org.OrgId))
            {
                return stub.Response("组织已加入通道", StatusCode.BAD_OTHERS);
            }
            //组织地址校验
            if (channelconfig.OrgConfigs.Any(p => p.Address == org.Address))
            {
                return stub.Response("组织Address已配置在通道", StatusCode.BAD_OTHERS);
            }

            var orgconfig = new OrgConfig();
            orgconfig.OrgId = org.OrgId;
            orgconfig.Address = org.Address;
            orgconfig.Name = org.Name;
            orgconfig.Certificate = org.Certificate;

            //组织加入通道
            channelconfig.OrgConfigs.Add(orgconfig);

            //更新系统链码的背书策略
            channelconfig.ChainCodeConfigs.Where(p => p.Name == ConfigKey.SysBlockQueryChaincode).FirstOrDefault().Policy.OrgIds.Add(org.OrgId);
            channelconfig.ChainCodeConfigs.Where(p => p.Name == ConfigKey.SysBlockQueryChaincode).FirstOrDefault().Policy.Number++;

            channelconfig.ChainCodeConfigs.Where(p => p.Name == ConfigKey.SysCodeLifeChaincode).FirstOrDefault().Policy.OrgIds.Add(org.OrgId);
            channelconfig.ChainCodeConfigs.Where(p => p.Name == ConfigKey.SysCodeLifeChaincode).FirstOrDefault().Policy.Number++;

            channelconfig.ChainCodeConfigs.Where(p => p.Name == ConfigKey.SysNetConfigChaincode).FirstOrDefault().Policy.OrgIds.Add(org.OrgId);
            channelconfig.ChainCodeConfigs.Where(p => p.Name == ConfigKey.SysNetConfigChaincode).FirstOrDefault().Policy.Number++;
            //保存数据
            stub.PutState(ConfigKey.Channel, channelconfig);
            return stub.Response("", StatusCode.Successful);
        }

        /// <summary>
        /// 添加组织节点 只能更改自己的组织节点
        /// </summary>
        /// <param name="stub"></param>
        /// <returns></returns>
        private ChainCodeInvokeResponse AddOrgMember(IChaincodeStub stub)
        {
            if (stub.GetArgs().Count() != 1)
            {
                return stub.Response("", StatusCode.BAD_ARGS_NUMBER);
            }
            var arg = stub.GetArgs()[0];
            OrgMemberConfig newOrgMember = Newtonsoft.Json.JsonConvert.DeserializeObject<OrgMemberConfig>(arg);

            #region 请求合法性校验
            //如果修改的组织节点等于本组织节点 则需要验证请求是否由本节点发出
            var identity = stub.GetPeerIdentity();
            if (newOrgMember.OrgId == identity.OrgId)
            {
                var signature = stub.GetTxRequestHeaderSignature();
                //这里添加验证
            }

            #endregion


            #region 成员数据完整性校验


            if (string.IsNullOrEmpty(newOrgMember.OrgId))
            {
                return stub.Response("组织ID不能为空", StatusCode.BAD_OTHERS);
            }
            if (string.IsNullOrEmpty(newOrgMember.Name))
            {
                return stub.Response("成员名称不能为空", StatusCode.BAD_OTHERS);
            }
            if (newOrgMember.Certificate == null)
            {
                return stub.Response("证书不存在", StatusCode.BAD_OTHERS);
            }
            if (!newOrgMember.Certificate.Check())
            {
                return stub.Response("证书数据不完整", StatusCode.BAD_OTHERS);
            }
            if (newOrgMember.Certificate.TBSCertificate.CAType == CAType.Peer)
            {
                return stub.Response("证书类型不正确", StatusCode.BAD_OTHERS);
            }

            #endregion


            var channelconfig = stub.GetState<ChannelConfig>(ConfigKey.Channel);
            var org = channelconfig.OrgConfigs.Where(p => p.OrgId == newOrgMember.OrgId).FirstOrDefault();
            if (org == null)
            {
                return stub.Response("组织不存在", StatusCode.BAD_OTHERS);
            }
            #region 成员唯一性检验

            if (org.OrgMember.Any(p => p.Name == newOrgMember.Name))
            {
                return stub.Response("成员名称重复", StatusCode.BAD_OTHERS);
            }
            
            if (org.OrgMember.Any(p => p.Certificate.TBSCertificate.SerialNumber == p.Certificate.TBSCertificate.SerialNumber))
            {
                return stub.Response("成员证书编号重复", StatusCode.BAD_OTHERS);
            }
            
            if (org.OrgMember.Any(p => p.Certificate.TBSCertificate.PublicKey == p.Certificate.TBSCertificate.PublicKey))
            {
                return stub.Response("成员公钥重复", StatusCode.BAD_OTHERS);
            }
            #endregion

            org.OrgMember.Add(newOrgMember);
            stub.PutState(ConfigKey.Channel, channelconfig);
            return stub.Response("", StatusCode.Successful);
        }

        /// <summary>
        /// 更新节点    只能更改自己的组织节点
        /// </summary>
        /// <param name="stub"></param>
        /// <returns></returns>
        private ChainCodeInvokeResponse UpdateOrgMember(IChaincodeStub stub)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// 更新组织 只能更改自己的组织节点
        /// </summary>
        /// <param name="stub"></param>
        /// <returns></returns>
        private ChainCodeInvokeResponse UpdateOrg(IChaincodeStub stub)
        {
            if (stub.GetArgs().Count() != 1)
            {
                return stub.Response("", StatusCode.BAD_ARGS_NUMBER);
            }
            var arg = stub.GetArgs()[0];

            OrgConfig newOrg = Newtonsoft.Json.JsonConvert.DeserializeObject<OrgConfig>(arg);

            #region 数据完整性校验


            if (string.IsNullOrEmpty(newOrg.OrgId))
            {
                return stub.Response("组织ID不能为空", StatusCode.BAD_OTHERS);
            }
            if (string.IsNullOrEmpty(newOrg.Name))
            {
                return stub.Response("组织名称不能为空", StatusCode.BAD_OTHERS);
            }
            if (string.IsNullOrEmpty(newOrg.Address))
            {
                return stub.Response("组织链接地址不能为空", StatusCode.BAD_OTHERS);
            }
            if (newOrg.Certificate == null)
            {
                return stub.Response("组织证书不能为空", StatusCode.BAD_OTHERS);
            }
            if (!newOrg.Certificate.Check())
            {
                return stub.Response("证书数据不完整", StatusCode.BAD_OTHERS);
            }
            if (newOrg.Certificate.TBSCertificate.CAType != CAType.Peer)
            {
                return stub.Response("证书类型不正确", StatusCode.BAD_OTHERS);
            }

            #endregion


            var channelconfig = stub.GetState<ChannelConfig>(ConfigKey.Channel);

            //获取旧的组织配置
            var oldorg = channelconfig.OrgConfigs.Where(p => p.OrgId == newOrg.OrgId).FirstOrDefault();
            if (oldorg == null)
            {
                return stub.Response("组织不存在", StatusCode.BAD_OTHERS);
            }
            oldorg.Address = newOrg.Address;
            oldorg.Name = newOrg.Name;
            oldorg.Certificate = newOrg.Certificate;

            stub.PutState(ConfigKey.Channel, channelconfig);
            return stub.Response("", StatusCode.Successful);
        }

        #endregion


        #region 链码查询

        public ChainCodeInvokeResponse Query(IChaincodeStub stub)
        {
            try
            {
                var funcname = stub.GetFunction();
                switch (funcname)
                {
                    //查询配置信息
                    case ConfigKey.QueryChannelConfig:
                        return QueryChannelConfig(stub);
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                return stub.Response(ex.Message, StatusCode.BAD_OTHERS);
            }
        }

        /// <summary>
        /// 获取整个通道配置
        /// </summary>
        /// <param name="stub"></param>
        /// <returns></returns>
        public ChainCodeInvokeResponse QueryChannelConfig(IChaincodeStub stub)
        {
            var config = stub.GetState<ChannelConfig>(ConfigKey.Channel);
            return stub.Response(config);
        }

        #endregion
    }
}
