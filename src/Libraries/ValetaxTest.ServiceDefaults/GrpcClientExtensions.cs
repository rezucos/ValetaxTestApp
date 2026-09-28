using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace ValetaxTest.ServiceDefaults;

public static class GrpcClientExtensions
{
    /// <summary>
    /// Registers a gRPC client of service <paramref name="name"/> (address from <c>Services:{name}:GrpcAddress</c>)
    /// and a Polly pipeline with the same name: timeouts, retries and a circuit breaker.
    /// </summary>
    public static IServiceCollection AddGrpcServiceClient<TClient>(
        this IServiceCollection services,
        string name,
        IConfiguration configuration,
        Func<ChannelBase, TClient> createClient)
        where TClient : class
    {
        var address = configuration[$"Services:{name}:GrpcAddress"]
            ?? throw new InvalidOperationException($"gRPC address of '{name}' is not configured (Services:{name}:GrpcAddress).");

        services.AddKeyedSingleton(name, (provider, _) =>
        {
            // With a "dns:///host:port" address the channel finds all replicas of the service and takes them in turn.
            // Without it every call would go to one replica: a gRPC connection stays open for a long time.
            // DNS is read again every 15 seconds to notice new replicas.
            var resolverServices = new ServiceCollection()
                .AddSingleton<ResolverFactory>(new DnsResolverFactory(refreshInterval: TimeSpan.FromSeconds(15)))
                .BuildServiceProvider();

            return GrpcChannel.ForAddress(address, new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Insecure,
                ServiceConfig = new ServiceConfig { LoadBalancingConfigs = { new RoundRobinConfig() } },
                ServiceProvider = resolverServices,
                HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                    PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
                },
                LoggerFactory = provider.GetRequiredService<ILoggerFactory>(),
                // A timed-out call throws OperationCanceledException, which is what Polly's timeout expects.
                ThrowOperationCanceledOnCancellation = true,
            });
        });

        services.AddSingleton(provider => createClient(provider.GetRequiredKeyedService<GrpcChannel>(name)));

        services.AddResiliencePipeline(name, (pipeline, context) =>
        {
            var logger = context.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ValetaxTest.Grpc");
            var transientFailure = new PredicateBuilder()
                .Handle<RpcException>(exception => TransientErrorDetector.IsTransient(exception.StatusCode))
                .Handle<TimeoutRejectedException>();

            pipeline
                .AddTimeout(TimeSpan.FromSeconds(10))
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = transientFailure,
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(200),
                    OnRetry = args =>
                    {
                        logger.LogWarning(args.Outcome.Exception, "Call to {Service} failed, retry {Attempt} in {Delay}",
                            name, args.AttemptNumber + 1, args.RetryDelay);
                        return ValueTask.CompletedTask;
                    },
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = transientFailure,
                    FailureRatio = 0.5,
                    MinimumThroughput = 5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(15),
                    OnOpened = args =>
                    {
                        logger.LogError(args.Outcome.Exception, "Circuit to {Service} opened for {BreakDuration}", name, args.BreakDuration);
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = _ =>
                    {
                        logger.LogInformation("Circuit to {Service} is half-open, probing", name);
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = _ =>
                    {
                        logger.LogInformation("Circuit to {Service} closed", name);
                        return ValueTask.CompletedTask;
                    },
                })
                .AddTimeout(TimeSpan.FromSeconds(3));
        });

        return services;
    }
}
