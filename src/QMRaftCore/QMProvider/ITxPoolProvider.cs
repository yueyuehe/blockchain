namespace QMRaftCore.QMProvider
{
    /// <summary>
    /// 交易池提供程序 用于获取交易池
    /// </summary>
    public interface ITxPoolProvider
    {
        ITxPool GetTxPool(string channelId);

    }
}
