using Newtonsoft.Json.MediaTypeFormatter.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Newtonsoft.Json.MediaTypeFormatter.Handlers
{
    public abstract class MessageHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var corrId = Thread.CurrentThread.ManagedThreadId.ToString();
            var ip = request.GetClientIpAddress();
            var requestInfo = string.Format("{0} {1} {2}", ip, request.Method, request.RequestUri);
            //ignore swagger
            if (request.RequestUri.PathAndQuery.ToLowerInvariant().Contains("/swagger"))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var start = Stopwatch.GetTimestamp();

            string requestMessage = null;
            if (CanHandleRequestContent(request))
            {
                requestMessage = await request.Content.ReadAsStringAsync();
            }
            await IncommingMessageAsync(corrId, requestInfo, requestMessage);

            var response = await base.SendAsync(request, cancellationToken);

            var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());
            var statusCode = (int)response.StatusCode;

            var respondInfo = string.Format("[{0}] HTTP {1} {2} responded {3} in {4} ms",
                ip, request.Method, request.RequestUri, statusCode, elapsedMs);

            string responseMessage = null;
            if (response.IsSuccessStatusCode && CanHandleResponse(response))
            {
                responseMessage = JsonConvert.SerializeObject(((ObjectContent)response.Content).Value);
            }
            if (!response.IsSuccessStatusCode)
            {
                string unSuccess = statusCode + response.ReasonPhrase;
                respondInfo = unSuccess + ":" + respondInfo;
            }

            await OutgoingMessageAsync(corrId, respondInfo, responseMessage);

            await InOutMessageAsync(corrId, respondInfo, requestMessage, responseMessage);
            return response;
        }

        protected abstract Task IncommingMessageAsync(string correlationId, string requestInfo, string message);
        protected abstract Task OutgoingMessageAsync(string correlationId, string respondInfo, string message);
        protected abstract Task InOutMessageAsync(string correlationId, string respondInfo, string inMsg, string outMsg);

        public bool CanHandleResponse(HttpResponseMessage response)
        {
            response.Content.Headers.TryGetValues("Content-Type", out var contentTypes);
            if (contentTypes == null)
            {
                return false;
            }
            if (contentTypes.Any(c => c.Contains("text/encrypt")))
            {
                return false;
            }
            var objectContent = response.Content as ObjectContent;
            var canHandleResponse = objectContent != null;
            //&& objectContent.ObjectType == typeof(PagedDataInquiryResponse<Task>);
            return canHandleResponse;
        }

        public bool CanHandleRequestContent(HttpRequestMessage request)
        {
            request.Content.Headers.TryGetValues("Content-Type", out var contentTypes);
            if (contentTypes == null)
            {
                return false;
            }
            return !contentTypes.Any(c => c.Contains("text/encrypt"));
        }


        static long GetElapsedMilliseconds(long start, long stop)
        {
            return (stop - start) * 1000 / Stopwatch.Frequency;
        }
    }
}
