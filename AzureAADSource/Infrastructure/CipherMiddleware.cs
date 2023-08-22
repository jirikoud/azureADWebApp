using System.Text;

namespace AzureAADSource.Infrastructure
{
    public class CipherMiddleware
    {
        private readonly RequestDelegate _next;
        private CipherTools _cipherTools;
        private const string serverPrivateKeyPem = "-----BEGIN EC PRIVATE KEY-----\r\nMIIBaAIBAQQgFJU4mP0PhwKmWlMuBOlqn1tYaCFPuotpBMC6defPpa2ggfowgfcC\r\nAQEwLAYHKoZIzj0BAQIhAP////8AAAABAAAAAAAAAAAAAAAA////////////////\r\nMFsEIP////8AAAABAAAAAAAAAAAAAAAA///////////////8BCBaxjXYqjqT57Pr\r\nvVV2mIa8ZR0GsMxTsPY7zjw+J9JgSwMVAMSdNgiG5wSTamZ44ROdJreBn36QBEEE\r\naxfR8uEsQkf4vOblY6RA8ncDfYEt6zOg9KE5RdiYwpZP40Li/hp/m47n60p8D54W\r\nK84zV2sxXs7LtkBoN79R9QIhAP////8AAAAA//////////+85vqtpxeehPO5ysL8\r\nYyVRAgEBoUQDQgAEnf0O6t2D1/UOj1EUGFsMaQLqXXMWxbwYWmc7kGQlOLGL3Qcj\r\nVms5ws/Te6KklMXPYXO7A+NVeRqarjGohM0GLA==\r\n-----END EC PRIVATE KEY-----";
        private const string mobilePublicKeyPem = "-----BEGIN PUBLIC KEY-----\r\nMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEbMECUW9nmiTCqouARPQGXr8Auy9R\r\n1JDIKyrLItxIsUYny8MVpvjLV1ASoyLHfRPGEmTIbg0Tq9r+Oj9XT5Oq1w==\r\n-----END PUBLIC KEY-----";

        public CipherMiddleware(RequestDelegate next, CipherTools cipherTools)
        {
            _cipherTools = cipherTools;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var blockResponseCipher = context.Request.Headers["no-cipher"] == "1";
                var useResponseCipher = context.Request.Path.StartsWithSegments(PathString.FromUriComponent("/api/cipher")) && !blockResponseCipher;

                var privateKey = _cipherTools.ReadPrivateKey(serverPrivateKeyPem);
                var publicKey = _cipherTools.ReadPublicKey(mobilePublicKeyPem);
                byte[] derivedKey = _cipherTools.DeriveKey(privateKey, publicKey);

                if (context.Request.ContentType == "application/json+encrypted")
                {
                    byte[]? inMessage = null;
                    if (context.Request.ContentLength > 0)
                    {
                        var memoryStream = new MemoryStream();
                        byte[] buffer = new byte[100000];
                        while (true)
                        {
                            var read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
                            memoryStream.Write(buffer, 0, read);
                            if (read < buffer.Length)
                            {
                                break;
                            }
                        }
                        inMessage = memoryStream.ToArray();
                    }

                    //var testModel = new CipherMessageRequest() { Message = "Zkouska zpravy" };
                    //var testText = JsonSerializer.Serialize(testModel);
                    //var testMessage = Encoding.UTF8.GetBytes(testText);
                    //var testData = _cipherTools.EncryptWithChaChaPoly(testMessage, derivedKey);
                    //File.WriteAllBytes("d:\\Temp\\mPatient\\data.dat", testData);

                    if (inMessage != null)
                    {
                        //Rozšifrovat zprávu
                        var decryptedMessage = _cipherTools.DecryptWithChaChaPoly(inMessage, derivedKey);
                        var message = Encoding.UTF8.GetString(decryptedMessage);

                        context.Request.Body = new MemoryStream(decryptedMessage);
                        context.Request.ContentType = "application/json";
                        context.Request.ContentLength = message.Length;
                    }
                    else
                    {
                        context.Request.Body = new MemoryStream();
                        context.Request.ContentType = "application/json";
                        context.Request.ContentLength = 0;
                    }
                }

                MemoryStream responseStream = new MemoryStream();
                var originalBody = context.Response.Body;
                if (useResponseCipher)
                { 
                    context.Response.Body = responseStream;
                }

                // Call the next delegate/middleware in the pipeline.
                await _next(context);

                if (useResponseCipher)
                {
                    if (context.Response.StatusCode == 200)
                    {
                        var outMessage = responseStream.ToArray();
                        var cipherResponse = _cipherTools.EncryptWithChaChaPoly(outMessage, derivedKey);
                        await originalBody.WriteAsync(cipherResponse);
                    }
                    else
                    {
                        responseStream.CopyTo(originalBody);
                    }
                    context.Response.Body = originalBody;
                }
            }
            catch (Exception exception)
            {

            }
        }
    }

    public static class CipherMiddlewareExtensions
    {
        public static IApplicationBuilder UseCipher(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CipherMiddleware>();
        }
    }
}
