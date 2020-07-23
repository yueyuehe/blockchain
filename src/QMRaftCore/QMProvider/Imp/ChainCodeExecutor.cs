using QMBlockSDK.CC;
using QMBlockSDK.TX;
using QMRaftCore.BlockChain.Chaincode.Imp;
using QMRaftCore.Data;
using QMRaftCore.Data.Imp;
using QMRaftCore.FiniteStateMachine;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace QMRaftCore.QMProvider
{
    /// <summary>
    /// 链码执行器
    /// </summary>
    public class ChainCodeExecutor : IChainCodeExecutor
    {
        private readonly IAssemblyProvider _assemblyProvider;
        private readonly IIdentityProvider _identityProvider;
        private readonly DataManager _dataManager;

        public ChainCodeExecutor(
           IAssemblyProvider assemblyProvider,
           IIdentityProvider identityProvider,
           DataManager dataManager)
        {
            _assemblyProvider = assemblyProvider;
            _identityProvider = identityProvider;
            _dataManager = dataManager;
        }

        #region  码中码  链码中的链码

        public Task<ChainCodeInvokeResponse> ChaincodeInit(TxRequest request)
        {
            if (request.Data.Type != TxType.Invoke)
            {
                return Task.FromResult(new ChainCodeInvokeResponse() { StatusCode = StatusCode.BAD_TX_TYPE });
            }

            var stub = GetChaincodeStub(request);
            var chaincode = new Chaincode();
            chaincode.NameSpace = stub.GetChaincodeNameSpace();
            chaincode.Name = stub.GetChaincodeName();
            chaincode.Version = stub.GetChaincodeVersion();
            var ass = _assemblyProvider.GetAssembly(stub.GetChannelId(), stub.GetChaincodeName(), stub.GetChaincodeNameSpace(), stub.GetChaincodeVersion());
            var classfullname = chaincode.NameSpace + "." + chaincode.Name;
            //必须使用 名称空间+类名称
            var type = ass.GetType(classfullname);
            //方法的名称
            MethodInfo method = type.GetMethod("Init");
            //必须使用名称空间+类名称
            var obj = ass.CreateInstance(classfullname);
            var rs = method.Invoke(obj, new object[] { stub });
            if (rs != null)
            {
                return Task.FromResult((ChainCodeInvokeResponse)rs);
            }
            return null;
        }

        public Task<ChainCodeInvokeResponse> ChaincodeInvoke(TxRequest request)
        {
            request.Data.Type = TxType.Invoke;
            var stub = GetChaincodeStub(request);
            return ChaincodeInvoke(stub);
        }

        public Task<ChainCodeInvokeResponse> ChaincodeQuery(TxRequest request)
        {
            request.Data.Type = TxType.Query;
            var stub = GetChaincodeStub(request);
            return ChaincodeQuery(stub);
        }

        #endregion


        /** 根据交易类型  执行不同的函数
         * 
         * Config --->Config的链码中
         * Invoke --->  IChaincode 的 Invoke 返回 InvokeReponse
         * Query  --->  IChaincode 的 Query方法       返回读集和T 数据
         * 
         **/


        public Task<ChainCodeInvokeResponse> Submit(TxRequest request)
        {
            var stub = GetChaincodeStub(request);
            return Submit(stub);
        }


        private Task<ChainCodeInvokeResponse> Submit(IChaincodeStub stub)
        {
            var chaincodeName = stub.GetChaincodeName();
            //系统链码
            if (chaincodeName == ConfigKey.SysBlockQueryChaincode
                || chaincodeName == ConfigKey.SysCodeLifeChaincode
                || chaincodeName == ConfigKey.SysNetConfigChaincode)
            {
                switch (stub.GetTxType())
                {
                    case TxType.Invoke:
                        return SystemInvoke(stub);
                    case TxType.Query:
                        return SystemQuery(stub);
                    default:
                        return null;
                }
            }
            //一般交易
            else
            {
                switch (stub.GetTxType())
                {
                    case TxType.Invoke:
                        return ChaincodeInvoke(stub);
                    case TxType.Query:
                        return ChaincodeQuery(stub);
                    default:
                        return null;
                }
            }
        }

        #region 业务交易

        private Task<ChainCodeInvokeResponse> ChaincodeInvoke(IChaincodeStub stub)
        {
            try
            {
                var chaincode = new Chaincode();
                chaincode.NameSpace = stub.GetChaincodeNameSpace();
                chaincode.Name = stub.GetChaincodeName();
                chaincode.Version = stub.GetChaincodeVersion();
                var ass = _assemblyProvider.GetAssembly(stub.GetChannelId(), stub.GetChaincodeName(), stub.GetChaincodeNameSpace(), stub.GetChaincodeVersion());
                var classfullname = chaincode.NameSpace + "." + chaincode.Name;
                //必须使用 名称空间+类名称
                var type = ass.GetType(classfullname);
                //方法的名称
                MethodInfo method = type.GetMethod("Invoke");
                //必须使用名称空间+类名称
                var obj = ass.CreateInstance(classfullname);
                var rs = method.Invoke(obj, new object[] { stub });
                if (rs != null)
                {
                    return Task.FromResult((ChainCodeInvokeResponse)rs);
                }
            }
            catch (Exception ex)
            {
                return null;
            }

            return null;
        }

        private Task<ChainCodeInvokeResponse> ChaincodeQuery(IChaincodeStub stub)
        {
            var chaincode = new Chaincode();
            chaincode.NameSpace = stub.GetChaincodeNameSpace();
            chaincode.Name = stub.GetChaincodeName();
            chaincode.Version = stub.GetChaincodeVersion();
            var ass = _assemblyProvider.GetAssembly(stub.GetChannelId(), stub.GetChaincodeName(), stub.GetChaincodeNameSpace(), stub.GetChaincodeVersion());
            var classfullname = chaincode.NameSpace + "." + chaincode.Name;
            //必须使用 名称空间+类名称
            var type = ass.GetType(classfullname);
            //方法的名称
            MethodInfo method = type.GetMethod("Query");
            //必须使用名称空间+类名称
            var obj = ass.CreateInstance(classfullname);
            var rs = method.Invoke(obj, new object[] { stub });
            if (rs != null)
            {
                return Task.FromResult((ChainCodeInvokeResponse)rs);
            }
            return null;
        }

        #endregion

        #region 配置交易

        private async Task<ChainCodeInvokeResponse> SystemInvoke(IChaincodeStub stub)
        {
            switch (stub.GetChaincodeName())
            {
                case ConfigKey.SysCodeLifeChaincode:
                    return new CodeLifeChaincode().Invoke(stub);
                case ConfigKey.SysNetConfigChaincode:
                    return new NetConfigChaincode().Invoke(stub);
                case ConfigKey.SysBlockQueryChaincode:
                    return new BlockQueryChaincode().Invoke(stub);
                default:
                    return null;
            }
        }

        private async Task<ChainCodeInvokeResponse> SystemQuery(IChaincodeStub stub)
        {

            switch (stub.GetChaincodeName())
            {
                case ConfigKey.SysCodeLifeChaincode:
                    return new CodeLifeChaincode().Query(stub);
                case ConfigKey.SysNetConfigChaincode:
                    return new NetConfigChaincode().Query(stub);
                case ConfigKey.SysBlockQueryChaincode:
                    return new BlockQueryChaincode().Query(stub);
                default:
                    return null;
            }
        }

        #endregion

        #region 类型转换
        private IChaincodeStub GetChaincodeStub(TxRequest request)
        {
            //如果是初始化通道通道的config是null 
            var requestData = request.Data;
            var identity = _identityProvider.GetPeerIdentity();
            //如果是初始化化通道
            if (request.Header.ChaincodeName == ConfigKey.SysNetConfigChaincode && request.Header.FuncName == ConfigKey.InitChannelFunc)
            {
                return new ChaincodeStub(this, _dataManager.GetStatusDB(), request, identity);
            }
            //如果是其他链码
            var config = _dataManager.GetChannelConfig(requestData.Channel.ChannelId);
            var chaincode = config.ChainCodeConfigs.Where(p => p.Name == requestData.Channel.Chaincode.Name).FirstOrDefault();
            if (chaincode == null)
            {
                throw new Exception("链码不存在");
            }
            requestData.Channel.Chaincode.Version = chaincode.Version;
            requestData.Channel.Chaincode.NameSpace = chaincode.Namespace;
            return new ChaincodeStub(this, _dataManager.GetStatusDB(), request, identity);
        }

        #endregion
    }
}
