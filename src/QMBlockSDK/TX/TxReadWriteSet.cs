using System.Collections.Generic;

namespace QMBlockSDK.TX
{
    public class TxReadWriteSet
    {
        //读集
        public ICollection<ReadItem> ReadSet { get; set; }
        //写集
        public ICollection<WriteItem> WriteSet { get; set; }


        public TxReadWriteSet()
        {
            ReadSet = new List<ReadItem>();
            WriteSet = new List<WriteItem>();
        }
    }
    public class ReadWriteSetItem
    {
        public string ChainCodeName { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

    }


    /// <summary>
    /// 状态数据 保持的key-value值
    /// </summary>
    public class WriteItem
    {
        public string Key { get; set; }
        public string Chaincode { get; set; }
        public string Data { get; set; }
    }

    /// <summary>
    /// 读集
    /// </summary>
    public class ReadItem
    {
        /// <summary>
        /// 链码名称
        /// </summary>
        public string Chaincode { get; set; }

        /// <summary>
        /// 键值
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 区块号
        /// </summary>
        public long Number { get; set; }
        /// <summary>
        /// 交易ID
        /// </summary>
        public string TxId { get; set; }
    }

}
