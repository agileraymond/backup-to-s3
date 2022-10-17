using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using IHost host = Host.CreateDefaultBuilder(args).Build();
IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

// read config value with localFolder > aws s3 bucket
var folderMappings = config.GetValue<string>("FolderMappings");
var rootDrive = "c:\\";
var lastModifiedMinutes = config.GetValue<double>("SyncFilesLastModifiedMinutes");

// s3 client 
var s3Client = new AmazonS3Client();
var checkDate = DateTime.Now.AddMinutes(-lastModifiedMinutes);

foreach (var folderConfig in folderMappings.Split(';'))
{   
    var folderArray = folderConfig.Split(':');     
    var folder = Path.Join(rootDrive, folderArray[0]);
    var s3Bucket = folderArray[1];
    Console.WriteLine(folder);
    var fileEntries = new DirectoryInfo($"{folder}").GetFiles("*", SearchOption.AllDirectories);
    
    foreach (var file in fileEntries)
    {
        if (file.LastWriteTime >= checkDate)
        {
            var key = file.FullName.Replace(rootDrive, "").Replace("\\", "/");

            // upload to s3
            try
            {
                await s3Client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
                {
                    BucketName = s3Bucket,
                    Key = key,
                    FilePath = file.ToString(),
                    ContentType = "text/plain"
                });
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}