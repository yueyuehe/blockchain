using QMBlockSDK.Ledger;
using QMBlockSDK.TX;
using QMRaftCore.Msg.Model;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMRaftCore.Msg.Imp
{
    /// <summary>
    /// 交易消息队列->往交易队列中发送交易的上链信息
    /// 区块头信息  交易返回信息 交易头信息
    /// </summary>
    public class TxMQ
    {
        private readonly MQSetting _setting;

        private readonly ConnectionFactory _factory;
        public TxMQ(MQSetting setting)
        {
            _setting = setting;
            _factory = new ConnectionFactory();
            if (!string.IsNullOrEmpty(_setting.UserName))
            {
                _factory.UserName = _setting.UserName;
            }
            if (!string.IsNullOrEmpty(_setting.UserName))
            {
                _factory.UserName = _setting.UserName;
            }
            if (!string.IsNullOrEmpty(_setting.Password))
            {
                _factory.Password = _setting.Password;
            }
            if (!string.IsNullOrEmpty(_setting.VirtualHost))
            {
                _factory.VirtualHost = _setting.VirtualHost;
            }
            if (_setting.Port != 0)
            {
                _factory.Port = _setting.Port;
            }
            if (!string.IsNullOrEmpty(_setting.Host))
            {
                _factory.HostName = _setting.Host;
            }
        }
        /// <summary>
        /// 推送交易结果
        /// </summary>
        public void PublishTxResponse(QMBlockSDK.Ledger.Block block, string errorMsg)
        {
            if (string.IsNullOrEmpty(errorMsg))
            {
                using (var connection = _factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: block.Header.ChannelId, ExchangeType.Direct, true, false, null);
                    //channel.QueueDeclare("txchannel", true, false, false, null);
                    foreach (var item in block.Data.Envelopes)
                    {
                        var response = new TxResponse
                        {
                            ChannelId = block.Header.ChannelId,
                            Status = true,
                            Msg = "上链成功",
                            TxId = item.TxReqeust.Data.TxId,
                            Data = item.PayloadReponse.Message,
                            BlockNumber = block.Header.Number,
                            BlockDataHash = block.Header.DataHash,
                            BlockTimestamp = block.Header.Timestamp
                        };
                        var body = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(response));
                        channel.BasicPublish(block.Header.ChannelId, item.TxReqeust.Header.TxHeaderId, null, body);
                    }
                }
            }
            else
            {
                PublishTxResponse(block.Data.Envelopes, errorMsg);
            }
        }

        public void PublishTxResponse(List<Envelope> errorTx, string errorMsg)
        {
            if (errorTx.Count == 0)
            {
                return;
            }
            var channelId = errorTx.First().TxReqeust.Header.ChannelId;
            //factory.UserName = user; 
            //factory.Password = pass;
            //factory.VirtualHost = 虚拟主机;
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare("channel", ExchangeType.Direct, true, false, null);
                //channel.QueueDeclare(channelId, true, false, false, null);
                //channel.QueueBind(channelId, "channel", "", null);

                foreach (var item in errorTx)
                {
                    var response = new TxResponse();
                    response.ChannelId = channelId;
                    response.Status = false;
                    response.Msg = errorMsg;
                    response.TxId = item.TxReqeust.Data.TxId;
                    response.Data = item.PayloadReponse.Message;
                    var body = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(response));
                    channel.BasicPublish(channelId, response.TxId, null, body);
                }
            }
        }
    }
}
