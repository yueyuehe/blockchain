using QMBlockSDK.Idn;
using QMBlockSDK.TX;
using System;
using System.Collections.Generic;

namespace QMBlockSDK.CC
{
    public interface IChaincodeStub
    {

        String GetTxRequestHeaderSignature();

        TxType GetTxType();
        string[] GetArgs();
        String GetFunction();
        String GetTxId();
        String GetChaincodeName();
        String GetChaincodeNameSpace();
        String GetChannelId();
        String GetChaincodeVersion();
        string GetState(String key);
        T GetState<T>(String key) where T : new();
        PubliclyIdentity GetPeerIdentity();

        ChainCodeInvokeResponse Response(string msg, StatusCode code);

        ChainCodeInvokeResponse Response<T>(T data) where T : new();


        ChainCodeInvokeResponse InvokeChaincode(String chaincodeName, List<byte[]> args, String channel);

        void PutState(String key, string value);

        void PutState(String key, object value);

        void DelState(String key);


        ChainCodeInvokeResponse ChaincodeQuery(Chaincode chaincode);


        bool ChaincodeInvoke(Chaincode chaincode);

        bool InitChaincode(string codename, string[] args);


    }

}
