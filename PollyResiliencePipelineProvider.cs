using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimit;
using Polly.Retry;
using Polly.Timeout;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;

namespace ConsoleApp6
{
    public class CustomResiliencePipelineBuilder
    {
        private readonly ResiliencePipelineBuilder<HttpResponseMessage> _pipelineBuilder = new();

        public ResiliencePipelineBuilder<HttpResponseMessage> Configure(
            int retryCount,
            TimeSpan retryDelay,
            int exceptionsAllowedBeforeBreaking,
            TimeSpan durationOfBreak)
        {
            _pipelineBuilder.AddRetry(AddRetryPolicy(retryCount, retryDelay));
            _pipelineBuilder.AddCircuitBreaker(AddCircuitBreakerPolicy(exceptionsAllowedBeforeBreaking, durationOfBreak));

            return _pipelineBuilder;
        }

        // Add a retry policy to the pipeline
        public RetryStrategyOptions<HttpResponseMessage> AddRetryPolicy(
            int retryCount,
            TimeSpan retryDelay)
        {
            var strategyOptions = new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = retryCount,
                Delay = retryDelay,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                             .Handle<Exception>(ex =>
                             {
                                 var shouldRetry = RetryableExceptions.Contains(ex.GetType());

                                 if (shouldRetry)
                                 {
                                     Console.WriteLine($"Retry triggered by exception: {ex.GetType().Name}");
                                 }

                                 return shouldRetry;
                             }),
                //.HandleResult(response =>
                //{
                //    var shouldRetry = ServerErrorCodes.Contains(response.StatusCode);

                //    if (shouldRetry)
                //    {
                //        Console.WriteLine($"Retry triggered by HTTP status code: {response.StatusCode}");
                //    }

                //    return shouldRetry;
                //}),

                // BackoffType = DelayBackoffType.Constant,
                OnRetry = args =>
                {
                    Console.WriteLine($"Retry attempt {args.AttemptNumber} after {args.RetryDelay.TotalMilliseconds} ms due to {args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString()}");
                    return ValueTask.CompletedTask;
                },
            };

            return strategyOptions;
        }

        // public IAsyncPolicy<HttpResponseMessage> Build() => _pipelineBuilder.Build();

        private static readonly ImmutableArray<Type> RetryableExceptions =
                    [
                        typeof(SocketException),
                        typeof(HttpRequestException),
                        typeof(TimeoutRejectedException),
                        typeof(BrokenCircuitException),
                        typeof(RateLimitRejectedException),
                        typeof(TimeoutException),
                     ];

        protected static readonly HashSet<HttpStatusCode> ServerErrorCodes = new HashSet<HttpStatusCode>
                {
                    HttpStatusCode.InternalServerError,
                    HttpStatusCode.NotImplemented,
                    HttpStatusCode.BadGateway,
                    HttpStatusCode.ServiceUnavailable,
                    HttpStatusCode.GatewayTimeout,
                    HttpStatusCode.HttpVersionNotSupported,
                    HttpStatusCode.VariantAlsoNegotiates,
                    HttpStatusCode.InsufficientStorage,
                    HttpStatusCode.LoopDetected,
                    HttpStatusCode.Processing,
                };

        // Add a circuit breaker policy to the pipeline
        public CircuitBreakerStrategyOptions<HttpResponseMessage> AddCircuitBreakerPolicy(
            int exceptionsAllowedBeforeBreaking,
            TimeSpan durationOfBreak)
        {
            var strategyOptions = new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = exceptionsAllowedBeforeBreaking,
                BreakDuration = durationOfBreak,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .HandleResult(message => ServerErrorCodes.Contains(message.StatusCode))
                            .Handle<Exception>(ex =>
                             {
                                 var shouldHandle = RetryableExceptions.Contains(ex.GetType());

                                 if (shouldHandle)
                                 {
                                     Console.WriteLine($"Retry triggered by exception: {ex.GetType().Name}");
                                 }

                                 return shouldHandle;
                             }),
                OnOpened = args =>
                {
                    Console.WriteLine($"Circuit opened for {args.BreakDuration.TotalMilliseconds} ms due to {args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString()}");
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    Console.WriteLine("Circuit closed. Resuming normal operation.");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = _ =>
                {
                    Console.WriteLine("Circuit half-opened. Testing for recovery...");
                    return ValueTask.CompletedTask;
                },
            };
            return strategyOptions;
        }

        // Add a timeout policy to the pipeline
        //public ResiliencePipelineBuilder AddTimeoutPolicy(TimeSpan timeout)
        //{
        //    var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(timeout, Polly.Timeout.TimeoutStrategy.Pessimistic);
        //    _policies.Add(timeoutPolicy);
        //    return this;
        //}

        // Build the resilience pipeline
        //public AsyncPolicyWrap<HttpResponseMessage> Build()
        //{
        //    return Policy.WrapAsync(_policies.ToArray());
        //}
    }

}
