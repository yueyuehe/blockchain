using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using QMBlockClientSDK.Imp;
using QMBlockClientSDK.Interface;
using QMBlockClientSDK.Model;
using QMBlockClientSDK.Service.Imp;
using QMBlockClientSDK.Service.Interface;
using QMBlockSDK.TX;
using QMBlockUtils;
using System;
using System.Security.Cryptography;
using System.Linq;
using RabbitMQ.Client;
using System.Text;

namespace ConsoleApp1
{
    class Program
    {
        static readonly string channelId = "mychannel";
        static ServiceProvider serviceProvider = null;
        static void Main(string[] args)
        {
            var guid = Guid.NewGuid().ToString();
            var ip = "http://localhost:6001";
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug);
            });
            serviceProvider = new ServiceCollection().AddHttpClient()
                           .AddSingleton<ILoggerFactory>(loggerFactory)
                           .AddLogging()
                           .AddScoped<IConfigService, ConfigService>()
                           .AddScoped<ITxService, TxService>()
                           .AddScoped<ICaService, CaService>()
                           .AddScoped<IQMClientFactory, QMClientFactory>()
                           .AddScoped<IRequestClient, WebApiClient>()
                           .AddSingleton<PeerSetting>(new PeerSetting() { Url = ip })
                           .BuildServiceProvider();
            Action2();
            return;
        }


        public static void Action2()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine(@"
                        0:设置连接地址
                        1:添加节组织，
                        2:安装链码，
                        3:初始化链码，
                        4:执行链码，
                        5:查询
                        6:初始化channel,
                        7:加入通道
                        8:创建CA证书
                        9:注册CA证书
                        ");

                    var input = Console.ReadLine();
                    QMBlockSDK.TX.TxResponse response = null;
                    switch (input)
                    {
                        case "0":
                            Console.WriteLine("请输入连接服务器IP");
                            var key = Console.ReadLine();
                            break;
                        case "1":
                            response = AddOrg();
                            break;
                        case "2":
                            response = InstallChaincode();
                            break;
                        case "3":
                            response = InitChaincode();
                            break;
                        case "4":
                            response = InvokeTx();
                            break;
                        case "5":
                            response = QueryTx();
                            break;
                        case "6":
                            response = InitChannel();
                            break;
                        case "7":
                            response = JoinChannel();
                            break;
                        case "8":
                            response = CreateCa();
                            break;
                        case "9":
                            response = Register();
                            break;
                    }
                    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(response));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public static QMBlockSDK.TX.TxResponse AddOrg()
        {
            var client = GetChannel();
            return client.AddOrg("org2", "org2.peer0.com", "https://localhost:5001", "xxx").Result;
        }

        public static QMBlockSDK.TX.TxResponse InstallChaincode()
        {
            var client = GetChannel();


            var model = new ChaincodeModel()
            {
                Name = "TestChainCode",
                Namespace = "ChainCodeTest",
                Version = "1.0",
                Policy = Newtonsoft.Json.JsonConvert.SerializeObject(new { number = 1, OrgIds = new string[] { "org1" } })
            };
            return client.InstallChaincode(model.Name, model.Namespace, model.Version, model.Policy).Result;

        }

        public static QMBlockSDK.TX.TxResponse InitChaincode()
        {
            var client = GetChannel();
            return client.InitChaincode("TestChainCode", new string[] { "" }).Result;
        }

        public static QMBlockSDK.TX.TxResponse InvokeTx()
        {
            _ = new QMBlockSDK.TX.TxHeader
            {
                ChannelId = "mychannel",
                Type = TxType.Invoke,
                Args = new string[] { "b", "a", "1" },
                FuncName = "Transfer",
                ChaincodeName = "TestChainCode"
            };
            var client = GetChannel();
            return client.InvokeTx("TestChainCode", "Transfer", new string[] { "b", "a", "1" }).Result;
        }

        public static QMBlockSDK.TX.TxResponse InitChannel()
        {
            var config = serviceProvider.GetService<IConfigService>();
            return config.InitChannel("mychannel").Result;
        }

        public static QMBlockSDK.TX.TxResponse JoinChannel()
        {
            var config = serviceProvider.GetService<IConfigService>();
            return config.JoinChannel("mychannel").Result;
        }

        public static QMBlockSDK.TX.TxResponse QueryTx()
        {
            var client = GetChannel();
            return client.QueryTx("TestChainCode", "FINDBYKEY", new string[] { "a" }).Result;
        }


        public static TxResponse CreateCa()
        {
            var ca = GetCAService();
            var account = ca.CreateCertificateAsync("admin", "hwadmin", QMBlockSDK.Idn.CAType.Peer, "hworg").Result;
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(account));
            return new TxResponse();
        }


        /// <summary>
        /// 注册
        /// </summary>
        /// <returns></returns>
        public static TxResponse Register()
        {
            var rs = GetCAService().RegistAccountAsync("admin", "hwadmin", channelId, new QMBlockSDK.Idn.Certificate());
            Console.WriteLine(rs);
            return new TxResponse();
        }


        private static ICaService GetCAService()
        {
            var factory = serviceProvider.GetService<IQMClientFactory>();
            return factory.GetCaService();
        }
        private static ChannelClient GetChannel()
        {
            var factory = serviceProvider.GetService<IQMClientFactory>();
            return factory.GetChannel("mychannel");
        }
    }
}
