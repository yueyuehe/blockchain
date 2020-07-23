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
        protected readonly TxReadWriteSet _txreadWriteSet;
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
            _txreadWriteSet = new TxReadWriteSet();
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
            rs.TxReadWriteSet = _txreadWriteSet;
            return rs;
        }

        public ChainCodeInvokeResponse Response<T>(T data) where T : new()
        {
            var rs = new ChainCodeInvokeResponse();
            rs.StatusCode = StatusCode.Successful;
            rs.TxReadWriteSet = _txreadWriteSet;
            rs.Data = data;
            return rs;
        }
        public ChainCodeInvokeResponse InvokeChaincode(string chaincodeName, List<byte[]> args, string channel)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region 读集
        public string GetState(string key)
        {
            //key = GetChannelId() + "_" + GetChaincodeName() + "_" + key;
            //之后再解决
            //不通链码有不同的 文档
            //var docname = GetChaincodeName();
            //if (docname == ConfigKey.SysBlockQueryChaincode
            //    || docname == ConfigKey.SysCodeLifeChaincode
            //    || docname == ConfigKey.SysNetConfigChaincode)
            //{
            //    docname = ConfigKey.ChannelConfig;
            //}

            var collection = _mongoDB.GetCollection<DataStatus>(ConfigKey.DataStatusDocument);
            var filter = Builders<DataStatus>.Filter.Eq("Key", key);
            var rs = collection.Find(filter).FirstOrDefault();
            if (rs == null)
            {
                return null;
            }
            if (_txreadWriteSet.ReadSet.ContainsKey(key))
            {
                _txreadWriteSet.ReadSet.Remove(key);
            }
            _txreadWriteSet.ReadSet.Add(key, rs.Version);
            return rs.Value;
        }


        public T GetState<T>(string key) where T : new()
        {
            var data = GetState(key);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(data);
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


            var rs = _chainCodeExecutor.ChaincodeInvoke(txRequest).Result;
            if (rs.StatusCode != StatusCode.Successful)
            {
                return false;
            }
            //读写集追加到集合
            foreach (var item in rs.TxReadWriteSet.ReadSet)
            {
                if (!_txreadWriteSet.ReadSet.ContainsKey(item.Key))
                {
                    _txreadWriteSet.ReadSet.Add(item.Key, item.Value);
                }
            }

            foreach (var item in rs.TxReadWriteSet.WriteSet)
            {
                if (_txreadWriteSet.WriteSet.ContainsKey(item.Key))
                {
                    _txreadWriteSet.WriteSet.Remove(item.Key);
                }
                _txreadWriteSet.WriteSet.Add(item.Key, item.Value);
            }

            foreach (var item in rs.TxReadWriteSet.DeletedSet)
            {
                if (_txreadWriteSet.DeletedSet.ContainsKey(item.Key))
                {
                    _txreadWriteSet.DeletedSet.Remove(item.Key);
                }
                _txreadWriteSet.DeletedSet.Add(item.Key, item.Value);
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
            foreach (var item in rs.TxReadWriteSet.ReadSet)
            {
                if (!_txreadWriteSet.ReadSet.ContainsKey(item.Key))
                {
                    _txreadWriteSet.ReadSet.Add(item.Key, item.Value);
                }
            }

            foreach (var item in rs.TxReadWriteSet.WriteSet)
            {
                if (_txreadWriteSet.WriteSet.ContainsKey(item.Key))
                {
                    _txreadWriteSet.WriteSet.Remove(item.Key);
                }
                _txreadWriteSet.WriteSet.Add(item.Key, item.Value);
            }

            foreach (var item in rs.TxReadWriteSet.DeletedSet)
            {
                if (_txreadWriteSet.DeletedSet.ContainsKey(item.Key))
                {
                    _txreadWriteSet.DeletedSet.Remove(item.Key);
                }
                _txreadWriteSet.DeletedSet.Add(item.Key, item.Value);
            }

            return rs;
        }

        #endregion

        #region set集

        public void PutState(string key, string value)
        {
            //key = GetChannelId() + "_" + GetChaincodeName() + "_" + key;
            var wset = _txreadWriteSet.WriteSet;
            if (wset.ContainsKey(key))
            {
                wset.Remove(key);
            }
            wset.Add(key, value);
        }

        public void PutState(string key, object value)
        {
            PutState(key, Newtonsoft.Json.JsonConvert.SerializeObject(value));
        }

        #endregion

        #region 删除
        public void DelState(string key)
        {
            key = GetChannelId() + "_" + GetChaincodeName() + "_" + key;
            var dset = _txreadWriteSet.DeletedSet;
            if (dset.ContainsKey(key))
            {
                dset.Remove(key);
            }
            dset.Add(key, true);
        }

        #endregion

        public bool InitChaincode(string chaincodename, string[] args)
        {
            if (this.GetChaincodeName() != ConfigKey.SysCodeLifeChaincode && this.GetFunction() != ConfigKey.InitChaincodeFunc)
            {
                return false;
            }

            //var tx = new TxRequest();
            //tx.Timestamp = _txRequest.Timestamp;
            //tx.TxId = _txRequest.TxId;
            //tx.Type = _txRequest.Type;
            //tx.Channel.ChannelId = _txRequest.Channel.ChannelId;
            //tx.Channel.Chaincode.Args = args;
            //tx.Channel.Chaincode.Name = chaincodename;

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
            foreach (var item in rs.TxReadWriteSet.ReadSet)
            {
                if (!_txreadWriteSet.ReadSet.ContainsKey(item.Key))
                {
                    _txreadWriteSet.ReadSet.Add(item.Key, item.Value);
                }
            }

            foreach (var item in rs.TxReadWriteSet.WriteSet)
            {
                if (_txreadWriteSet.WriteSet.ContainsKey(item.Key))
                {
                    _txreadWriteSet.WriteSet.Remove(item.Key);
                }
                _txreadWriteSet.WriteSet.Add(item.Key, item.Value);
            }

            foreach (var item in rs.TxReadWriteSet.DeletedSet)
            {
                if (_txreadWriteSet.DeletedSet.ContainsKey(item.Key))
                {
                    _txreadWriteSet.DeletedSet.Remove(item.Key);
                }
                _txreadWriteSet.DeletedSet.Add(item.Key, item.Value);
            }

            return true;
        }

        public string GetTxRequestHeaderSignature()
        {
            throw new NotImplementedException();
        }
        
    
    }
}
