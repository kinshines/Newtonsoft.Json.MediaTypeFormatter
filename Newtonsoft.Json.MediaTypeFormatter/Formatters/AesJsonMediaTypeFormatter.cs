using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Newtonsoft.Json.MediaTypeFormatter.Formatters
{
    /// <summary>
    /// JsonFormatter+AES(ECB,PKCS7,noIV)
    /// </summary>
    public class AesJsonMediaTypeFormatter : JsonMediaTypeFormatter
    {
        public JsonSerializer Serializer { get; private set; }
        readonly byte[] keyArray;
        public AesJsonMediaTypeFormatter(string encryptKey)
        {
            base.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/encrypt"));
            base.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/encrypt") { CharSet = "utf-8" });

            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };
            Serializer = new JsonSerializer
            {
                ContractResolver = contractResolver
            };
            keyArray = Encoding.UTF8.GetBytes(encryptKey);
        }

        /// <summary>
        /// AES解密后json反序列化
        /// Request.Content-Type指定text/encrypt
        /// </summary>
        /// <param name="type"></param>
        /// <param name="readStream"></param>
        /// <param name="content"></param>
        /// <param name="formatterLogger"></param>
        /// <returns></returns>
        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            // failsafe
            if (readStream == null) { return null; }

            var bytes = default(byte[]);
            using (var memstream = new MemoryStream())
            {
                readStream.CopyTo(memstream);
                bytes = memstream.ToArray();
            }

            if (bytes.Length == 0)
            {
                readStream.Position = 0;
                using (var reader = new JsonTextReader(new StreamReader(readStream)))
                {
                    //  We want to try deserialization without specifying an explicit type first,
                    //  then see if the resulting type is compatible with the type that is expected
                    //  from the Web API stack stream.
                    //  If not, then we try to read it again using an explicit type
                    //  (although it probably won't work anyway still :p)
                    if (typeof(JObject).IsSubclassOf(type))
                    {
                        return Task.FromResult(Serializer.Deserialize(reader));
                    }
                    return Task.FromResult(Serializer.Deserialize(reader, type));
                }
            }

            var plain = Encoding.UTF8.GetString(bytes);
            var decByte = DecryptByte(plain, keyArray);

            using (var memstream = new MemoryStream())
            {
                memstream.Write(decByte, 0, decByte.Length);
                memstream.Position = 0;
                using (var reader = new JsonTextReader(new StreamReader(memstream)))
                {
                    if (typeof(JObject).IsSubclassOf(type))
                    {
                        return Task.FromResult(Serializer.Deserialize(reader));
                    }
                    return Task.FromResult(Serializer.Deserialize(reader, type));
                }
            }
        }

        /// <summary>
        /// json序列化后AES加密输出
        /// Request.Accept指定text/encrypt
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="writeStream"></param>
        /// <param name="content"></param>
        /// <param name="transportContext"></param>
        /// <returns></returns>
        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            if (value == null) { return null; }

            var bytes = default(byte[]);
            using (var memstream = new MemoryStream())
            using (var writer = new JsonTextWriter(new StreamWriter(memstream)))
            {
                Serializer.Serialize(writer, value);
                writer.Flush();
                bytes = memstream.ToArray();
            }

            var base64 = EncryptByte(bytes, keyArray);
            using (var writer = new StreamWriter(writeStream))
            {
                writer.Write(base64);
                writer.Flush();
            }

            return Task.FromResult(0);
        }

        private static byte[] DecryptByte(string encrypted, byte[] keyArray)
        {
            byte[] toDecryptArray = Convert.FromBase64String(encrypted);
            using (Aes aes = Aes.Create("AES"))
            {
                aes.KeySize = keyArray.Length * 8;
                aes.Key = keyArray;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform cTransform = aes.CreateDecryptor();
                return cTransform.TransformFinalBlock(toDecryptArray, 0, toDecryptArray.Length);
            }
        }

        private static string EncryptByte(byte[] plainArray, byte[] keyArray)
        {
            using (Aes aes = Aes.Create("AES"))
            {
                aes.KeySize = keyArray.Length * 8;
                aes.Key = keyArray;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform cTransform = aes.CreateEncryptor();
                var resultArray = cTransform.TransformFinalBlock(plainArray, 0, plainArray.Length);
                return Convert.ToBase64String(resultArray);
            }
        }
    }
}
