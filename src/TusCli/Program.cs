using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using TusDotNetClient;
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
                .Select(md =>
                {
                    var parts = md.Split('=');
                    return (parts[0], parts[1]);
                })
                .ToArray();

            var client = new TusClient();
            client.UploadProgress += OnUploadProgress;

            try
            {
                var fileUrl = client.Create(Address, file, metadata);
                client.Upload(fileUrl, file);
            }
            catch (Exception e)
            {
                WriteLine();
                Error.WriteLine($"Operation failed with message: '{e.Message}'");
                return 2;
            }

            return 0;
        }

        private void OnUploadProgress(long bytesTransferred, long bytesTotal)
        {
            var progress = (bytesTransferred / (double) bytesTotal);
            var percentString = $"{progress * 100:0.00}%".PadRight(8);
            var progressBarMaxWidth = BufferWidth - percentString.Length - 2;
            var progressBar = Enumerable.Range(0, (int) Math.Round(progressBarMaxWidth * progress))
                .Select(_ => '=')
                .ToArray();
            if (progress < 1 && progressBar.Length > 0)
                progressBar[progressBar.Length - 1] = '>';
            SetCursorPosition(0, CursorTop);
            Write($"{percentString}[{string.Join("", progressBar).PadRight(progressBarMaxWidth)}]");
        }
    }
}