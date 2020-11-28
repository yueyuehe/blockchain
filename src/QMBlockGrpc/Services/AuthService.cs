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
     * 1.client����֤���ʶ
     * 2.server����֤���ʶ��ȡ֤�鼰�û�������Ϣ
     * 3.server��������ַ�����Ȼ���ַ����ͱ�ʶ���浽�����У�30��
     * 4.client�����Ż����ַ��������Լ���˽Կ����ǩ����Ȼ��ǩ���ͱ�ʶ���͸�server
     * 5.server���õ�ǩ�����ǩ��������֤���ɹ��򷵻�token�����ұ���token����Ϣ
     * 6.client����token������������
     * 7.server����token�������û�������Ȩ�޷���
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
        /// ��ȡ����ַ��� ����ǩ��У�� �� ֤���ʶ��
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<AuthReply> GetCode(AuthRequest request, ServerCallContext context)
        {
            try
            {
                Certificate ca = null;
                //��ȡ����
                var config = await _queryBll.QueryChannelConfigAsync(request.Data);

                #region �������л�ȡ֤��

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

                #region ��������ַ��� ������� ����
                var code = Guid.NewGuid().ToString();
                ca.SignatureValue = code;
                //һ��������Ч
                _cache.Set(request.CaNumber, ca, TimeSpan.FromSeconds(60));
                //����
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
        /// ǩ��У��ͨ���� ����token
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
                //��֤ǩ��
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
                //30������Ч
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

        //���� ��¼ȷ�Ϻ�tokenˢ��
        [Authorize]
        public override Task<AuthReply> RefreshToken(AuthRequest request, ServerCallContext context)
        {
            try
            {
                //data û��token ��ʶ��login�ж� �ܽ����ͱ�ʾclient��������
                if (string.IsNullOrEmpty(request.Data))
                {
                    return Task.FromResult(new AuthReply() { Status = true });
                }
                //data ������ ���Ҹ�������token ������token�ҵ�CA֤�飬��������token����
                //�����е�tokenˢ�� 
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
                issuer: identity.Name,   //ǩ����
                audience: ca.TBSCertificate.Subject, //������
                claims: claims,         //�Զ�������
                notBefore: DateTime.Now,//xxx����֮ǰ��������
                expires: DateTime.Now.AddMinutes(30),//token����������
                signingCredentials: creds);
            //"iss" #�Ǳ��롣issuer ����ʵ�壬�����Ƿ���������û�����Ϣ��Ҳ����jwt��ǩ���ߡ�
            //"iat" #�Ǳ��롣issued at�� token����ʱ�䣬unixʱ�����ʽ
            //"exp" #�Ǳ��롣expire ָ��token���������ڡ�unixʱ�����ʽ
            //"aud" #�Ǳ��롣���ո�JWT��һ����
            //"sub" #�Ǳ��롣��JWT��������û�
            //"nbf" #�Ǳ��롣not before�������ǰʱ����nbf���ʱ��֮ǰ����Token�������ܣ�һ�㶼����һЩ��أ����缸���ӡ�
            //"jti" #�Ǳ��롣JWT ID����Ե�ǰtoken��Ψһ��ʶ
            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        #region CA֤�鴴����ע��


        /// <summary>
        /// �����˺� pk-֤��
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
                        PravateKey = "�˺Ż��û�������"
                    });
                }
                //��CA�û��򴴽��˺�
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

                //������Ǵ���peer�ڵ�ĸ�֤�� ����Ҫ��֤peer�ڵ�����
                if (request.AccountType != "0")
                {
                    var identity = _identityProvider.GetPeerIdentity();
                    if (!identity.Valid())
                    {
                        throw new Exception("���У��ʧ��");
                    }
                }

                //�����˺���������֤�� ��֤������ǩ��,�����Ǹ�֤��ǩ��
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

        //��member���͵�֤����ص�org�����peer�ڵ�
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
                //��ȡ֤��
                var ca = Newtonsoft.Json.JsonConvert.DeserializeObject<Certificate>(request.Certificate);
                //У��ǩ��
                var rs = RSAHelper.VerifyData(_identityProvider.GetPublicKey(), ca.TBSCertificate, ca.SignatureValue);
                //�����peer�ڵ����� �򷵻�false
                //peer�ڵ���������֯����ͨ����ʱ�����У��
                if (ca.TBSCertificate.CAType == CAType.Peer)
                {
                    return Task.FromResult(new RegistReply()
                    {
                        Status = false,
                        Msg = "ǩ��У��ʧ��"
                    });
                }
                else
                {
                    //��֤������ 
                    var caconfig = new OrgMemberConfig();
                    caconfig.Name = ca.TBSCertificate.Subject;
                    caconfig.OrgId = _identityProvider.GetPeerIdentity().OrgId;
                    caconfig.Certificate = ca;
                    var response = _invokeBLL.RegistMember(request.ChannelId, caconfig);
                    return Task.FromResult(new RegistReply()
                    {
                        Status = true,
                        Msg = "ע��ɹ�!"
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
