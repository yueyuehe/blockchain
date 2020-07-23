using QMBlockSDK.CC;
using QMBlockSDK.Config;
using QMBlockSDK.TX;
using QMBlockServer.Service.Intetface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QMBlockServer.Service.Imp
{

    /// <summary>
    /// 常用的查询交易
    /// </summary>
    public class QueryBLL : IQueryBLL
    {
        public readonly ITxService _txService;
        public QueryBLL(ITxService service)
        {
            _txService = service;
        }
        //查询配置
        public async Task<ChannelConfig> QueryChannelConfigAsync(string channelId)
        {
            var txHeader = new QMBlockSDK.TX.TxHeader();
            txHeader.ChannelId = channelId;
            txHeader.ChaincodeName = ConfigKey.SysNetConfigChaincode;
            txHeader.FuncName = ConfigKey.QueryChannelConfig;
            txHeader.Args = new string[0];
            txHeader.Type = TxType.Query;
            var rs = await _txService.QueryTx(txHeader);
            if (rs.Status)
            {
                return rs.Data as ChannelConfig;
            }
            else
            {
                throw new Exception(rs.Msg);
            }
        }


    }
}
