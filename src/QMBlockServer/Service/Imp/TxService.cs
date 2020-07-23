using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QMBlockSDK.Helper;
using QMBlockSDK.TX;
using QMBlockServer.Service.Intetface;
using QMRaftCore;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.Node;
using System;
using System.Threading.Tasks;

namespace QMBlockServer.Service.Imp
{
    public class TxService : ITxService
    {
        private readonly IMemoryCache _cache;
        private readonly NodePeer _nodePeer;
        private readonly ILogger<TxService> _logger;

        public TxService(IMemoryCache memoryCache, ILogger<TxService> logger)
        {
            _cache = memoryCache;
            _nodePeer = _cache.Get<NodePeer>(CacheKeys.NodePeer);
            _logger = logger;
        }

        //区块分发 peer
        public Task<HandOutResponse> BlockHandOut(HandOutRequest request)
        {
            var node = _nodePeer.GetNode(request.ChannelId);
            if (node == null)
            {
                throw new Exception($"节点未加入{request.ChannelId} 通道");
            }
            return node.Handle(request);
        }

        //背书 peer
        public Task<EndorseResponse> Endorse(EndorseRequest request)
        {
            var node = _nodePeer.GetNode(request.ChannelId);
            if (node == null)
            {
                throw new Exception($"节点未加入{request.ChannelId} 通道");
            }
            return node.Endorse(request.Request);
        }

        //交易 peer
        public async Task<TxResponse> Transaction(TxRequest request)
        {
            var node = _nodePeer.GetNode(request.Data.Channel.ChannelId);
            if (node == null)
            {
                throw new Exception($"节点未加入{request.Header.ChannelId} 通道");
            }
            return await node.Transaction(request);
        }


        #region 创建通道 启动通道

        public async Task<TxResponse> InitChannel(TxHeader request)
        {
            var channel = _nodePeer.GetNode(request.ChannelId);
            if (channel != null)
            {
                return new TxResponse()
                {
                    Status = false,
                    Msg = $"通道 {request.ChannelId} 已存在"
                };
            }
            //如果是初始化通道
            if (request.ChaincodeName == QMBlockSDK.CC.ConfigKey.SysNetConfigChaincode
                && request.FuncName == QMBlockSDK.CC.ConfigKey.InitChannelFunc)
            {
                var rs = await _nodePeer.InitChannel(request.ChannelId);
                return rs;
            }
            else
            {
                return new TxResponse()
                {
                    Status = false,
                    Msg = "初始化失败Channel失败"
                };
            }
        }

        //加入某个通道或者启动某个通道 前提是通道中已经配置了该节点
        public async Task<TxResponse> JoinChannel(TxHeader request)
        {
            var channel = _nodePeer.GetNode(request.ChannelId);
            if (channel != null)
            {
                return new TxResponse()
                {
                    Status = false,
                    Msg = $"通道 {request.ChannelId} 已存在"
                };
            }
            //如果是加入通道
            if (request.ChaincodeName == QMBlockSDK.CC.ConfigKey.SysNetConfigChaincode
                && request.FuncName == QMBlockSDK.CC.ConfigKey.JoinChannelFunc)
            {
                return await _nodePeer.JoinChannel(request.ChannelId);
            }
            else
            {
                return new TxResponse()
                {
                    Status = false,
                    Msg = "加入channel失败"
                };
            }
        }

        #endregion


        //执行交易 member
        public async Task<TxResponse> InvokeTx(TxHeader request)
        {
            //如果是初始化通道
            if (request.ChaincodeName == QMBlockSDK.CC.ConfigKey.SysNetConfigChaincode
                    && request.FuncName == QMBlockSDK.CC.ConfigKey.InitChannelFunc)
            {
                return await InitChannel(request);
            }
            //如果是加入通道
            if (request.ChaincodeName == QMBlockSDK.CC.ConfigKey.SysNetConfigChaincode
                && request.FuncName == QMBlockSDK.CC.ConfigKey.JoinChannelFunc)
            {
                return await JoinChannel(request);
            }//其他情况是执行链码
            var txRequest = ModelHelper.ToTxRequest(request);
            var node = _nodePeer.GetNode(request.ChannelId);
            if (node == null)
            {
                throw new Exception($"节点未加入{request.ChannelId} 通道");
            }
            return await node.TransactionCommit(txRequest);
        }

        //查询交易 member
        public async Task<TxResponse> QueryTx(TxHeader request)
        {
            var txRequest = ModelHelper.ToTxRequest(request);
            var node = _nodePeer.GetNode(request.ChannelId);
            if (node == null)
            {
                throw new Exception($"节点未加入{request.ChannelId} 通道");
            }
            return await node.TransactionCommit(txRequest);
        }
    }
}
