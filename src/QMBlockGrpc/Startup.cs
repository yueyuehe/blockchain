using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QMBlockSDK.CC;
using QMBlockSDK.Idn;
using QMBlockServer.Service.Imp;
using QMBlockServer.Service.Intetface;
using QMRaftCore.Concensus.Node;
using QMRaftCore.Data.Model;
using QMRaftCore.FiniteStateMachine;
using QMRaftCore.Infrastructure;
using QMRaftCore.QMProvider;
using QMRaftCore.QMProvider.Imp;
using System;
using System.IO;
using System.Text;

namespace QMBlockGrpc
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();

            #region EF数据库配置 （临时）

            //services.AddDbContext<BlockContext>(options =>
            //options.UseSqlServer(Configuration.GetConnectionString("BlockContext")), ServiceLifetime.Transient);

            #endregion

            #region raft网络相关配置
            services.AddMemoryCache();


            services.AddScoped<IInvokeBLL, InvokeBLL>();
            services.AddScoped<IQueryBLL, QueryBLL>();
            services.AddScoped<IIdentityProvider, IdentityProvider>();
            //services.AddScoped<IPeersProvider, GrpcPeerProvider>();
            services.AddScoped<IAssemblyProvider, AssemblyProvider>();
            //services.AddScoped<IChainCodeExecutor, ChainCodeExecutor>();
            //services.AddScoped<IPolicyProvider, PolicyProvider>();
            //services.AddScoped<IConfigProvider, ConfigProvider>();
            services.AddScoped<IRaftNetService, RaftNetService>();
            services.AddScoped<QMBlockServer.Service.Intetface.ITxService, QMBlockServer.Service.Imp.TxService>();
            #endregion

            #region mongoDB配置
            //区块数据
            services.Configure<BlockDatabaseSettings>(
             Configuration.GetSection(nameof(BlockDatabaseSettings)));
            services.AddSingleton<IBlockDatabaseSettings>(sp =>
                sp.GetRequiredService<IOptions<BlockDatabaseSettings>>().Value);
            //历史数据库
            services.Configure<HistoryDatabaseSettings>(
             Configuration.GetSection(nameof(HistoryDatabaseSettings)));
            services.AddSingleton<IHistoryDatabaseSettings>(sp =>
                sp.GetRequiredService<IOptions<HistoryDatabaseSettings>>().Value);
            //状态数据库
            services.Configure<StatusDatabaseSettings>(
             Configuration.GetSection(nameof(StatusDatabaseSettings)));
            services.AddSingleton<IStatusDatabaseSettings>(sp =>
                sp.GetRequiredService<IOptions<StatusDatabaseSettings>>().Value);

            #endregion

            //var pkfile = Configuration.GetSection("peerIdentity:PkFile").Value;
            //var cafile = Configuration.GetSection("peerIdentity:CaFile").Value;
            var mspFile = Configuration.GetSection("peerIdentity:MspFile").Value;
            #region CA账号与节点账号
            var basepath = AppContext.BaseDirectory;
            var account = new UserAccount();
            //如果文件存在
            //if (File.Exists(pkfile) && File.Exists(cafile))
            //{
            //    account.PrivateKey = File.ReadAllText(pkfile).Trim();
            //    var castr = File.ReadAllText(cafile).Trim();
            //    account.Certificate = Newtonsoft.Json.JsonConvert.DeserializeObject<Certificate>(castr);
            //}
            var path = Path.Combine(basepath, mspFile);
            if (File.Exists(Path.Combine(basepath, mspFile)))
            {
                var mspdata = File.ReadAllText(mspFile).Trim();
                account = Newtonsoft.Json.JsonConvert.DeserializeObject<UserAccount>(mspdata);
            }

            var caAccount = new CaAccount()
            {
                Username = Configuration.GetSection("peerIdentity:ca:username").Value,
                Password = Configuration.GetSection("peerIdentity:ca:password").Value
            };
            PeerIdentity peerIdentity = new PeerIdentity()
            {
                Address = Configuration.GetSection("peerIdentity:peerUrl").Value,
                OrgId = Configuration.GetSection("peerIdentity:OrgId").Value,
                Certificate = account.Certificate,
                PrivateKey = account.PrivateKey,
                Name = account.Certificate?.TBSCertificate.Subject
            };

            services.AddSingleton<UserAccount>(account);
            services.AddSingleton<CaAccount>(caAccount);
            services.AddSingleton<PeerIdentity>(peerIdentity);

            #endregion

            #region auth身份验证与授权
            var securityKey = string.IsNullOrEmpty(account.PrivateKey) ? "securityKey" : account.PrivateKey;  //configuration.GetSection("peerIdentity:privateKey").ToString();
            string name = account.Certificate == null ? "name" : account.Certificate.TBSCertificate.Subject; //configuration.GetSection("peerIdentity:name").ToString();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                 .AddJwtBearer(options =>
                 {
                     options.TokenValidationParameters = new TokenValidationParameters
                     {
                         ValidateIssuer = true,//是否验证Issuer
                         //ValidateAudience = true,//是否验证Audience
                         ValidateLifetime = true,//是否验证失效时间
                         //验证码有效时间
                         ClockSkew = TimeSpan.FromSeconds(30),
                         ValidateIssuerSigningKey = true,//是否验证SecurityKey
                         //ValidAudience = Const.Domain,//Audience
                         ValidIssuer = name,//Issuer，这两项和前面签发jwt的设置一致
                         IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey))//拿到SecurityKey
                     };
                 });
            services.AddAuthorization();
            #endregion

            services.AddScoped<NodePeer>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
            IWebHostEnvironment env,
            IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            #region 身份验证
            app.UseAuthentication();
            app.UseAuthorization();
            #endregion

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapGrpcService<GreeterService>();
                endpoints.MapGrpcService<AuthService>();
                endpoints.MapGrpcService<QMBlockGrpc.Services.TxService>();
                endpoints.MapGrpcService<QMBlockGrpc.Services.NetService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
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
