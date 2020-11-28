using Microsoft.Extensions.Logging;
using QMBlockClientSDK.Interface;
using QMBlockClientSDK.Service.Interface;
using QMBlockSDK.TX;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading.Tasks;

namespace QMBlockClientSDK.Service.Imp
{
    public class TxService : ITxService
    {
        private readonly IRequestClient _client;

        private readonly ILogger<TxService> _logger;

        public TxService(IRequestClient client)
        {
            _client = client;
            //_logger = factory.CreateLogger<TxService>();
        }

        public async Task<TxResponse> InvokeTx(TxHeader request)
        {
            request.Type = TxType.Invoke;
            return await _client.TxInvoke(request);
        }


        public async Task<TxResponse> InvokeTxWaitResult(TxHeader request)
        {
            request.Type = TxType.Invoke;
            //开始监听
            var task = ListenTx(request);
            var response = await _client.TxInvoke(request);

            //如果状态是true 则等待上链中，返回监听结果
            if (response.Status)
            {
                return await task;
            }
            else
            {
                //结束监听 返回错误结果
                //task.
                return response;
            }
        }


        public async Task<TxResponse> QueryTx(TxHeader request)
        {
            request.Type = TxType.Query;
            return await _client.TxQuery(request);
        }

        private async Task<TxResponse> ListenTx(TxHeader txHeader)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            return await Task.Run(() =>
              {
                  var status = false;
                  TxResponse response = null;
                  using (var connection = factory.CreateConnection())
                  using (var channel = connection.CreateModel())
                  {
                      channel.ExchangeDeclare(txHeader.ChannelId, ExchangeType.Direct, true, false, null);
                      var queueName = channel.QueueDeclare().QueueName;
                      channel.QueueBind(queueName, txHeader.ChannelId, txHeader.TxHeaderId);
                      var consumer = new EventingBasicConsumer(channel);
                      consumer.Received += (model, ea) =>
                      {
                          try
                          {
                              var body = ea.Body.ToArray();
                              var message = Encoding.UTF8.GetString(body);
                              Console.WriteLine(message);
                              response = Newtonsoft.Json.JsonConvert.DeserializeObject<TxResponse>(message);
                          }
                          catch (Exception ex)
                          {
                              response = new TxResponse
                              {
                                  Status = false,
                                  Msg = "反序列化TxResponse失败"
                              };
                              _logger.LogError(ex, ex.Message);
                          }
                          finally
                          {
                              status = true;
                          }
                      };
                      channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
                      while (true)
                      {
                          if (status)
                          {
                              return response;
                          }
                          System.Threading.Thread.Sleep(200);
                      }
                  } 
              });
        }

        /*
        private async Task<TxResponse> ListenTx(TxHeader txHeader, EventHandler<BasicDeliverEventArgs> action)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            await Task.Run(() =>
             {
                 using (var connection = factory.CreateConnection())
                 using (var channel = connection.CreateModel())
                 {
                     channel.ExchangeDeclare(txHeader.ChannelId, ExchangeType.Direct, true, false, null);
                     var queueName = channel.QueueDeclare().QueueName;
                     channel.QueueBind(queueName, txHeader.ChannelId, txHeader.TxHeaderId);
                     var consumer = new EventingBasicConsumer(channel);
                     consumer.Received += action;
                     channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
                 }
             });
        }
        */
    }
}
