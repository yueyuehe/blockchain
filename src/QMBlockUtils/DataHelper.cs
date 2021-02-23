using System;
using System.Security.Cryptography;
using System.Text;

namespace QMBlockUtils
{
    public class DataHelper
    {
        public static string GenerateMD5(string txt)
        {
            using (MD5 mi = MD5.Create())
            {
                byte[] buffer = Encoding.Default.GetBytes(txt);
                //开始加密
                byte[] newBuffer = mi.ComputeHash(buffer);
                StringBuilder sb = new StringBuilder();
                return Convert.ToBase64String(newBuffer);
            }
        }

        /// <summary>
        /// 创建RSA公钥私钥
        /// </summary>
        public static string[] CreateRSAKey()
        {
            //创建RSA对象
            using (var rsa = RSA.Create())
            {
                var privateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
                var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
                return new string[] { privateKey, publicKey };
            }
        }

        /// <summary>
        /// 使用RSA实现加密
        /// </summary>
        /// <param name="data">加密数据</param>
        /// <returns></returns>
        public string RSAEncrypt(string data, string publicKey)
        {
            //C#默认只能使用[公钥]进行加密(想使用[公钥解密]可使用第三方组件BouncyCastle来实现)
            //创建RSA对象并载入[公钥]
            RSACryptoServiceProvider rsaPublic = new RSACryptoServiceProvider();
            rsaPublic.FromXmlString(publicKey);
            //对数据进行加密
            byte[] publicValue = rsaPublic.Encrypt(Encoding.UTF8.GetBytes(data), false);
            string publicStr = Convert.ToBase64String(publicValue);//使用Base64将byte转换为string
            return publicStr;
        }

        /// <summary>
        /// 使用RSA实现解密
        /// </summary>
        /// <param name="data">解密数据</param>
        /// <returns></returns>
        public string RSADecrypt(string data, string privateKey)
        {
            //C#默认只能使用[私钥]进行解密(想使用[私钥加密]可使用第三方组件BouncyCastle来实现)
            //创建RSA对象并载入[私钥]
            RSACryptoServiceProvider rsaPrivate = new RSACryptoServiceProvider();
            rsaPrivate.FromXmlString(privateKey);
            //对数据进行解密
            byte[] privateValue = rsaPrivate.Decrypt(Convert.FromBase64String(data), false);//使用Base64将string转换为byte
            string privateStr = Encoding.UTF8.GetString(privateValue);
            return privateStr;
        }

        /// <summary>
        /// 数字签名
        /// </summary>
        /// <param name="plaintext">原文</param>
        /// <param name="privateKey">私钥</param>
        /// <returns>签名</returns>
        public static string RSASignature(string plaintext, string privateKey)
        {
            return "test rsa";
            try
            {
                var digest = GenerateMD5(plaintext);
                UnicodeEncoding ByteConverter = new UnicodeEncoding();
                byte[] dataToEncrypt = ByteConverter.GetBytes(digest);
                using (RSACryptoServiceProvider RSAalg = new RSACryptoServiceProvider())
                {
                    RSAalg.FromXmlString(privateKey);
                    //使用SHA1进行摘要算法，生成签名
                    byte[] encryptedData = RSAalg.SignData(dataToEncrypt, new SHA1CryptoServiceProvider());
                    return Convert.ToBase64String(encryptedData);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// 验证签名
        /// </summary>
        /// <param name="plaintext">原文</param>
        /// <param name="SignedData">签名</param>
        /// <param name="publicKey">公钥</param>
        /// <returns></returns>
        public static bool VerifySignature(string plaintext, string SignedData, string publicKey)
        {
            var digest = GenerateMD5(plaintext);
            using (RSACryptoServiceProvider RSAalg = new RSACryptoServiceProvider())
            {
                RSAalg.FromXmlString(publicKey);
                UnicodeEncoding ByteConverter = new UnicodeEncoding();
                byte[] dataToVerifyBytes = ByteConverter.GetBytes(digest);
                byte[] signedDataBytes = Convert.FromBase64String(SignedData);
                return RSAalg.VerifyData(dataToVerifyBytes, new SHA1CryptoServiceProvider(), signedDataBytes);
            }
        }


    }
}


