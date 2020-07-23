using System;
using System.Security.Cryptography;
using System.Text;

namespace QMBlockUtils
{
    public class RSAHelper
    {
        /**
         * 使用场景
         *  1.计算T的hash
         *  2.使用private对数据加密
         *  3.使用公钥对private加密的数据解密，验证
         */

        /// <summary>
        /// 对一段数据进行签名
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string SignData<T>(string privateKey, T data) where T : new()
        {
            var str = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            var md5 = RSAHelper.GenerateMD5Byte(str);
            using (var rsa = RSA.Create())
            {
                rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKey), out int bytesRead);
                var rs = rsa.SignData(md5, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                return Convert.ToBase64String(rs);
            }
        }

        public static string SignData(string privateKey, string data)
        {
            var md5 = RSAHelper.GenerateMD5Byte(data);
            using (var rsa = RSA.Create())
            {
                rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKey), out int bytesRead);
                var rs = rsa.SignData(md5, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                return Convert.ToBase64String(rs);
            }
        }


        /// <summary>
        /// 签名校验
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="signData"></param>
        /// <returns></returns>
        public static bool VerifyData<T>(string publicKey, T data, string signData) where T : new()
        {
            var str = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            var md5 = RSAHelper.GenerateMD5Byte(str);
            using (var rsa = RSA.Create())
            {
                rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out int bytesRead);
                return rsa.VerifyData(md5, Convert.FromBase64String(signData), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            }
        }

        public static bool VerifyData(string publicKey, string data, string signData)
        {
            var md5 = RSAHelper.GenerateMD5Byte(data);
            using (var rsa = RSA.Create())
            {
                rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out int bytesRead);
                return rsa.VerifyData(md5, Convert.FromBase64String(signData), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            }
        }

        private static byte[] GenerateMD5Byte(string txt)
        {
            using (MD5 mi = MD5.Create())
            {
                byte[] buffer = Encoding.Default.GetBytes(txt);
                //开始加密
                return mi.ComputeHash(buffer);
            }
        }

        public static string GenerateMD5(string txt)
        {
            using (MD5 mi = MD5.Create())
            {
                byte[] buffer = Encoding.Default.GetBytes(txt);
                //开始加密
                byte[] newBuffer = mi.ComputeHash(buffer);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < newBuffer.Length; i++)
                {
                    sb.Append(newBuffer[i].ToString("x2"));
                }
                return sb.ToString();
               // return Convert.ToBase64String(newBuffer);
            }
        }

        public static string[] CreateAccount()
        {
            using (var rsa = RSA.Create())
            {
                var privateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
                var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
                return new string[] { privateKey, publicKey };
            }
        }
   
    }
}
