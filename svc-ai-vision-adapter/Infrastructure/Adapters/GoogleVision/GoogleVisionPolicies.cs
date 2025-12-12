using Google.Cloud.Vision.V1;
using Grpc.Core;
using Polly;

internal static class GoogleVisionPolicies
{
    public static IAsyncPolicy<BatchAnnotateImagesResponse> CreatePolicy()
    {
        return Policy<BatchAnnotateImagesResponse>
            .Handle<RpcException>(ex =>
                ex.Status.StatusCode == StatusCode.Unavailable ||
                ex.Status.StatusCode == StatusCode.DeadlineExceeded)
            .WaitAndRetryAsync(3, retry =>
                TimeSpan.FromMilliseconds(200 * Math.Pow(2, retry)));
    }
}
