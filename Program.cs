// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");

// loop thru localFolders and get files 

// check last modified date on file and sync file that meet a config date

// if I file fails to sync, write to a log file 

using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using IHost host = Host.CreateDefaultBuilder(args).Build();
IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

// read config value with localFolder > aws s3 bucket
var folderMappings = config.GetValue<string>("FolderMappings");

// s3 client 
var s3Client = new AmazonS3Client();

foreach (var folderConfig in folderMappings.Split(';'))
{   
    var folderArray = folderConfig.Split(':');     
    var folder = Path.Join("c:\\", folderArray[0]);
    var s3Bucket = folderArray[1];
    Console.WriteLine(folder);
    var fileEntries = new DirectoryInfo($"{folder}").GetFiles("*", SearchOption.AllDirectories);
    
    foreach (var file in fileEntries)
    {
        Console.WriteLine($"file {file}");    
        DateTime dt = File.GetLastAccessTime(file.ToString());
        Console.WriteLine("The last access time for this file was {0}.", dt);   

        // upload to s3
        try
        {
            await s3Client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
            { 
                BucketName = s3Bucket,
                Key = file.Name,
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