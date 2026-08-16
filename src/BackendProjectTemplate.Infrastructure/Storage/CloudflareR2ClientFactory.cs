using Amazon.Runtime;
using Amazon.S3;

namespace BackendProjectTemplate.Infrastructure.Storage;

internal sealed class CloudflareR2ClientFactory : ICloudflareR2ClientFactory
{
    public IAmazonS3 Create(CloudflareR2Options options)
    {
        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
        return new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = options.Endpoint.TrimEnd('/'),
            ForcePathStyle = true,
            AuthenticationRegion = "auto"
        });
    }
}
