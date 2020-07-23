using System.Collections.Generic;

namespace QMRaftCore.QMProvider.Imp
{
    public class TxPoolProvider : ITxPoolProvider
    {
        private readonly Dictionary<string, ITxPool> _pool;

        public TxPoolProvider()
        {
            _pool = new Dictionary<string, ITxPool>();
        }
        public ITxPool GetTxPool(string channelId)
        {
            if (!_pool.ContainsKey(channelId))
            {
                //_pool.Add(channelId, new TxPool());
            }
            return _pool[channelId];
        }
    }
}
