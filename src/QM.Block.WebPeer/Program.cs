using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace QM.Block.WebPeer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {

            var config = new ConfigurationBuilder()
            .AddJsonFile("hostsettings.json", optional: true)
            .Build();

            return Host.CreateDefaultBuilder(args)
                     .ConfigureWebHostDefaults(webBuilder =>
                     {
                         webBuilder.UseConfiguration(config);
                         webBuilder.UseStartup<Startup>();
                     });
        }
    }
}
