using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using static System.Console;

namespace TusCli
{
    class Program
    {
        static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);
        
        [Argument(0, "file", "File to upload")]
        [Required]
        public string FilePath { get; }
        
        [Argument(1, "Address to upload the file to")]
        [Required]
        public string Address { get; }
        
        [Option(Description = "Additional metadata to submit. Format: key1=value1,key2=value2")]
        public string Metadata { get; }

        public int OnExecute()
        {
            var file = new FileInfo(FilePath);
            if (!file.Exists)
            {
                Error.WriteLine($"Could not find file '{file.FullName}'.");
                return 1;
            }

            var metadata = Metadata?
                .Split(',')
                .ToDictionary(
                    s => s.Split('=')[0],
                    s => s.Split('=')[1]);
            
            var client = new TusClient();
            client.Uploading += OnUploadProgress;
            
            WriteLine("Uploading...");

            var fileUrl = client.Create(Address, file, metadata);
            client.Upload(fileUrl, file);
            
            return 0;
        }

        private void OnUploadProgress(long bytesTransferred, long bytesTotal)
        {
            var progress = (bytesTransferred / (double)bytesTotal) * 100;
            WriteLine($"{progress:0.00}%");
        }
    }
}