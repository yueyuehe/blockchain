using MongoDB.Driver;
using QMBlockSDK.Helper;
using QMBlockSDK.Idn;
using QMBlockSDK.MongoModel;
using QMBlockSDK.TX;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QMBlockSDK.CC
{
    public class ChaincodeStub : IChaincodeStub
    {
        private readonly TxRequest _txRequest;
        private readonly PeerIdentity _identity;
        protected readonly TxReadWriteSet _txReadWriteSet;
        protected readonly IChainCodeExecutor _chainCodeExecutor;
        private readonly IMongoDatabase _mongoDB;

        public ChaincodeStub(
            IChainCodeExecutor chainCodeExecutor,
            IMongoDatabase mongoDB,
            TxRequest txRequest,
            PeerIdentity peerIdentity)
        {
            _mongoDB = mongoDB;
            _chainCodeExecutor = chainCodeExecutor;
            _txRequest = txRequest;
            _identity = peerIdentity;
            _txReadWriteSet = new TxReadWriteSet();
        }

        #region 请求头信息
        public string GetFunction()
        {
            return _txRequest.Data.Channel.Chaincode.FuncName;
        }
        public string[] GetArgs()
        {
            return _txRequest.Data.Channel.Chaincode.Args;
        }

        public string GetChaincodeName()
        {
            return _txRequest.Data.Channel.Chaincode.Name;
        }
        public string GetTxId()
        {
            return _txRequest.Data.TxId;
        }

        public string GetChannelId()
        {
            return _txRequest.Data.Channel.ChannelId;
        }

        public string GetChaincodeVersion()
        {
            return _txRequest.Data.Channel.Chaincode.Version;
        }

        public string GetChaincodeNameSpace()
        {
            return _txRequest.Data.Channel.Chaincode.NameSpace;
        }

        public TxType GetTxType()
        {
            return _txRequest.Data.Type;
        }
        #endregion

        #region 返回值

        /// <summary>
        /// 对数据进行签名
        /// </summary>
        /// <returns></returns>
        public ChainCodeInvokeResponse Response(string msg, StatusCode code)
        {
            var rs = new ChainCodeInvokeResponse();
            rs.Message = msg;
            rs.StatusCode = code;
            if (rs.StatusCode != StatusCode.Successful)
            {
                return rs;
            }
            rs.TxReadWriteSet = _txReadWriteSet;
            return rs;
        }

        public ChainCodeInvokeResponse Response<T>(T data) where T : new()
        {
            var rs = new ChainCodeInvokeResponse();
            rs.StatusCode = StatusCode.Successful;
            rs.TxReadWriteSet = _txReadWriteSet;
            rs.Data = data;
            return rs;
        }
        public ChainCodeInvokeResponse InvokeChaincode(string chaincodeName, List<byte[]> args, string channel)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region 读集
        public T GetState<T>(string key) where T : new()
        {
            var rs = GetDataStatus(key, GetChaincodeName());
            if (rs == null)
            {
                return default(T);
            }
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(rs.Data);
        }

        private DataStatus GetDataStatus(string key, string chaincodename)
        {
            var collection = _mongoDB.GetCollection<DataStatus>(ConfigKey.DataStatusDocument);
            var rs = collection.AsQueryable().FirstOrDefault(p => p.Key == key && p.Chaincode == chaincodename);
            if (rs == null)
            {
                return null;
            }
            //设置读集
            var readItem = _txReadWriteSet.ReadSet.FirstOrDefault(p => p.Key == key && p.Chaincode == GetChaincodeName());
            if (readItem != null)
            {
                _txReadWriteSet.ReadSet.Remove(readItem);
            }
            readItem = new ReadItem()
            {
                Chaincode = rs.Chaincode,
                Key = rs.Key,
                Number = rs.BlockNumber,
                TxId = rs.TxId
            };
            _txReadWriteSet.ReadSet.Add(readItem);
            return rs;
        }

        /// <summary>
        /// 获取通道配置
        /// </summary>
        /// <returns></returns>
        public Config.ChannelConfig GetChannelConfig()
        {
            var name = GetChaincodeName();
            if (name == ConfigKey.SysCodeLifeChaincode || name == ConfigKey.SysNetConfigChaincode)
            {
                var status = GetDataStatus(ConfigKey.Channel, ConfigKey.SysNetConfigChaincode);
                if (status != null)
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<Config.ChannelConfig>(status.Data);
                }
            }
            return null;
        }
        #endregion

        #region 节点身份信息

        public PubliclyIdentity GetPeerIdentity()
        {
            return _identity.GetPublic();
        }

        #endregion

        #region 执行其他链码

        public bool ChaincodeInvoke(Chaincode chaincode)
        {
            /**
             * 链码中执行其他链码，本节点链码存在
             * 前一个交易是Invoke交易
             */

            if (_txRequest.Data.Type != TxType.Invoke)
            {
                return false;
            }

            var txheader = new TxHeader();
            txheader.ChannelId = _txRequest.Header.ChannelId;
            txheader.ChaincodeName = chaincode.Name;
            txheader.Args = chaincode.Args;
            txheader.FuncName = chaincode.FuncName;
            txheader.Type = TxType.Invoke;

            var txRequest = ModelHelper.ToTxRequest(txheader);

            //执行链码
            var rs = _chainCodeExecutor.ChaincodeInvoke(txRequest).Result;
            if (rs.StatusCode != StatusCode.Successful)
            {
                return false;
            }
            //追加读集
            foreach (var item in rs.TxReadWriteSet.ReadSet)
            {
                if (!_txReadWriteSet.ReadSet.Any(p => p.Key == item.Key && p.Chaincode == item.Chaincode))
                {
                    _txReadWriteSet.ReadSet.Add(item);
                }
            }
            //追加写集
            foreach (var item in rs.TxReadWriteSet.WriteSet)
            {
                var wirteItem = _txReadWriteSet.WriteSet.FirstOrDefault(p => p.Key == item.Key && p.Chaincode == item.Chaincode);
                if (wirteItem != null)
                {
                    _txReadWriteSet.WriteSet.Remove(wirteItem);
                }
                _txReadWriteSet.WriteSet.Add(item);
            }
            return true;
        }

        public ChainCodeInvokeResponse ChaincodeQuery(Chaincode chaincode)
        {
            //需要判断背书策略的节点是否一致 或则 tx 的节点 > 新交易的节点
            var txheader = new TxHeader();
            txheader.ChannelId = _txRequest.Header.ChannelId;
            txheader.ChaincodeName = chaincode.Name;
            txheader.Args = chaincode.Args;
            txheader.FuncName = chaincode.FuncName;
            txheader.Type = TxType.Query;

            var txRequest = ModelHelper.ToTxRequest(txheader);

            var rs = _chainCodeExecutor.ChaincodeQuery(txRequest).Result;


            if (rs.StatusCode != StatusCode.Successful)
            {
                return rs;
            }
            //读写集追加到集合
            //追加读集
            foreach (var item in rs.TxReadWriteSet.ReadSet)
            {
                if (!_txReadWriteSet.ReadSet.Any(p => p.Key == item.Key && p.Chaincode == item.Chaincode))
                {
                    _txReadWriteSet.ReadSet.Add(item);
                }
            }
            //追加写集
            foreach (var item in rs.TxReadWriteSet.WriteSet)
            {
                var wirteItem = _txReadWriteSet.WriteSet.FirstOrDefault(p => p.Key == item.Key && p.Chaincode == item.Chaincode);
                if (wirteItem != null)
                {
                    _txReadWriteSet.WriteSet.Remove(wirteItem);
                }
                _txReadWriteSet.WriteSet.Add(item);
            }
            return rs;
        }

        #endregion

        #region set集

        public void PutState<T>(string key, T data) where T : new()
        {
            var item = new WriteItem()
            {
                Key = key,
                Chaincode = GetChaincodeName(),
                Data = Newtonsoft.Json.JsonConvert.SerializeObject(data)
            };
            SetStatus(item);
        }

        private void SetStatus(WriteItem item)
        {
            var wset = _txReadWriteSet.WriteSet;
            var setItem = wset.FirstOrDefault(p => p.Key == item.Key && p.Chaincode == item.Chaincode);
            if (setItem != null)
            {
                wset.Remove(setItem);
            }
            wset.Add(item);
        }
        
        public void SetChannelConfig(Config.ChannelConfig config)
        {
            var name = GetChaincodeName();
            if (name == ConfigKey.SysCodeLifeChaincode || name == ConfigKey.SysNetConfigChaincode)
            {
                var item = new WriteItem()
                {
                    Key = ConfigKey.Channel,
                    Chaincode = ConfigKey.SysNetConfigChaincode,
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(config)
                };
                SetStatus(item);
            }
            else
            {
                throw new Exception("error:非系统链码");
            }
        }
       
        #endregion

        #region 删除
        public void DelState(string key)
        {
            key = GetChannelId() + "_" + GetChaincodeName() + "_" + key;
            var dset = _txReadWriteSet.WriteSet;
            var setItem = dset.FirstOrDefault(p => p.Key == key && p.Chaincode == GetChaincodeName());
            if (setItem != null)
            {
                dset.Remove(setItem);
            }
            dset.Add(new WriteItem() { Key = key, Chaincode = GetChaincodeName(), Data = null }); ;
        }

        #endregion

        public bool InitChaincode(string chaincodename, string[] args)
        {
            if (this.GetChaincodeName() != ConfigKey.SysCodeLifeChaincode && this.GetFunction() != ConfigKey.InitChaincodeFunc)
            {
                return false;
            }

            var txheader = new TxHeader();
            txheader.Type = _txRequest.Data.Type;
            txheader.ChannelId = _txRequest.Data.Channel.ChannelId;
            txheader.ChaincodeName = chaincodename;
            txheader.Args = args;
            var txRequest = ModelHelper.ToTxRequest(txheader);

            var rs = _chainCodeExecutor.ChaincodeInit(txRequest).Result;

            if (rs.StatusCode != StatusCode.Successful)
            {
                return false;
            }
            //读写集追加到集合
            //追加读集
            foreach (var item in rs.TxReadWriteSet.ReadSet)
            {
                if (!_txReadWriteSet.ReadSet.Any(p => p.Key == item.Key && p.Chaincode == item.Chaincode))
                {
                    _txReadWriteSet.ReadSet.Add(item);
                }
            }
            //追加写集
            foreach (var item in rs.TxReadWriteSet.WriteSet)
            {
                var wirteItem = _txReadWriteSet.WriteSet.FirstOrDefault(p => p.Key == item.Key && p.Chaincode == item.Chaincode);
                if (wirteItem != null)
                {
                    _txReadWriteSet.WriteSet.Remove(wirteItem);
                }
                _txReadWriteSet.WriteSet.Add(item);
            }

            return true;
        }

        public string GetTxRequestHeaderSignature()
        {
            throw new NotImplementedException();
        }

    }
}
