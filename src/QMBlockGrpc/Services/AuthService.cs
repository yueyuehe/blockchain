using Grpc.Core;
using Microsoft.Extensions.Logging;
using QMBlockServer.Service.Intetface;
using System.Threading.Tasks;
using System.Linq;
using QMBlockSDK.Idn;
using System;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using QMBlockUtils;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using QMRaftCore.QMProvider;
using Microsoft.AspNetCore.Authorization;
using QMBlockSDK.Config;

namespace QMBlockGrpc
{
    /**
     * 1.client传递证书标识
     * 2.server根据证书标识获取证书及用户基本信息
     * 3.server生成随机字符串，然后将字符串和标识保存到缓存中，30秒
     * 4.client端拿着缓存字符串，用自己的私钥进行签名，然后将签名和标识发送给server
     * 5.server端拿到签名后对签名进行验证，成功则返回token，并且保存token的信息
     * 6.client拿着token进行链接请求
     * 7.server根据token解析成用户，用于权限访问
     */
    public class AuthService : Auth.AuthBase
    {
        private readonly ILogger<AuthService> _logger;
        private readonly IIdentityProvider _identityProvider;
        private readonly IMemoryCache _cache;
        private readonly IInvokeBLL _invokeBLL;
        private readonly IQueryBLL _queryBll;

        public AuthService(ILogger<AuthService> logger,
            IInvokeBLL invokeBLL,
            IQueryBLL queryBll,
            IMemoryCache cache,
            IIdentityProvider identityProvider)
        {
            _cache = cache;
            _queryBll = queryBll;
            _logger = logger;
            _identityProvider = identityProvider;
            _invokeBLL = invokeBLL;
        }

        /// <summary>
        /// 获取随机字符串 用于签名校验 与 证书标识绑定
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<AuthReply> GetCode(AuthRequest request, ServerCallContext context)
        {
            try
            {
                Certificate ca = null;
                //获取配置
                var config = await _queryBll.QueryChannelConfigAsync(request.Data);

                #region 从配置中获取证书

                var org = config.OrgConfigs.Where(p => p.Certificate.TBSCertificate.SerialNumber == request.CaNumber).FirstOrDefault();
                if (org != null)
                {
                    ca = org.Certificate;
                }
                else
                {
                    foreach (var item in config.OrgConfigs)
                    {
                        var itemCA = item.OrgMember.Where(p => p.Certificate.TBSCertificate.SerialNumber == request.CaNumber).FirstOrDefault();
                        if (itemCA != null)
                        {
                            ca = itemCA.Certificate;
                            break;
                        }
                    }
                }

                #endregion

                if (ca == null)
                {
                    return new AuthReply()
                    {
                        Status = false
                    };
                }

                #region 生成随机字符串 保存编码 返回
                var code = Guid.NewGuid().ToString();
                ca.SignatureValue = code;
                //一分钟内有效
                _cache.Set(request.CaNumber, ca, TimeSpan.FromSeconds(60));
                //返回
                return new AuthReply()
                {
                    Status = true,
                    Code = code
                };
                #endregion

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new AuthReply()
                {
                    Status = false,
                    Msg = ex.Message
                };
            }
        }

        /// <summary>
        /// 签名校验通过后 返回token
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<AuthReply> GetToken(AuthRequest request, ServerCallContext context)
        {
            try
            {
                var rs = _cache.TryGetValue(request.CaNumber, out Certificate ca);
                if (!rs)
                {
                    return Task.FromResult(new AuthReply()
                    {
                        Status = false
                    });
                }
                //验证签名
                var verifyRs = RSAHelper.VerifyData(ca.TBSCertificate.PublicKey, ca.SignatureValue, request.Data);
                if (!verifyRs)
                {
                    return Task.FromResult(new AuthReply()
                    {
                        Status = false
                    });
                }
                _cache.Remove(request.CaNumber);
                var token = CreateToken(ca);
                //30分钟有效
                _cache.Set(RSAHelper.GenerateMD5(token), ca, TimeSpan.FromSeconds(30 * 60));
                return Task.FromResult(new AuthReply()
                {
                    Status = true,
                    Token = token
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Task.FromResult(new AuthReply()
                {
                    Status = false,
                    Msg = ex.Message
                });
            }
        }

        //用于 登录确认和token刷新
        [Authorize]
        public override Task<AuthReply> RefreshToken(AuthRequest request, ServerCallContext context)
        {
            try
            {
                //data 没有token 标识是login判断 能进来就表示client端已连接
                if (string.IsNullOrEmpty(request.Data))
                {
                    return Task.FromResult(new AuthReply() { Status = true });
                }
                //data 有数据 并且该数据是token ，根据token找到CA证书，重新生成token返回
                //用现有的token刷新 
                var rs = _cache.TryGetValue(RSAHelper.GenerateMD5(request.Data), out Certificate ca);
                if (rs)
                {
                    var token = CreateToken(ca);
                    return Task.FromResult(new AuthReply()
                    {
                        Status = true,
                        Token = token,
                    });
                }
                else
                {
                    return Task.FromResult(new AuthReply()
                    {
                        Status = false,
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Task.FromResult(new AuthReply()
                {
                    Status = false,
                    Msg = ex.Message
                });
            }
        }

        private string CreateToken(Certificate ca)
        {
            var identity = _identityProvider.GetPeerIdentity();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,"QMTOKEN"),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                new Claim("Version", ca.TBSCertificate.Version),
                new Claim("SerialNumber", ca.TBSCertificate.SerialNumber),
                new Claim("Signature", ca.TBSCertificate.Signature),
                new Claim("Issuer", ca.TBSCertificate.Issuer),
                new Claim("NotBefore", ca.TBSCertificate.NotBefore.ToString()),
                new Claim("NotAfter", ca.TBSCertificate.NotAfter.ToString()),
                new Claim("Subject", ca.TBSCertificate.Subject),
                new Claim("CAType", ca.TBSCertificate.CAType.ToString()),
                new Claim("PublicKey", ca.TBSCertificate.PublicKey)
               };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_identityProvider.GetPrivateKey()));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: identity.Name,   //签发者
                audience: ca.TBSCertificate.Subject, //接受者
                claims: claims,         //自定义属性
                notBefore: DateTime.Now,//xxx日期之前不被接受
                expires: DateTime.Now.AddMinutes(30),//token的生命周期
                signingCredentials: creds);
            //"iss" #非必须。issuer 请求实体，可以是发起请求的用户的信息，也可是jwt的签发者。
            //"iat" #非必须。issued at。 token创建时间，unix时间戳格式
            //"exp" #非必须。expire 指定token的生命周期。unix时间戳格式
            //"aud" #非必须。接收该JWT的一方。
            //"sub" #非必须。该JWT所面向的用户
            //"nbf" #非必须。not before。如果当前时间在nbf里的时间之前，则Token不被接受；一般都会留一些余地，比如几分钟。
            //"jti" #非必须。JWT ID。针对当前token的唯一标识
            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        #region CA证书创建于注册


        /// <summary>
        /// 创建账号 pk-证书
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<AccountReply> GenerateAccount(AccountRequest request, ServerCallContext context)
        {
            try
            {
                var causername = _identityProvider.GetCAUserName();
                var capassword = _identityProvider.GetCAPassword();
                if (causername != request.Username || capassword != request.Password)
                {
                    return Task.FromResult(new AccountReply()
                    {
                        Status = false,
                        PravateKey = "账号或用户名错误"
                    });
                }
                //是CA用户则创建账号
                var account = RSAHelper.CreateAccount();
                var privateKey = account[0];
                var publicKey = account[1];
                var ca = new Certificate();

                ca.TBSCertificate.Version = "1.0";
                ca.TBSCertificate.SerialNumber = Guid.NewGuid().ToString();
                ca.TBSCertificate.Signature = "RSA";
                ca.TBSCertificate.NotBefore = DateTime.Now.Ticks;
                ca.TBSCertificate.NotAfter = DateTime.Now.AddYears(3).Ticks;
                ca.TBSCertificate.Subject = request.AccountName;
                ca.TBSCertificate.PublicKey = publicKey;

                //如果不是创建peer节点的根证书 则需要验证peer节点的身份
                if (request.AccountType != "0")
                {
                    var identity = _identityProvider.GetPeerIdentity();
                    if (!identity.Valid())
                    {
                        throw new Exception("身份校验失败");
                    }
                }

                //根据账号类型生成证书 跟证书是自签名,其他是根证书签名
                switch (request.AccountType)
                {
                    case "0":
                        ca.TBSCertificate.CAType = CAType.Peer;
                        ca.TBSCertificate.Issuer = request.AccountName;
                        ca.SignatureValue = RSAHelper.SignData(privateKey, ca.TBSCertificate);
                        break;
                    case "1":
                        ca.TBSCertificate.Issuer = _identityProvider.GetPeerIdentity().GetPublic().Certificate.TBSCertificate.Subject;
                        ca.TBSCertificate.CAType = CAType.Admin;
                        ca.SignatureValue = RSAHelper.SignData(_identityProvider.GetPrivateKey(), ca.TBSCertificate);
                        break;
                    case "2":
                        ca.TBSCertificate.Issuer = _identityProvider.GetPeerIdentity().GetPublic().Certificate.TBSCertificate.Subject;
                        ca.TBSCertificate.CAType = CAType.User;
                        ca.SignatureValue = RSAHelper.SignData(_identityProvider.GetPrivateKey(), ca.TBSCertificate);
                        break;
                    case "3":
                        ca.TBSCertificate.Issuer = _identityProvider.GetPeerIdentity().GetPublic().Certificate.TBSCertificate.Subject;
                        ca.TBSCertificate.CAType = CAType.Reader;
                        ca.SignatureValue = RSAHelper.SignData(_identityProvider.GetPrivateKey(), ca.TBSCertificate);
                        break;
                    default:
                        break;
                }

                return Task.FromResult(new AccountReply()
                {
                    Status = true,
                    Certificate = Newtonsoft.Json.JsonConvert.SerializeObject(ca),
                    PravateKey = privateKey
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Task.FromResult(new AccountReply()
                {
                    Status = false,
                    PravateKey = ex.Message
                });
            }
        }

        //把member类型的证书加载到org下面的peer节点
        public override Task<RegistReply> Regist(RegistRequest request, ServerCallContext context)
        {
            try
            {
                if (_identityProvider.GetCAUserName() != request.Username
                    || _identityProvider.GetCAPassword() != request.Password)
                {
                    return Task.FromResult(new RegistReply()
                    {
                        Status = false
                    });
                }
                //获取证书
                var ca = Newtonsoft.Json.JsonConvert.DeserializeObject<Certificate>(request.Certificate);
                //校验签名
                var rs = RSAHelper.VerifyData(_identityProvider.GetPublicKey(), ca.TBSCertificate, ca.SignatureValue);
                //如果是peer节点类型 则返回false
                //peer节点类型在组织加入通道的时候进行校验
                if (ca.TBSCertificate.CAType == CAType.Peer)
                {
                    return Task.FromResult(new RegistReply()
                    {
                        Status = false,
                        Msg = "签名校验失败"
                    });
                }
                else
                {
                    //把证书上链 
                    var caconfig = new OrgMemberConfig();
                    caconfig.Name = ca.TBSCertificate.Subject;
                    caconfig.OrgId = _identityProvider.GetPeerIdentity().OrgId;
                    caconfig.Certificate = ca;
                    var response = _invokeBLL.RegistMember(request.ChannelId, caconfig);
                    return Task.FromResult(new RegistReply()
                    {
                        Status = true,
                        Msg = "注册成功!"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Task.FromResult(new RegistReply()
                {
                    Status = false,
                    Msg = ex.Message
                });
            }
        }

        #endregion
    }
}
