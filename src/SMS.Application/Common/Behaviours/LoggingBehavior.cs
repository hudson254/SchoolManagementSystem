using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SMS.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var requestId = Guid.NewGuid().ToString();

            _logger.LogInformation(
                "Processing request {RequestName} [RequestId: {RequestId}]",
                requestName,
                requestId);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await next();
                stopwatch.Stop();

                _logger.LogInformation(
                    "Request {RequestName} completed in {ElapsedMilliseconds}ms [RequestId: {RequestId}]",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    requestId);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Request {RequestName} failed after {ElapsedMilliseconds}ms [RequestId: {RequestId}]",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    requestId);

                throw;
            }
        }
    }
}