using Microsoft.Extensions.Logging;
using QMBlockSDK.Idn;
using QMBlockSDK.Ledger;
using QMBlockSDK.TX;
using QMBlockUtils;
using QMRaftCore.Concensus.Node;
using QMRaftCore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QMRaftCore.QMProvider.Imp
{
    /// <summary>
    /// 交易池对象 负责交易的打包 出块 交易验证等
    /// </summary>
    public class TxPool : ITxPool
    {
        private readonly List<Envelope> _txList;
        private readonly Timer _timer;
        private readonly object obj = new object();
        private readonly object packing = new object();
        private readonly IBlockDataManager _blockDataManager;
        private readonly INode _node;
        private readonly IConfigProvider _configProvider;
        //private readonly Dictionary<string, bool> _txStatus;
        private readonly ILogger<TxPool> _log;

        public TxPool(ILoggerFactory logfactory, IConfigProvider configProvider,
          IBlockDataManager blockDataManager, INode node)
        {
            _log = logfactory.CreateLogger<TxPool>();
            _configProvider = configProvider;
            _txList = new List<Envelope>();
            _timer = new Timer(ResetElectionTimer, null, _configProvider.GetBatchTimeout(), _configProvider.GetBatchTimeout());
            _blockDataManager = blockDataManager;
            _node = node;
            //_txStatus = new Dictionary<string, bool>();
        }

        private void ResetElectionTimer(object x)
        {
            BlockPacking();
        }

        public Task AddAsync(Envelope tx)
        {
            return Task.Run(() =>
            {
                try
                {
                    lock (obj)
                    {
                        _txList.Add(tx);
                        //判断交易数量
                    }
                    if (_txList.Count() >= _configProvider.GetMaxTxCount())
                    {
                        BlockPacking();
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, ex.Message);
                }
            });
        }

        private void BlockPacking()
        {
            try
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                lock (packing)
                {
                    //对交易进行排序
                    List<Envelope> data = null;
                    lock (obj)
                    {
                        if (_txList.Count == 0)
                        {
                            return;
                        }
                        data = _txList.Where(p => true).ToList();
                        _txList.Clear();
                    }

                    #region 组装区块数据
                    var txmq = new Msg.Imp.TxMQ(_configProvider.GetMQSetting());

                    var currentBlock = _blockDataManager.GetLastBlock(_node.GetChannelId());
                    var block = new Block();
                    block.Signer.Identity = _configProvider.GetPeerIdentity().GetPublic();
                    //交易验证
                    data = Validate(data, out List<Envelope> errorTx);
                    //错误交易发送
                    txmq.PublishTxResponse(errorTx, "交易失败");

                    block.Data.Envelopes = data;
                    block.Header.Timestamp = DateTime.Now.Ticks;
                    block.Header.Number = currentBlock.Header.Number + 1;
                    block.Header.ChannelId = _node.GetChannelId();
                    //Term
                    block.Header.PreviousHash = currentBlock.Header.DataHash;
                    block.Header.DataHash = RSAHelper.GenerateMD5(Newtonsoft.Json.JsonConvert.SerializeObject(block));
                    block.Signer.Signature = RSAHelper.SignData(_configProvider.GetPrivateKey(), block);

                    #endregion

                    #region 区块分发

                    Concensus.Messages.HandOutResponse ts = null;
                    try
                    {
                        _log.LogWarning("区块分发开始");
                        ts = _node.BlockHandOut(block).Result;
                        _log.LogWarning("区块分发结束");
                    }
                    catch (Exception ex)
                    {
                        ts = new Concensus.Messages.HandOutResponse
                        {
                            Message = ex.Message,
                            Success = false
                        };
                        _log.LogWarning("区块分发错误");
                        _log.LogError(ex, ex.Message);
                    }

                    #endregion

                    #region 上链结果消息发送
                    if (ts.Success)
                    {
                        txmq.PublishTxResponse(block, null);
                    }
                    else
                    {
                        txmq.PublishTxResponse(block, ts.Message);
                    }
                    /*
                    if (ts.Success)
                    {
                        foreach (var item in block.Data.Envelopes)
                        {
                            //
                            // _txStatus.Add(item.TxReqeust.Data.TxId, true);
                        }
                    }
                    else
                    {
                        //通知  block中的交易失败
                        foreach (var item in block.Data.Envelopes)
                        {
                            _txStatus.Add(item.TxReqeust.Data.TxId, false);
                        }
                    }
                    */
                    #endregion
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, ex.Message);
            }
            finally
            {
                _timer.Change(_configProvider.GetBatchTimeout(), _configProvider.GetBatchTimeout());
            }
        }

        private List<Envelope> Validate(List<Envelope> data, out List<Envelope> errTx)
        {
            //初始化世界状态的值
            var wordState = new Dictionary<string, long>();
            foreach (var item in data)
            {
                foreach (var readset in item.PayloadReponse.TxReadWriteSet.ReadSet)
                {
                    var version = Convert.ToInt64(readset.Number);
                    //如果世界状态键重复 有重复的读取  判断版本号 留下最新的版本号的版本做为世界状态版本号
                    if (wordState.ContainsKey(readset.Key))
                    {
                        if (wordState[readset.Key] < version)
                        {
                            wordState[readset.Key] = version;
                        }
                    }
                    else
                    {
                        wordState.Add(readset.Key, version);
                    }
                }
            }
            var rightTx = new List<Envelope>();
            //var errTx = new List<Envelope>();
            errTx = new List<Envelope>();
            foreach (var item in data)
            {
                var tx = true;
                //1.判断读集的版本号是否在世界状态中
                foreach (var read in item.PayloadReponse.TxReadWriteSet.ReadSet)
                {
                    var version = read.Number;
                    if (version != wordState[read.Key])
                    {
                        //交易无效
                        tx = false;
                        break;
                    }
                }
                //如果是有效的交易更新世界状态
                if (tx)
                {
                    foreach (var write in item.PayloadReponse.TxReadWriteSet.WriteSet)
                    {
                        if (wordState.ContainsKey(write.Key))
                        {
                            wordState[write.Key] = wordState[write.Key] + 1;
                        }
                    }
                    rightTx.Add(item);
                }
                else
                {
                    //_txStatus.Add(item.TxReqeust.Data.TxId, false);
                    errTx.Add(item);
                }
            }

            return rightTx;
        }

        /// <summary>
        /// 交易排序
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private List<TxRequest> TxSort(List<TxRequest> tx)
        {
            return tx;
        }


        /// <summary>
        /// 停止捕获交易
        /// </summary>
        public void StopCacheTx()
        {
            if (_txList != null)
            {
                _txList.Clear();
            }
            if (_timer != null)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void StartCacheTx()
        {
            if (_timer != null)
            {
                _timer.Change(_configProvider.GetBatchTimeout(), _configProvider.GetBatchTimeout());
            }
        }


        /*
        /// <summary>
        /// 获取tx的状态 0  上链成功  1上链失败 2超时
        /// </summary>
        /// <param name="txId"></param>
        /// <returns></returns>
        internal async Task<string> TxStatus(string txId)
        {
            return await Task.Run<string>(() =>
             {
                 try
                 {
                     var count = 0;
                     while (true)
                     {
                         if (_txStatus.ContainsKey(txId))
                         {
                             var rs = _txStatus[txId] == true ? "0" : "1";
                             _txStatus.Remove(txId);
                             return rs;
                         }
                         else
                         {
                             count++;
                             Thread.Sleep(1000);
                             if (count > 10)
                             {
                                 return "2";
                             }
                         }
                     }
                 }
                 catch (Exception ex)
                 {
                     _log.LogError(ex, ex.Message);
                     return "2";
                 }
             });
        }

        */
    }
}
