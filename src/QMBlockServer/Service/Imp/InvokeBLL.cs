using QMBlockSDK.CC;
using QMBlockSDK.Config;
using QMBlockSDK.Idn;
using QMBlockSDK.TX;
using QMBlockServer.Service.Intetface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QMBlockServer.Service.Imp
{

    /// <summary>
    /// 常用的执行交易
    /// </summary>
    public class InvokeBLL : IInvokeBLL
    {
        public readonly ITxService _txService;
        public InvokeBLL(ITxService service)
        {
            _txService = service;
        }

        /// <summary>
        /// 向节点注册成员
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="ca"></param>
        /// <returns></returns>
        public async Task<TxResponse> RegistMember(string channelId, OrgMemberConfig ca)
        {
            var txHeader = new QMBlockSDK.TX.TxHeader();
            txHeader.ChannelId = channelId;
            //txHeader.ChaincodeName = ConfigKey.SysIdentityChaincode;
            txHeader.ChaincodeName = ConfigKey.SysNetConfigChaincode;
            txHeader.FuncName = ConfigKey.AddOrgMemberFunc;
            txHeader.Args = new string[] { Newtonsoft.Json.JsonConvert.SerializeObject(ca) };
            txHeader.Type = TxType.Invoke;
            var rs = await _txService.InvokeTx(txHeader);
            return rs;
        }
    }
}
