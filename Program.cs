using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

using IHost host = Host.CreateDefaultBuilder(args).Build();
IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/error.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

// read config value with localFolder > aws s3 bucket
var folderMappings = config.GetValue<string>("FolderMappings");
var rootDrive = "d:\\";
var lastModifiedMinutes = config.GetValue<double>("SyncFilesLastModifiedMinutes");

try
{
    // s3 client 
    var s3Client = new AmazonS3Client();

    foreach (var folderConfig in folderMappings.Split(';'))
    {       
        var folderArray = folderConfig.Split(':');
        var folder = Path.Join(rootDrive, folderArray[0]);
        var s3Bucket = folderArray[1];
        var fileEntries = new DirectoryInfo($"{folder}").GetFiles("*", SearchOption.AllDirectories);

        // check bucket
        try
        {
            var getS3Bucket = await s3Client.GetBucketLocationAsync(s3Bucket);
        }
        catch
        {
            try
            {
                var putS3Bucket = await s3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = s3Bucket,
                    BucketRegionName = config.GetValue<string>("AwsRegion"),
                });
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message} {ex.StackTrace}");                
                continue;
            }
        }
        
        foreach (var file in fileEntries)
        {
            var key = file.FullName.Replace(rootDrive, "").Replace("\\", "/");

            try
            {
                var getS3Object = await s3Client.GetObjectAsync(s3Bucket, key);
                TimeSpan ts = file.LastWriteTimeUtc - getS3Object.LastModified;
                
                //if (Math.Abs(ts.TotalMinutes) > lastModifiedMinutes)
                if (file.Length != getS3Object.ContentLength)
                {
                    try
                    {
                        var updateS3Object = await s3Client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
                        {
                            BucketName = s3Bucket,
                            Key = key,
                            FilePath = file.ToString(),
                            ContentType = "text/plain"
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"{ex.Message} {ex.StackTrace}");
                        continue;
                    }                    
                }
            }
            catch
            {
                try
                {
                    // upload to s3
                    var putS3Object = await s3Client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
                    {
                        BucketName = s3Bucket,
                        Key = key,
                        FilePath = file.ToString(),
                        ContentType = "text/plain"
                    });
                }
                catch (Exception ex)
                {
                    Log.Error($"{ex.Message} {ex.StackTrace}");
                    continue;
                }               
            }
        }
    }
}
catch (Exception ex)
{
    Log.Error(ex, "error");
    throw;
}