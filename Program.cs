using ConsoleApp6;

var builder = WebApplication.CreateBuilder(args);

// Create a resilience pipeline
var resiliencePipelineBuilder = new CustomResiliencePipelineBuilder();
var resiliencePipeline = resiliencePipelineBuilder
    .Configure(retryCount: 10, retryDelay: TimeSpan.FromMilliseconds(3000), exceptionsAllowedBeforeBreaking: 3, durationOfBreak: TimeSpan.FromMilliseconds(60000)) // Add retry policy
    .Build(); // Add other policies as needed

// Add handler with resilience pipeline
var handler = new RetryHandler(resiliencePipeline)
{
    InnerHandler = new SocketsHttpHandler() // Base handler
};

// Create HttpMessageInvoker
var invoker = new HttpMessageInvoker(handler);

// Create a request and execute it
var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5125/v1/health");

try
{
    var response = await invoker.SendAsync(httpRequestMessage, CancellationToken.None);
    Console.WriteLine($"Response: {response.StatusCode}");
}
catch (Exception ex)
{
    Console.WriteLine($"Request failed: {ex.Message}");
}

var app = builder.Build();

app.Run();
