using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ConsoleApp1
{

    public class RasHelper
    {
        public static string GenRSAKeyPair()
        {
            var generator = new RsaKeyPairGenerator();
            var seed = Encoding.UTF8.GetBytes("");
            var secureRandom = new SecureRandom();
            secureRandom.SetSeed(seed);
            generator.Init(new KeyGenerationParameters(secureRandom, 4096));
            var pair = generator.GenerateKeyPair();
            //第一种方案
            //var privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(pair.Private);
            //var serializedPrivateBytes = privateKeyInfo.ToAsn1Object().GetDerEncoded();
            //var serializedPrivate = Convert.ToBase64String(serializedPrivateBytes);
            //Console.WriteLine("Private Key：" + serializedPrivate);

            //var publickKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(pair.Public);
            //var serializedPublicBytes = publickKeyInfo.ToAsn1Object().GetDerEncoded();
            //var serializedPublic = Convert.ToBase64String(serializedPublicBytes);
            //Console.WriteLine("Public Key：" + serializedPublic);

            //第二种方案
            var twPrivate = new StringWriter();
            PemWriter pwPrivate = new PemWriter(twPrivate);
            pwPrivate.WriteObject(pair.Private);
            pwPrivate.Writer.Flush();
            var privateKey = twPrivate.ToString();
            Console.WriteLine("Private Key：" + privateKey);

            var twPublic = new StringWriter();
            PemWriter pwPublic = new PemWriter(twPublic);
            pwPublic.WriteObject(pair.Public);
            pwPublic.Writer.Flush();
            var publicKey = twPublic.ToString();
            Console.WriteLine("Public Key：" + publicKey);
            return privateKey;
        }
    }
}
