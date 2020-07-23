using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QMBlockSDK.CC;
using QMBlockSDK.DAL;
using QMRaftCore.Concensus.Node;
using QMRaftCore.Concensus.Peers;
using QMRaftCore.FiniteStateMachine;
using QMRaftCore.Infrastructure;
using QMRaftCore.Log;
using QMRaftCore.QMImp;
using QMRaftCore.QMProvider;
using QMRaftCore.QMProvider.Imp;

namespace QMPeer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddDbContext<BlockContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("BlockContext")), ServiceLifetime.Transient);

            services.AddScoped<IIdentityProvider, IdentityProvider>();
            services.AddScoped<IPeersProvider, PeersProvider>();
            services.AddScoped<IAssemblyProvider, AssemblyProvider>();
            services.AddScoped<IBlockDataManager, BlockDataManager>();
            services.AddScoped<IChainCodeExecutor, ChainCodeExecutor>();
            services.AddScoped<IFiniteStateMachine, DBStateMachine>();
            services.AddScoped<IPolicyProvider, PolicyProvider>();
            services.AddScoped<IConfigProvider, ConfigProvider>();
            services.AddScoped<NodePeer>();
            
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
            IHostingEnvironment env,
            IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseMvc();
            StartPeer(serviceProvider);
        }

        public static void StartPeer(
            IServiceProvider serviceProvider
            )
        {
            //节点提供对象
            var nodepeer = serviceProvider.GetService<NodePeer>();
            nodepeer.StartNodesAsync().Wait();
        }



    }
}
