using QMBlockSDK.CC;
using System;

namespace ChainCodeTest
{
    public class TestChainCode : IChaincode
    {
        public ChainCodeInvokeResponse Init(IChaincodeStub stub)
        {
            var account = new Account()
            {
                Name = "李四",
                Money = 1000
            };
            stub.PutState("a", account);
            account = new Account()
            {
                Name = "张三",
                Money = 1000
            };
            stub.PutState("b", account);
            return stub.Response("", StatusCode.Successful);
        }

        public ChainCodeInvokeResponse Invoke(IChaincodeStub stub)
        {
            var func = stub.GetFunction().ToUpper();
            switch (func)
            {
                case "TRANSFER":
                    return Transfer(stub);
                default:
                    return stub.Response("can not find function", StatusCode.BAD_OTHERS);
            }

        }

        private ChainCodeInvokeResponse Transfer(IChaincodeStub stub)
        {
            var args = stub.GetArgs();
            if (args.Length != 3)
            {
                return stub.Response("", StatusCode.BAD_ARGS_NUMBER);
            }
            var a = stub.GetState<Account>(args[0]);
            var b = stub.GetState<Account>(args[1]);
            a.Money = a.Money - Convert.ToDecimal(args[2]);
            b.Money = b.Money + Convert.ToDecimal(args[2]);
            if (a.Money < 0)
            {
                return stub.Response("账户 A 余额不足", StatusCode.BAD_BUSINESS);
            }
            stub.PutState(args[0], a);
            stub.PutState(args[1], b);
            return stub.Response("", StatusCode.Successful);

        }


        public ChainCodeInvokeResponse Query(IChaincodeStub stub)
        {
            var func = stub.GetFunction().ToUpper();
            switch (func)
            {
                case "FINDBYKEY":
                    return FindByKey(stub);
                default:
                    return null;
            }
        }

        private ChainCodeInvokeResponse FindByKey(IChaincodeStub stub)
        {
            if (stub.GetArgs().Length != 1)
            {
                return stub.Response("", StatusCode.BAD_ARGS_NUMBER);
            }
            var rs = stub.GetState("a");
            return stub.Response(rs, StatusCode.Successful);
        }
    }


    public class Account
    {
        public string Name { get; set; }

        public decimal Money { get; set; }


    }
}
