using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Newtonsoft.Json.MediaTypeFormatter.Extensions
{
    public static class HttpRequestMessageExtension
    {
        private const string HttpContext = "MS_HttpContext";
        private const string RemoteEndpointMessage = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";

        public static string GetClientIpAddress(this HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey(HttpContext))
            {
                dynamic ctx = request.Properties[HttpContext];
                if (ctx != null)
                {
                    return ctx.Request.UserHostAddress;
                }
            }

            if (request.Properties.ContainsKey(RemoteEndpointMessage))
            {
                dynamic remoteEndpoint = request.Properties[RemoteEndpointMessage];
                if (remoteEndpoint != null)
                {
                    return remoteEndpoint.Address;
                }
            }

            string ip = null;
            var csvList = GetHeaderValueAs<string>(request, "X-Forwarded-For");
            ip = SplitCsv(csvList).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(ip))
            {
                ip = GetHeaderValueAs<string>(request, "REMOTE_ADDR");
            }
            if (!string.IsNullOrEmpty(ip) && ip.StartsWith("::ffff:") && ip.Length > 7)
            {
                ip = ip.Substring(7);
            }

            return ip;

        }

        private static T GetHeaderValueAs<T>(HttpRequestMessage httpRequest, string headerName)
        {
            IEnumerable<string> values;
            if (httpRequest?.Headers.TryGetValues(headerName, out values) ?? false)
            {
                string rawValues = values.ToString();
                if (!string.IsNullOrWhiteSpace(rawValues))
                {
                    return (T)Convert.ChangeType(rawValues, typeof(T));
                }
            }
            return default(T);
        }

        public static List<string> SplitCsv(string csvList, bool nullOrWhitespaceReturnNull = false)
        {
            if (string.IsNullOrWhiteSpace(csvList))
            {
                return nullOrWhitespaceReturnNull ? null : new List<string>();
            }
            return csvList.TrimEnd(',').Split(',').AsEnumerable().Select(s => s.Trim()).ToList();
        }
    }
}
