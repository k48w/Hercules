using Hercules;

namespace Hercules
{
    internal class HttpClientLoggingHandler : MessageProcessingHandler
    {
        public HttpClientLoggingHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override HttpRequestMessage ProcessRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            App.Logger.WriteLine("HttpClientLoggingHandler::ProcessRequest", $"{request.Method} {SanitizeUri(request.RequestUri)}");
            return request;
        }

        protected override HttpResponseMessage ProcessResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            App.Logger.WriteLine("HttpClientLoggingHandler::ProcessResponse", $"{(int)response.StatusCode} {response.ReasonPhrase} {SanitizeUri(response.RequestMessage?.RequestUri)}");
            return response;
        }

        private static string SanitizeUri(Uri? uri) => uri is null
            ? "<unknown>"
            : $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}";
    }
}
