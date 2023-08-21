using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;

namespace AzureAADSource.Infrastructure
{
    public class CipherTools
    {
        private int _nonceSize = 96;
        private int _macSize = 128;
        private int _tagSize = 128;
        private bool _useSystemCipher;
        private SecureRandom _random = new SecureRandom();

        public CipherTools(IConfiguration configuration)
        {
            _useSystemCipher = configuration.GetValue<bool>("UseSystemCipher");
        }

        #region --- Private methods ---

        private byte[] EncryptWithBCAes(byte[] messageToEncrypt, byte[] key, byte[]? nonce = null)
        {
            if (nonce == null)
            {
                //Using random nonce large enough not to repeat
                nonce = new byte[_nonceSize / 8];
                _random.NextBytes(nonce, 0, nonce.Length);
            }

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

        private byte[] DecryptWithBCAes(byte[] encryptedMessage, byte[] key)
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

        private byte[] EncryptWithSystemAes(byte[] messageToEncrypt, byte[] key, byte[]? nonce = null)
        {
            if (nonce == null)
            {
                //Using random nonce large enough not to repeat
                nonce = new byte[_nonceSize / 8];
                _random.NextBytes(nonce, 0, nonce.Length);
            }

            byte[] cipherText = new byte[messageToEncrypt.Length];
            byte[] tag = new byte[_tagSize / 8];

            var crypto = new System.Security.Cryptography.AesGcm(key);
            crypto.Encrypt(nonce, messageToEncrypt, cipherText, tag, null);

            //Assemble Message
            using (var combinedStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(combinedStream))
                {
                    //Prepend Nonce
                    binaryWriter.Write(nonce);
                    //Write Cipher Text
                    binaryWriter.Write(cipherText);
                    //Append Tag
                    binaryWriter.Write(tag);
                }
                return combinedStream.ToArray();
            }
        }

        private byte[] DecryptWithSystemAes(byte[] encryptedMessage, byte[] key)
        {
            using (var cipherStream = new MemoryStream(encryptedMessage))
            {
                using (var cipherReader = new BinaryReader(cipherStream))
                {
                    //Grab Nonce
                    var nonce = cipherReader.ReadBytes(_nonceSize / 8);

                    //Grab tag
                    var tag = encryptedMessage[(encryptedMessage.Length - (_tagSize / 8))..];

                    //Decrypt Cipher Text
                    var cipherText = cipherReader.ReadBytes(encryptedMessage.Length - nonce.Length - tag.Length);
                    var plainText = new byte[cipherText.Length];

                    var crypto = new System.Security.Cryptography.AesGcm(key);

                    crypto.Decrypt(nonce, cipherText, tag, plainText, null);

                    return plainText;
                }
            }
        }

        private byte[] EncryptWithBCChaChaPoly(byte[] messageToEncrypt, byte[] key)
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

        private byte[] DecryptWithBCChaChaPoly(byte[] encryptedMessage, byte[] key)
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

        /// <summary>
        /// Nepoužívat na Windows, ChaCha20Poly1305 hlásí, že je algorytmus nedostupný na platformě
        /// </summary>
        private byte[] EncryptWithSystemChaChaPoly(byte[] messageToEncrypt, byte[] key)
        {
            if (!System.Security.Cryptography.ChaCha20Poly1305.IsSupported)
            {
                return new byte[0];
            }
            //Using random nonce large enough not to repeat
            var nonce = new byte[_nonceSize / 8];
            _random.NextBytes(nonce, 0, nonce.Length);

            byte[] cipherText = new byte[messageToEncrypt.Length];
            byte[] tag = new byte[_tagSize / 8];

            var crypto = new System.Security.Cryptography.ChaCha20Poly1305(key);
            crypto.Encrypt(nonce, messageToEncrypt, cipherText, tag, null);

            //Assemble Message
            using (var combinedStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(combinedStream))
                {
                    //Prepend Nonce
                    binaryWriter.Write(nonce);
                    //Write Cipher Text
                    binaryWriter.Write(cipherText);
                    //Append Tag
                    binaryWriter.Write(tag);
                }
                return combinedStream.ToArray();
            }
        }

        /// <summary>
        /// Nepoužívat na Windows, ChaCha20Poly1305 hlásí, že je algorytmus nedostupný na platformě
        /// </summary>
        private byte[] DecryptWithSystemChaChaPoly(byte[] encryptedMessage, byte[] key)
        {
            if (!_useSystemCipher)
            {
                return new byte[0];
            }

            using (var cipherStream = new MemoryStream(encryptedMessage))
            {
                using (var cipherReader = new BinaryReader(cipherStream))
                {
                    //Grab Nonce
                    var nonce = cipherReader.ReadBytes(_nonceSize / 8);

                    //Grab tag
                    var tag = encryptedMessage[(encryptedMessage.Length - (_tagSize / 8))..];

                    //Decrypt Cipher Text
                    var cipherText = cipherReader.ReadBytes(encryptedMessage.Length - nonce.Length - tag.Length);
                    var plainText = new byte[cipherText.Length];

                    var crypto = new System.Security.Cryptography.ChaCha20Poly1305(key);

                    crypto.Decrypt(nonce, cipherText, tag, plainText, null);

                    return plainText;
                }
            }
        }

        #endregion

        #region --- Public methods ---

        public AsymmetricCipherKeyPair GenerateKeyPair()
        {
            X9ECParameters ecParams = ECNamedCurveTable.GetByName("secp256r1");
            var ecDomainParams = new ECDomainParameters(
            ecParams.Curve, ecParams.G, ecParams.N, ecParams.H, ecParams.GetSeed());

            ECKeyGenerationParameters ecKeyGenParams = new ECKeyGenerationParameters(ecDomainParams, _random);
            ECKeyPairGenerator ecKeyPairGen = new ECKeyPairGenerator();
            ecKeyPairGen.Init(ecKeyGenParams);
            AsymmetricCipherKeyPair ecKeyPair = ecKeyPairGen.GenerateKeyPair();
            return ecKeyPair;
        }

        public string? GetPublicKeyPem(AsymmetricCipherKeyPair keyPair)
        {
            TextWriter textWriter = new StringWriter();
            var pemWriter = new PemWriter(textWriter);
            pemWriter.WriteObject(keyPair.Public);
            pemWriter.Writer.Flush();
            return textWriter.ToString();
        }

        public string? GetPrivateKeyPem(AsymmetricCipherKeyPair keyPair)
        {
            TextWriter textWriter = new StringWriter();
            var pemWriter = new PemWriter(textWriter);
            pemWriter.WriteObject(keyPair.Private);
            pemWriter.Writer.Flush();
            return textWriter.ToString();
        }

        public ECPrivateKeyParameters ReadPrivateKey(string pem)
        {
            PemReader pemReader = new PemReader(new StringReader(pem));
            AsymmetricCipherKeyPair keyPair = (AsymmetricCipherKeyPair)pemReader.ReadObject();
            ECPrivateKeyParameters privateKey = (ECPrivateKeyParameters)keyPair.Private;
            return privateKey;
        }

        public ECPublicKeyParameters ReadPublicKey(string pem)
        {
            PemReader pemReader = new PemReader(new StringReader(pem));
            ECPublicKeyParameters publicKey = (ECPublicKeyParameters)pemReader.ReadObject();
            return publicKey;
        }

        public byte[] DeriveKey(ECPrivateKeyParameters privateKey, ECPublicKeyParameters publicKey)
        {
            ECDHCBasicAgreement keyAgreement = new ECDHCBasicAgreement();
            keyAgreement.Init(privateKey);
            BigInteger secret = keyAgreement.CalculateAgreement(publicKey);
            var inKey = secret.ToByteArrayUnsigned();

            HkdfParameters parameters = new HkdfParameters(inKey, null, null);
            HkdfBytesGenerator hkdf = new HkdfBytesGenerator(new Sha256Digest());
            hkdf.Init(parameters);
            byte[] derivedKey = new byte[32];
            hkdf.GenerateBytes(derivedKey, 0, 32);
            return derivedKey;
        }

        public byte[] GenerateRandomNonce()
        {
            var nonce = new byte[_nonceSize / 8];
            _random.NextBytes(nonce, 0, nonce.Length);
            return nonce;
        }

        public byte[] EncryptWithAes(byte[] messageToEncrypt, byte[] key, byte[]? nonce = null, bool? forceSystem = null)
        {
            return (forceSystem ?? _useSystemCipher) ? EncryptWithSystemAes(messageToEncrypt, key, nonce) : EncryptWithBCAes(messageToEncrypt, key, nonce);
        }

        public byte[] DecryptWithAes(byte[] encryptedMessage, byte[] key, bool? forceSystem = null)
        {
            return (forceSystem ?? _useSystemCipher) ? DecryptWithSystemAes(encryptedMessage, key) : DecryptWithBCAes(encryptedMessage, key);
        }

        public byte[] EncryptWithChaChaPoly(byte[] messageToEncrypt, byte[] key, bool? forceSystem = null)
        {
            return (forceSystem ?? _useSystemCipher) ? EncryptWithSystemChaChaPoly(messageToEncrypt, key) : EncryptWithBCChaChaPoly(messageToEncrypt, key);
        }

        public byte[] DecryptWithChaChaPoly(byte[] encryptedMessage, byte[] key, bool? forceSystem = null)
        {
            return (forceSystem ?? _useSystemCipher) ? DecryptWithSystemChaChaPoly(encryptedMessage, key) : DecryptWithBCChaChaPoly(encryptedMessage, key);
        }

        #endregion
    }
}
