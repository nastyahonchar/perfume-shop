using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Platform.ServiceDefaults;

public sealed class GrpcServerLoggingInterceptor(ILogger<GrpcServerLoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await continuation(request, context);

            logger.LogInformation(
                "gRPC {GrpcMethod} from {Peer} finished with status {GrpcStatus} in {Elapsed} ms",
                context.Method, context.Peer, StatusCode.OK, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (RpcException ex)
        {
            var level = ex.StatusCode is StatusCode.NotFound or StatusCode.InvalidArgument
                or StatusCode.AlreadyExists or StatusCode.FailedPrecondition
                ? LogLevel.Warning
                : LogLevel.Error;

            logger.Log(level,
                "gRPC {GrpcMethod} from {Peer} finished with status {GrpcStatus} in {Elapsed} ms: {Detail}",
                context.Method, context.Peer, ex.StatusCode, stopwatch.ElapsedMilliseconds, ex.Status.Detail);

            throw;
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "gRPC {GrpcMethod} from {Peer} cancelled by the client after {Elapsed} ms",
                context.Method, context.Peer, stopwatch.ElapsedMilliseconds);

            throw new RpcException(new Status(StatusCode.Cancelled, "The call was cancelled"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "gRPC {GrpcMethod} from {Peer} failed with status {GrpcStatus} in {Elapsed} ms",
                context.Method, context.Peer, StatusCode.Internal, stopwatch.ElapsedMilliseconds);

            throw new RpcException(new Status(StatusCode.Internal, "An unexpected error occurred"));
        }
    }
}