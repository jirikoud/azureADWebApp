using AzureAADSource.Infrastructure;
using AzureAADSource.Models;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto.Parameters;
using System.Diagnostics;
using System.Text;

namespace AzureAADSource.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CipherController : ControllerBase
    {
        private CipherTools _cipherTools;
        private int _messageItems = 800;
        private int _repeatCount = 1;

        public CipherController(CipherTools cipherTools)
        {
            _cipherTools = cipherTools;
        }

        private long ProcessSystemAesTest(string message, byte[] derivedKey, int repeatCount)
        {
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < repeatCount; i++)
            {
                var encryptedData = _cipherTools.EncryptWithAes(Encoding.UTF8.GetBytes(message), derivedKey, null, true);
                var decryptedData = _cipherTools.DecryptWithAes(encryptedData, derivedKey, true);
            }

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private long ProcessBCAesTest(string message, byte[] derivedKey, int repeatCount)
        {
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < repeatCount; i++)
            {
                var encryptedData = _cipherTools.EncryptWithAes(Encoding.UTF8.GetBytes(message), derivedKey, null, false);
                var decryptedData = _cipherTools.DecryptWithAes(encryptedData, derivedKey, false);
            }

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private long ProcessSystemChaChaPolyTest(string message, byte[] derivedKey, int repeatCount)
        {
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < repeatCount; i++)
            {
                var encryptedData = _cipherTools.EncryptWithChaChaPoly(Encoding.UTF8.GetBytes(message), derivedKey, true);
                var decryptedData = _cipherTools.DecryptWithChaChaPoly(encryptedData, derivedKey, true);
            }

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private long ProcessBCChaChaPolyTest(string message, byte[] derivedKey, int repeatCount)
        {
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < repeatCount; i++)
            {
                var encryptedData = _cipherTools.EncryptWithChaChaPoly(Encoding.UTF8.GetBytes(message), derivedKey, false);
                var decryptedData = _cipherTools.DecryptWithChaChaPoly(encryptedData, derivedKey, false);
            }

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        [HttpGet]
        public ActionResult<string> Get()
        {
            try
            {
                string privateAlice = "-----BEGIN EC PRIVATE KEY-----\r\nMHcCAQEEINjJKI/Y+anyZu9SD55wkfxO1wTWu5lhJsV9r4m67yW4oAoGCCqGSM49\r\nAwEHoUQDQgAEJNXEFVDaFoF3cx7YnfmNQhrkhoEzyaZpLhv+ri8bFXE8EL67FcOY\r\nz94MHNHDv1XcW1sbWvCCxW74BpWcZayyIw==\r\n-----END EC PRIVATE KEY-----";
                string publicBob = "-----BEGIN PUBLIC KEY-----\r\nMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEbMECUW9nmiTCqouARPQGXr8Auy9R\r\n1JDIKyrLItxIsUYny8MVpvjLV1ASoyLHfRPGEmTIbg0Tq9r+Oj9XT5Oq1w==\r\n-----END PUBLIC KEY-----";
                string message = "Zkouška sirén";

                //var ecKeyPair = _cipherTools.GenerateKeyPair();
                //string? publicKey = _cipherTools.GetPublicKeyPem(ecKeyPair);
                //string? privateKey = _cipherTools.GetPrivateKeyPem(ecKeyPair);

                ECPrivateKeyParameters privateKeyAlice = _cipherTools.ReadPrivateKey(privateAlice);
                ECPublicKeyParameters publicKeyBob = _cipherTools.ReadPublicKey(publicBob);

                byte[] derivedKey = _cipherTools.DeriveKey(privateKeyAlice, publicKeyBob);

                //TAG test
                var nonce = _cipherTools.GenerateRandomNonce();

                var testMessage = Encoding.UTF8.GetBytes("Zkouska");
                var encryptedBC = _cipherTools.EncryptWithAes(testMessage, derivedKey, nonce, false);
                var encryptedSys = _cipherTools.EncryptWithAes(testMessage, derivedKey, nonce, true);

                var decryptedBC = _cipherTools.DecryptWithAes(encryptedSys, derivedKey, false);
                var decryptedSys = _cipherTools.DecryptWithAes(encryptedBC, derivedKey, true);

                message = Utils.GenerateLargeMessage(_messageItems);
                //derivedKey = Convert.FromBase64String("15zJkw8Dyr1w4iQZw92miip3CQBWlVkpiJUsZacdexs=");

                //var encryptedData = Convert.FromBase64String(cipheredText);

                long timeSystemAes = ProcessSystemAesTest(message, derivedKey, _repeatCount);
                long timeBCAes = ProcessBCAesTest(message, derivedKey, _repeatCount);
                long timeSystemChaCha = ProcessSystemChaChaPolyTest(message, derivedKey, _repeatCount);
                long timeBCChaCha = ProcessBCChaChaPolyTest(message, derivedKey, _repeatCount);

                Console.WriteLine("The file was encrypted.");
                var responseText = $"Test success, message length = {message.Length} Bytes, times: ChaChaPoly={timeBCChaCha}ms, Aes={timeBCAes}ms, System Aes={timeSystemAes}ms, System ChaCha={timeSystemChaCha}ms";
                return Ok(responseText);
            }
            catch (Exception ex)
            {
                var errorText = $"The encryption failed. {ex}";
                Console.WriteLine(errorText);
                return Ok(errorText);
            }
        }

        [HttpPost]
        [Consumes("application/json+encrypted", "application/json")]
        public ActionResult<CipherMessageRequest> Message(CipherMessageRequest? model)
        {
            try
            {
                if (model != null)
                {
                    model.Message = "Uspesne desifrovano";
                }
                else
                {
                    model = new CipherMessageRequest()
                    {
                        Message = "Nacteni selhalo",
                    };
                }
                return Ok(model);
            }
            catch (Exception exception)
            {
                var errorText = $"The encryption failed. {exception}";
                Console.WriteLine(errorText);
                return Ok(errorText);
            }
        }
    }
}
