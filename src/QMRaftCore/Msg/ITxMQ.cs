using System;
using System.Collections.Generic;
using System.Text;

namespace QMRaftCore.Msg
{
    interface ITxMQ
    {
        void PublishTxResponse(QMBlockSDK.Ledger.Block block);
    }
}
