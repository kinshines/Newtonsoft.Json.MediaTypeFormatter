using Newtonsoft.Json.MediaTypeFormatter.Extensions;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Newtonsoft.Json.MediaTypeFormatter.Formatters
{
    public class JsonNetMediaTypeFormatter : JsonMediaTypeFormatter
    {
        public JsonSerializer Serializer { get; private set; }

        /// <summary>
        /// Specify the media types that this MediaTypeFormatter handles
        /// </summary>
        public JsonNetMediaTypeFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" });
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json") { CharSet = "utf-8" });

            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            Serializer = new JsonSerializer
            {
                ContractResolver = contractResolver
            };
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            return readStream.ReadAsJson(type, Serializer);
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, System.Net.TransportContext transportContext)
        {
            return writeStream.WriteAsJson(value, Serializer);
        }
    }
}
