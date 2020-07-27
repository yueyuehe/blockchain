using QMBlockSDK.TX;
using System;
using System.Collections.Generic;
using System.Text;

namespace QMRaftCore.QMProvider
{
    /// <summary>
    /// 消息队列
    /// </summary>
    public interface IMQProvider
    {
        //通知交易结果
        void PublishTxReponse(TxResponse response);

    }
}
