using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using System.Buffers.Text;
using System.IO;
using System.Security.Cryptography;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using System;

namespace AzureAADSource.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CipherController : ControllerBase
    {
        private int _nonceSize = 96;
        private int _macSize = 128;
        private SecureRandom _random = new SecureRandom();

        private byte[] EncryptWithKey(byte[] messageToEncrypt, byte[] key)
        {
            //Using random nonce large enough not to repeat
            var nonce = new byte[_nonceSize / 8];
            _random.NextBytes(nonce, 0, nonce.Length);

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), _macSize, nonce, null);
            cipher.Init(true, parameters);

            //Generate Cipher Text With Auth Tag
            var cipherText = new byte[cipher.GetOutputSize(messageToEncrypt.Length)];
            var len = cipher.ProcessBytes(messageToEncrypt, 0, messageToEncrypt.Length, cipherText, 0);
            cipher.DoFinal(cipherText, len);

            //Assemble Message
            using (var combinedStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(combinedStream))
                {
                    //Prepend Nonce
                    binaryWriter.Write(nonce);
                    //Write Cipher Text
                    binaryWriter.Write(cipherText);
                }
                return combinedStream.ToArray();
            }
        }


        private byte[] DecryptWithKey(byte[] encryptedMessage, byte[] key)
        {
            using (var cipherStream = new MemoryStream(encryptedMessage))
            {
                using (var cipherReader = new BinaryReader(cipherStream))
                {
                    //Grab Nonce
                    var nonce = cipherReader.ReadBytes(_nonceSize / 8);

                    var cipher = new GcmBlockCipher(new AesEngine());
                    var parameters = new AeadParameters(new KeyParameter(key), _macSize, nonce);
                    cipher.Init(false, parameters);

                    //Decrypt Cipher Text
                    var cipherText = cipherReader.ReadBytes(encryptedMessage.Length - nonce.Length);
                    var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];

                    var len = cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0);
                    cipher.DoFinal(plainText, len);

                    return plainText;
                }
            }
        }

        public IActionResult Get()
        {
            try
            {
                string privateAlice = "-----BEGIN EC PRIVATE KEY-----\r\nMHcCAQEEINjJKI/Y+anyZu9SD55wkfxO1wTWu5lhJsV9r4m67yW4oAoGCCqGSM49\r\nAwEHoUQDQgAEJNXEFVDaFoF3cx7YnfmNQhrkhoEzyaZpLhv+ri8bFXE8EL67FcOY\r\nz94MHNHDv1XcW1sbWvCCxW74BpWcZayyIw==\r\n-----END EC PRIVATE KEY-----";
                string publicBob = "-----BEGIN PUBLIC KEY-----\r\nMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEbMECUW9nmiTCqouARPQGXr8Auy9R\r\n1JDIKyrLItxIsUYny8MVpvjLV1ASoyLHfRPGEmTIbg0Tq9r+Oj9XT5Oq1w==\r\n-----END PUBLIC KEY-----";
                string cipheredText = "XP0kOOq82qZc7NDSUklEp9Nwivux9KEvpUVF+lgCv02geii6lt5vsf2T29AF";
                string inKeyTest = "97PPgKfhkMAGPDP1mXQSFGWAvlLcqgq0EHerGem6LvA=";
                string derivedKeyTest = "OP5dPKzscBrv3XodGGbjsyUsOeNjaAgtnadgr7Eb82U=";
                string message = "Zkouška sirén";

                X9ECParameters ecParams = ECNamedCurveTable.GetByName("secp256r1");
                var ecDomainParams = new ECDomainParameters(
                ecParams.Curve, ecParams.G, ecParams.N, ecParams.H, ecParams.GetSeed());

                ECKeyGenerationParameters ecKeyGenParams = new ECKeyGenerationParameters(ecDomainParams, _random);
                ECKeyPairGenerator ecKeyPairGen = new ECKeyPairGenerator();
                ecKeyPairGen.Init(ecKeyGenParams);
                AsymmetricCipherKeyPair ecKeyPair = ecKeyPairGen.GenerateKeyPair();

                TextWriter textWriter = new StringWriter();
                var pemWriter = new PemWriter(textWriter);
                pemWriter.WriteObject(ecKeyPair.Public);
                pemWriter.Writer.Flush();
                string? privateKey = textWriter.ToString();

                PemReader pemReaderAlice = new PemReader(new StringReader(privateAlice));
                AsymmetricCipherKeyPair keyPairAlice = (AsymmetricCipherKeyPair)pemReaderAlice.ReadObject();
                ECPrivateKeyParameters privateKeyParamsAlice = (ECPrivateKeyParameters)keyPairAlice.Private;

                PemReader pemReaderBob = new PemReader(new StringReader(publicBob));
                ECPublicKeyParameters publicKeyParamsBob = (ECPublicKeyParameters)pemReaderBob.ReadObject();

                ECDHCBasicAgreement keyAgreement = new ECDHCBasicAgreement();
                keyAgreement.Init(privateKeyParamsAlice);
                BigInteger secret = keyAgreement.CalculateAgreement(publicKeyParamsBob);
                var inKey = secret.ToByteArrayUnsigned();

                var derivedKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, inKey, 32, null, null);
                var derivedKeyText = Convert.ToBase64String(derivedKey);


                //derivedKey = Convert.FromBase64String("15zJkw8Dyr1w4iQZw92miip3CQBWlVkpiJUsZacdexs=");

                var encryptedData = Convert.FromBase64String(cipheredText);

                //var encryptedData = EncryptWithKey(System.Text.Encoding.UTF8.GetBytes(message), derivedKey);
                var encryptedText = Convert.ToBase64String(encryptedData);


                var decryptedData = DecryptWithKey(encryptedData, derivedKey);


                //byte[] decryptedData = new byte[data.Length];
                //byte[] nonce = Convert.FromBase64String(nonceText);
                //byte[] tag = Convert.FromBase64String(tagText);

                //var crypto = new AesGcm(derivedKey);
                //crypto.Decrypt(nonce, data, tag, decryptedData, null);

                var decryptedText = System.Text.Encoding.UTF8.GetString(decryptedData);

                Console.WriteLine("The file was encrypted.");
                return Ok(decryptedText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The encryption failed. {ex}");
            }
            return Ok();
        }
    }
}
