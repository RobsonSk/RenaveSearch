using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class LoggingHandler : DelegatingHandler
{
    public LoggingHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Log the request headers
        Console.WriteLine("Request Headers:");
        foreach (var header in request.Headers)
        {
            Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}