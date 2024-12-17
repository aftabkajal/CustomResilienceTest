using Polly;

namespace ConsoleApp6
{
    public class RetryHandler : DelegatingHandler
    {
        private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;

        public RetryHandler(ResiliencePipeline<HttpResponseMessage> resiliencePipeline)
        {
            _resiliencePipeline = resiliencePipeline;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Execute the request through the resilience pipeline
            return await _resiliencePipeline.ExecuteAsync(
                async token => await base.SendAsync(request, token),
                cancellationToken);
        }
    }
}
