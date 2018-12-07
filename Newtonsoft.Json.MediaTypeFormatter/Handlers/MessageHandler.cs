using System;
using System.Collections.Generic;
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
            var requestInfo = string.Format("{0} {1}", request.Method, request.RequestUri);
            //ignore swagger
            if (request.RequestUri.PathAndQuery.ToLowerInvariant().Contains("/swagger"))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var requestMessage = await request.Content.ReadAsStringAsync();

            await IncommingMessageAsync(corrId, requestInfo, requestMessage);

            var response = await base.SendAsync(request, cancellationToken);

            string responseMessage = "";

            if (response.IsSuccessStatusCode && CanHandleResponse(response))
            {
                responseMessage = JsonConvert.SerializeObject(((ObjectContent)response.Content).Value);
            }
            else
            {
                responseMessage = response.ReasonPhrase;
            }

            await OutgoingMessageAsync(corrId, requestInfo, responseMessage);

            return response;
        }

        protected abstract Task IncommingMessageAsync(string correlationId, string requestInfo, string message);
        protected abstract Task OutgoingMessageAsync(string correlationId, string requestInfo, string message);

        public bool CanHandleResponse(HttpResponseMessage response)
        {
            var objectContent = response.Content as ObjectContent;
            var canHandleResponse = objectContent != null;
            //&& objectContent.ObjectType == typeof(PagedDataInquiryResponse<Task>);
            return canHandleResponse;
        }
    }
}
