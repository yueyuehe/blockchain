using QMBlockSDK.CC;
using QMBlockSDK.Config;
using System;
using System.IO;
using System.Linq;

namespace QMRaftCore.BlockChain.Chaincode.Imp
{
    public class CodeLifeChaincode : IChaincode
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
                    //安装链码 把链码配置到通道中
                    case ConfigKey.InstallChaincodeFunc:
                        return InstallChaincode(stub);
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


        #region 链码配置


        /// <summary>
        /// 安装链码(将链码加入到配置中)
        /// </summary>
        /// <param name="stub"></param>
        /// <returns></returns>
        public ChainCodeInvokeResponse InstallChaincode(IChaincodeStub stub)
        {
            /**
             * arg[0] 链码名称
             * arg[1] 命名空间 也是dll文件名称
             * arg[2] 版本号
             * arg[3] 背书策略
             * 
             * 1.打包上传到指定的文件夹 (每个单独的 不是写链码)
             * * 2.安装链码 即在配置中写入链码
             * 3.链码初始化
             * 
             **/
            try
            {
                var args = stub.GetArgs();
                if (args.Length != 4)
                {
                    return stub.Response("", StatusCode.BAD_ARGS_NUMBER);
                }
                var chaincodeConfig = new ChaincodeConfig();
                chaincodeConfig.Name = args[0];
                chaincodeConfig.Namespace = args[1];
                chaincodeConfig.Version = args[2];
                chaincodeConfig.Status = ChaincodeStatus.INSTALLED;
                chaincodeConfig.Policy = Newtonsoft.Json.JsonConvert.DeserializeObject<Policy>(args[3]);


                #region 检查参数

                if (string.IsNullOrEmpty(chaincodeConfig.Name))
                {
                    return stub.Response("请输入链码名称", StatusCode.BAD_OTHERS);
                }
                if (string.IsNullOrEmpty(chaincodeConfig.Namespace))
                {
                    return stub.Response("请输入链码命名空间", StatusCode.BAD_OTHERS);
                }
                if (string.IsNullOrEmpty(chaincodeConfig.Version) || !decimal.TryParse(chaincodeConfig.Version, out decimal rs))
                {
                    return stub.Response("版本号格式不正确", StatusCode.BAD_OTHERS);
                }
                #endregion

                #region 检查代码是否已经上传到了正确目录

                String basePath = AppContext.BaseDirectory;
                var chaincodepath = Path.Combine(basePath, ConfigKey.ChaincodePath, stub.GetChannelId(), args[0], args[1], args[2]);
                if (!Directory.Exists(chaincodepath))
                {
                    return stub.Response("链码不存在", StatusCode.BAD_OTHERS);
                }
                #endregion



                var channelConfig = stub.GetState<ChannelConfig>(ConfigKey.Channel);

                #region 检查链码是否重复

                if (channelConfig.ChainCodeConfigs.Any(p => p.Name == chaincodeConfig.Name))
                {
                    return stub.Response("链码已经存在", StatusCode.BAD_OTHERS);
                }

                #endregion


                //校验组织是否存在

                #region 检查链码组织是否

                #endregion

                foreach (var item in chaincodeConfig.Policy.OrgIds)
                {
                    //如果组织不存在
                    if (!channelConfig.OrgConfigs.Any(p => p.OrgId == item))
                    {
                        return stub.Response(item + "组织不存在", StatusCode.BAD_OTHERS);
                    }
                }
                channelConfig.ChainCodeConfigs.Add(chaincodeConfig);
                stub.PutState(ConfigKey.Channel, channelConfig);
                return stub.Response("", StatusCode.Successful);
            }
            catch (Exception ex)
            {
                return stub.Response(ex.Message, StatusCode.BAD_OTHERS);
            }
        }


        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="stub"></param>
        /// <returns></returns>
        public ChainCodeInvokeResponse InitChaincode(IChaincodeStub stub)
        {
            try
            {
                /**
                 * 1.获取链码的配置信息
                 * 2.判断链码的状态 待初始化 运行中 已停止
                 * 3.状态为待初始化的才可进行初始化操作
                 * 4.初始化需要调用链码的 Init方法 需要用到链码执行器
                 */
                var args = stub.GetArgs();
                if (args.Length != 2)
                {
                    return stub.Response("", StatusCode.BAD_ARGS_NUMBER);
                }
                var channelconfig = stub.GetState<ChannelConfig>(ConfigKey.Channel);
                var chaincode = channelconfig.ChainCodeConfigs
                                .Where(p => p.Name == args[0]).FirstOrDefault();
                if (chaincode == null)
                {
                    return stub.Response("链码不存在", StatusCode.BAD_OTHERS);
                }
                //如果不是已安装状态
                if (chaincode.Status != ChaincodeStatus.INSTALLED)
                {
                    return stub.Response("", StatusCode.BAD_CHAINCODE_STATUS);
                }

                chaincode.Status = ChaincodeStatus.SERVICE;

                var code = new QMBlockSDK.TX.Chaincode();
                code.Name = chaincode.Name;
                //code.NameSpace = chaincode.Namespace;
                //code.Version = chaincode.Version;
                code.Args = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(args[1]);

                var rs = stub.InitChaincode(code.Name, code.Args);
                if (rs)
                {
                    stub.PutState(ConfigKey.Channel, channelconfig);
                    return stub.Response("", StatusCode.Successful);
                }
                else
                {
                    return stub.Response("", StatusCode.BAD_OTHERS);
                }
            }
            catch (Exception ex)
            {
                return stub.Response(ex.Message, StatusCode.BAD_OTHERS);
            }
        }

        #endregion


    }
}
