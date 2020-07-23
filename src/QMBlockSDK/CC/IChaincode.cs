namespace QMBlockSDK.CC
{
    public interface IChaincode
    {
        ChainCodeInvokeResponse Init(IChaincodeStub stub);

        ChainCodeInvokeResponse Invoke(IChaincodeStub stub);

        ChainCodeInvokeResponse Query(IChaincodeStub stub);

    }
}
