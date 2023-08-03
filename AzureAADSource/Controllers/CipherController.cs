using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Buffers.Text;
using System.IO;
using System.Security.Cryptography;
using System.Text.Unicode;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AzureAADSource.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CipherController : ControllerBase
    {
        public IActionResult Get()
        {
            try
            {
                string privateAlice = "-----BEGIN PRIVATE KEY-----MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQg5f/l0osQj9mV3kZUpEKMK/iY5JplkHOAhvj7jzCyv/ShRANCAARswQJRb2eaJMKqi4BE9AZevwC7L1HUkMgrKssi3EixRifLwxWm+MtXUBKjIsd9E8YSZMhuDROr2v46P1dPk6rX-----END PRIVATE KEY-----";
                string privateBob = "-----BEGIN PRIVATE KEY-----\r\nMIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQggja4Ej9TkxtlcFTA\r\nddILSDDbx6FQTD5fd5jd9sg31guhRANCAAQoX+YTzy3AGr9cctHjQZ9MaSz2kk2f\r\nCd3mKMJ1N0BLvx4LLrw3r/GFlVXVQS4v78Lt+iyXQ3mUsm9znmhoEdly\r\n-----END PRIVATE KEY-----";
                string cipheredText = "Srr23zad688FBpbTMbyjxKQ=";
                string nonceText = "0zHvUkrFAGqlfp2K";
                string tagText = "+ZSghUV/tbyJGVPXZ14TTg==";

                //CngKey bobPrivateKey = CngKey.Import(Convert.FromBase64String(bobPrivate), CngKeyBlobFormat.GenericPrivateBlob);
                //ECDiffieHellmanCng crypto = new ECDiffieHellmanCng(bobPrivateKey);

                ECDiffieHellman cryptoAlice = new ECDiffieHellmanCng(ECCurve.CreateFromFriendlyName("secp256r1"));
                var privateAliceKeyZero = cryptoAlice.ExportECPrivateKeyPem();
                cryptoAlice.ImportFromPem(privateAlice);
                var privateAliceKey = cryptoAlice.ExportECPrivateKeyPem();
                var publicAliceKey = cryptoAlice.PublicKey;

                ECDiffieHellman cryptoBob = new ECDiffieHellmanCng(ECCurve.CreateFromFriendlyName("secp256r1"));
                cryptoBob.ImportFromPem(privateBob);
                var privateBobKey = cryptoBob.ExportECPrivateKeyPem();
                var publicBobKey = cryptoBob.PublicKey;

                var inKey = cryptoAlice.DeriveKeyMaterial(publicBobKey);
                var inKeyText = Convert.ToHexString(inKey);

                //var memoryStream = new MemoryStream();
                //memoryStream.Write(Convert.FromBase64String("XlsY5scsUiUeaCMgdDfUdTffiaGfkJboCyvZoImJ8blrH+ufwNvIkaAYQkvc"));
                var derivedKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, inKey, 32, null, null);
                //CryptoStream cryptStream = new CryptoStream(memoryStream, aes.CreateDecryptor(key, iv), CryptoStreamMode.Read);

                derivedKey = Convert.FromBase64String("15zJkw8Dyr1w4iQZw92miip3CQBWlVkpiJUsZacdexs=");

                var data = Convert.FromBase64String(cipheredText);
                byte[] decryptedData = new byte[data.Length];
                byte[] nonce = Convert.FromBase64String(nonceText);
                byte[] tag = Convert.FromBase64String(tagText);

                var crypto = new AesGcm(derivedKey);
                crypto.Decrypt(nonce, data, tag, decryptedData, null);

                var decryptedText = System.Text.Encoding.UTF8.GetString(decryptedData);




                //using (FileStream fileStream = new("TestData.txt", FileMode.OpenOrCreate))
                //{
                //    using (Aes aes = Aes.Create())
                //    {
                //        byte[] key =
                //        {
                //            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                //            0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16
                //        };
                //        aes.Key = key;

                //        byte[] iv = aes.IV;
                //        fileStream.Write(iv, 0, iv.Length);

                //        using (CryptoStream cryptoStream = new(
                //            fileStream,
                //            aes.CreateEncryptor(),
                //            CryptoStreamMode.Write))
                //        {
                //            // By default, the StreamWriter uses UTF-8 encoding.
                //            // To change the text encoding, pass the desired encoding as the second parameter.
                //            // For example, new StreamWriter(cryptoStream, Encoding.Unicode).
                //            using (StreamWriter encryptWriter = new(cryptoStream))
                //            {
                //                encryptWriter.WriteLine("Hello World!");
                //            }
                //        }
                //    }
                //}

                Console.WriteLine("The file was encrypted.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The encryption failed. {ex}");
            }
            return Ok();
        }
    }
}
