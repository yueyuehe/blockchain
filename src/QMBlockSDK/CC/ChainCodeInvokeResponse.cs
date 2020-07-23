using QMBlockSDK.TX;
using System;

namespace QMBlockSDK.CC
{
    public class ChainCodeInvokeResponse
    {
        public StatusCode StatusCode { get; set; }
        public String Message { get; set; }
        public object Data { get; set; }
        public TxReadWriteSet TxReadWriteSet { get; set; }

        //public ChaincodeProposalPayload Payload { get; set; }
    }


    public enum StatusCode
    {
        BAD_TX_TYPE,

        BAD_ARGS_NUMBER,

        BAD_CHAINCODE_STATUS,

        BAD_ORG_REPEAT,

        Successful = 200,

        BAD_OTHERS,

        BAD_BUSINESS

    }
}
