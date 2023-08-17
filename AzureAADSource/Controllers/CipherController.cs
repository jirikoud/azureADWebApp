using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
//using System.Security.Cryptography;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using AzureAADSource.Models;
using System.Text.Json;
using System.Diagnostics;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using static System.Net.Mime.MediaTypeNames;
using System.Text;

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

        private byte[] EncryptWithChaChaPoly(byte[] messageToEncrypt, byte[] key)
        {
            if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes", nameof(key));

            //Using random nonce large enough not to repeat
            var nonce = new byte[_nonceSize / 8];
            _random.NextBytes(nonce, 0, nonce.Length);

            var keyMaterial = new KeyParameter(key);
            var parameters = new ParametersWithIV(keyMaterial, nonce);
            var cipher = new ChaCha20Poly1305();
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

        private byte[] DecryptWithChaChaPoly(byte[] encryptedMessage, byte[] key)
        {
            using (var cipherStream = new MemoryStream(encryptedMessage))
            {
                using (var cipherReader = new BinaryReader(cipherStream))
                {
                    //Grab Nonce
                    var nonce = cipherReader.ReadBytes(_nonceSize / 8);

                    var keyMaterial = new KeyParameter(key);
                    var parameters = new ParametersWithIV(keyMaterial, nonce);
                    var cipher = new ChaCha20Poly1305();
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

        private string GenerateLargeMessage()
        {
            var model = new JsonModel()
            {
                Title = "Secret message",
                SubTitle = "Secret subtitle",
                Items = new List<string>(),
            };
            for (int i = 0; i < 500000; i++)
            {
                model.Items.Add($"Item{_random.NextLong}");
            }
            string jsonString = JsonSerializer.Serialize(model);
            return jsonString;
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

                HkdfParameters parameters = new HkdfParameters(inKey, null, null);
                HkdfBytesGenerator hkdf = new HkdfBytesGenerator(new Sha256Digest());
                hkdf.Init(parameters);
                byte[] derivedKey = new byte[32];
                hkdf.GenerateBytes(derivedKey, 0, 32);
                var derivedKeyText = Convert.ToBase64String(derivedKey);

                message = GenerateLargeMessage();
                //derivedKey = Convert.FromBase64String("15zJkw8Dyr1w4iQZw92miip3CQBWlVkpiJUsZacdexs=");

                //var encryptedData = Convert.FromBase64String(cipheredText);

                Stopwatch stopwatch = Stopwatch.StartNew();
                var encryptedData = EncryptWithChaChaPoly(Encoding.UTF8.GetBytes(message), derivedKey);
                //var encryptedText = Convert.ToBase64String(encryptedData);

                //var decryptedData = DecryptWithChaChaPoly(encryptedData, derivedKey);
                stopwatch.Stop();
                var elapsedChaCha = stopwatch.ElapsedMilliseconds;

                stopwatch = Stopwatch.StartNew();
                encryptedData = EncryptWithKey(Encoding.UTF8.GetBytes(message), derivedKey);
                //var encryptedText = Convert.ToBase64String(encryptedData);

                //decryptedData = DecryptWithKey(encryptedData, derivedKey);
                stopwatch.Stop();
                var elapsedAes = stopwatch.ElapsedMilliseconds;

                //byte[] decryptedData = new byte[data.Length];
                //byte[] nonce = Convert.FromBase64String(nonceText);
                //byte[] tag = Convert.FromBase64String(tagText);

                //var crypto = new AesGcm(derivedKey);
                //crypto.Decrypt(nonce, data, tag, decryptedData, null);

                //var decryptedText = System.Text.Encoding.UTF8.GetString(decryptedData);

                Console.WriteLine("The file was encrypted.");
                var responseText = $"Test success, message length = {message.Length} Bytes, times: ChaChaPoly={elapsedChaCha}ms, Aes={elapsedAes}ms";
                return Ok(responseText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The encryption failed. {ex}");
            }
            return Ok();
        }
    }
}
