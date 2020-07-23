using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QMBlockSDK.Config;
using QMBlockSDK.TX;

namespace QMPeer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly TxController _c;
        public TestController(TxController c)
        {
            _c = c;
        }

        [HttpGet("addorg")]
        public async Task<JsonResult> AddOrg()
        {
            var org = new OrgPeerConfig();
            org.OrgId = "org2";
            org.PeerName = "org.peer0.com";
            org.PublicKey = "xxxx";
            org.Url = "https://localhost:5001";

            var tx = new TxRequest();
            tx.Channel.ChannelId = "mychannel";
            tx.Type = TxType.Invoke;
            tx.Channel.Chaincode.Args = new string[] { Newtonsoft.Json.JsonConvert.SerializeObject(org) };
            tx.Channel.Chaincode.FuncName = "addOrg";
            tx.Channel.Chaincode.NameSpace = "xxx";
            tx.Channel.Chaincode.Name = "SystemChaincode";
            tx.Channel.Chaincode.Version = "1.0";
            return await _c.Submit(tx);

        }

        [HttpGet("addchaincode")]
        public async Task<JsonResult> Addchaincode()
        {
            var tx = new TxRequest();
            tx.Channel.ChannelId = "mychannel";
            tx.Type = TxType.Invoke;
            tx.Channel.Chaincode.Args = new string[] { "TestChainCode", "ChainCodeTest", "1.0", "['org1']" };
            tx.Channel.Chaincode.FuncName = "InstallChaincode";
            tx.Channel.Chaincode.NameSpace = "xxx";
            tx.Channel.Chaincode.Name = "SystemChaincode";
            tx.Channel.Chaincode.Version = "1.0";
            return await _c.Submit(tx);
        }

        [HttpGet("initchaincode")]
        public async Task<JsonResult> Initchaincode()
        {
            var tx = new TxRequest();
            tx.Channel.ChannelId = "mychannel";
            tx.Type = TxType.Invoke;
            tx.Channel.Chaincode.Args = new string[] { "TestChainCode", "ChainCodeTest", "1.0", "['org1']" };
            tx.Channel.Chaincode.FuncName = "INITCHAINCODE";
            tx.Channel.Chaincode.NameSpace = "ChainCodeTest";
            tx.Channel.Chaincode.Name = "SystemChaincode";
            tx.Channel.Chaincode.Version = "1.0";
            return await _c.Submit(tx);
        }

        [HttpGet("exchaincode")]
        public async Task<JsonResult> exchaincode()
        {
            var tx = new TxRequest();
            tx.Channel.ChannelId = "mychannel";
            tx.Type = TxType.Invoke;
            tx.Channel.Chaincode.Args = new string[] { "b", "a", "1" };
            tx.Channel.Chaincode.FuncName = "Transfer";
            tx.Channel.Chaincode.NameSpace = "ChainCodeTest";
            tx.Channel.Chaincode.Name = "TestChainCode";
            tx.Channel.Chaincode.Version = "1.0";
            return await _c.Submit(tx);
        }
    }
}