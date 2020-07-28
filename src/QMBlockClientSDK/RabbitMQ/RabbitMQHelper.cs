using QMBlockSDK.TX;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QMBlockClientSDK.RabbitMQ
{
    public class RabbitMQHelper
    {
        public async Task<TxResponse> ListenTx(string txheaderId, string channelId)
        {
            return await Task.Run(() =>
             {
                 var factory = new ConnectionFactory() { HostName = "localhost" };
                 using (var connection = factory.CreateConnection())
                 using (var channel = connection.CreateModel())
                 {
                     var status = false;
                     TxResponse response = null;
                     channel.ExchangeDeclare(channelId, ExchangeType.Direct, true, false, null);
                     var queueName = channel.QueueDeclare().QueueName;
                     channel.QueueBind(queueName, channelId, txheaderId);
                     var consumer = new EventingBasicConsumer(channel);
                     consumer.Received += (model, ea) =>
                     {
                         try
                         {
                             var body = ea.Body.ToArray();
                             var message = Encoding.UTF8.GetString(body);
                             response = Newtonsoft.Json.JsonConvert.DeserializeObject<TxResponse>(message);
                         }
                         catch (Exception ex)
                         {

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
                         System.Threading.Thread.Sleep(100);
                     }
                 }
             });
        }
    }
}
