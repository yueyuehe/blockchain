using QMBlockSDK.TX;
using System;

namespace QMBlockSDK.Helper
{
    public class ModelHelper
    {
        public static TxRequest ToTxRequest(TxHeader header)
        {
            var txRequest = new TxRequest();
            txRequest.Header = header;
            txRequest.Data.Channel.ChannelId = header.ChannelId;
            txRequest.Data.TxId = Guid.NewGuid().ToString();
            txRequest.Data.Type = header.Type;
            txRequest.Data.Timestamp = DateTime.Now.Ticks;
            txRequest.Data.Channel.Chaincode.FuncName = header.FuncName;
            txRequest.Data.Channel.Chaincode.Args = header.Args;
            txRequest.Data.Channel.Chaincode.Name = header.ChaincodeName;
            return txRequest;
        }


    }
}
