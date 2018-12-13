using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
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

        public int OnExecute()
        {
            var file = new FileInfo(FilePath);
            if (!file.Exists)
            {
                Error.WriteLine($"Could not find file '{file.FullName}'.");
                return 1;
            }
            
            var client = new TusClient();
            client.Uploading += OnUploadProgress;
            
            WriteLine("Starting upload.");

            var fileUrl = client.Create(Address, file);
            client.Upload(fileUrl, file);
            
            WriteLine("Upload done.");            
            return 0;
        }

        private void OnUploadProgress(long bytesTransferred, long bytesTotal)
        {
            var progress = (bytesTransferred / (double)bytesTotal) * 100;
            WriteLine($"{progress:0.00}%");
        }
    }
}