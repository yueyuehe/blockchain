using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QMBlockSDK.Idn;
using QMBlockServer.Service.Imp;
using QMBlockServer.Service.Intetface;
using QMRaftCore.Concensus.Node;
using QMRaftCore.Data.Model;
using QMRaftCore.Msg.Model;
using QMRaftCore.QMProvider;
using QMRaftCore.QMProvider.Imp;
using System;
using System.IO;
using System.Text;

namespace QM.Block.WebPeer
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

            #region EF���ݿ����� ����ʱ��

            //services.AddDbContext<BlockContext>(options =>
            //options.UseSqlServer(Configuration.GetConnectionString("BlockContext")), ServiceLifetime.Transient);

            #endregion

            #region raft�����������
            services.AddMemoryCache();
            services.AddHttpClient();
            services.AddScoped<IInvokeBLL, InvokeBLL>();
            services.AddScoped<IQueryBLL, QueryBLL>();
            services.AddScoped<IIdentityProvider, IdentityProvider>();
            services.AddScoped<IAssemblyProvider, AssemblyProvider>();
            services.AddScoped<IRaftNetService, RaftNetService>();
            services.AddScoped<QMBlockServer.Service.Intetface.ITxService, QMBlockServer.Service.Imp.TxService>();
            #endregion

            #region mongoDB����
            //��������
            services.Configure<BlockDatabaseSettings>(
             Configuration.GetSection(nameof(BlockDatabaseSettings)));
            services.AddSingleton<IBlockDatabaseSettings>(sp =>
                sp.GetRequiredService<IOptions<BlockDatabaseSettings>>().Value);
            //��ʷ���ݿ�
            services.Configure<HistoryDatabaseSettings>(
             Configuration.GetSection(nameof(HistoryDatabaseSettings)));
            services.AddSingleton<IHistoryDatabaseSettings>(sp =>
                sp.GetRequiredService<IOptions<HistoryDatabaseSettings>>().Value);
            //״̬���ݿ�
            services.Configure<StatusDatabaseSettings>(
             Configuration.GetSection(nameof(StatusDatabaseSettings)));
            services.AddSingleton<IStatusDatabaseSettings>(sp =>
                sp.GetRequiredService<IOptions<StatusDatabaseSettings>>().Value);

            #endregion

            #region CA�˺���ڵ��˺�
            var mspFile = Configuration.GetSection("peerIdentity:MspFile").Value;
            var basepath = AppContext.BaseDirectory;
            var account = new UserAccount();
            //����ļ�����
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

            #region auth�����֤����Ȩ
            /*
            var securityKey = string.IsNullOrEmpty(account.PrivateKey) ? "securityKey" : account.PrivateKey;  //configuration.GetSection("peerIdentity:privateKey").ToString();
            string name = account.Certificate == null ? "name" : account.Certificate.TBSCertificate.Subject; //configuration.GetSection("peerIdentity:name").ToString();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        
                        //�Ƿ���֤Issuer
                        ValidateIssuer = true,
                        //ValidateAudience = true,
                        //�Ƿ���֤Audience
                        //�Ƿ���֤ʧЧʱ��
                        ValidateLifetime = true,
                        //��֤����Чʱ��
                        ClockSkew = TimeSpan.FromSeconds(30),
                        //�Ƿ���֤SecurityKey
                        ValidateIssuerSigningKey = true,
                        //ValidAudience = Const.Domain,//Audience
                        //Issuer���������ǰ��ǩ��jwt������һ��
                        ValidIssuer = name,
                        //�õ�SecurityKey
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey))
                    };
                });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("order", policy => policy.RequireClaim("EmployeeNumber"));
            });
            */
            #endregion

            #region RabbitMQ��Ϣ��������
            var mq = new MQSetting();
            //Configuration.GetSection("RabbitMQ").Bind(mq);
            //var mqsetting = Configuration.Get<MQSetting>( "RabbitMQ"); .Bind<MQSetting>("RabbitMQ");
            services.AddSingleton(mq);
            #endregion

            services.AddScoped<NodePeer>();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IServiceProvider serviceProvider
            )
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            StartPeer(serviceProvider);
        }

        public void StartPeer(IServiceProvider serviceProvider)
        {
            //�ڵ��ṩ����
            var nodepeer = serviceProvider.GetService<NodePeer>();
            nodepeer.StartNodesAsync().Wait();
        }
    }
}
