using System.Collections.Generic;

namespace QMBlockSDK.TX
{
    public class TxReadWriteSet
    {
        public IDictionary<string, string> ReadSet { get; set; }
        public IDictionary<string, string> WriteSet { get; set; }
        public IDictionary<string, bool> DeletedSet { get; set; }


        public TxReadWriteSet()
        {
            ReadSet = new Dictionary<string, string>();
            WriteSet = new Dictionary<string, string>();
            DeletedSet = new Dictionary<string, bool>();
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

}
