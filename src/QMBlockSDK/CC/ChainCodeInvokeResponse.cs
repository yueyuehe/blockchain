using QMBlockSDK.TX;
using System;

namespace QMBlockSDK.CC
{
    /// <summary>
    /// ChainCode执行完成后返回的结果对象
    /// </summary>
    public class ChainCodeInvokeResponse
    {
        /// <summary>
        /// 状态吗
        /// </summary>
        public StatusCode StatusCode { get; set; }
        /// <summary>
        /// Message消息
        /// </summary>
        public String Message { get; set; }
        /// <summary>
        /// 返回的数据
        /// </summary>
        public object Data { get; set; }
        /// <summary>
        /// 读写集 （chaincode执行需要reader和write哪些Key）
        /// </summary>
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
