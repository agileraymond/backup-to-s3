// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");

// loop thru localFolders and get files 

// check last modified date on file and sync file that meet a config date

// if I file fails to sync, write to a log file 

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using IHost host = Host.CreateDefaultBuilder(args).Build();
IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

// read config value with localFolder > aws s3 bucket
var folderMappings = config.GetValue<string>("FolderMappings");

foreach (var folderConfig in folderMappings.Split(';'))
{
    var folderArray = folderConfig.Split(':');
    Console.WriteLine($"local folder: {folderArray[0]} s3: {folderArray[1]}");
    //Directory.SetCurrentDirectory(folderArray[0]);

    var fileEntries = new DirectoryInfo(Path.Combine(".", folderArray[0])).GetFiles("*", SearchOption.AllDirectories);
    foreach (var file in fileEntries)
    {
        Console.WriteLine($"file {file}");    
    }
}