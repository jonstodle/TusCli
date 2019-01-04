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
        private static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        // ReSharper disable UnassignedGetOnlyAutoProperty
        [Argument(0, "file", "File to upload")]
        [Required]
        public string FilePath { get; }

        [Argument(1, "address", "The endpoint of the Tus server")]
        [Required]
        public string Address { get; }

        [Option(Description = "Additional metadata to submit. Format: key1=value1,key2=value2")]
        public string Metadata { get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty

        public int OnExecute()
        {
            var file = new FileInfo(FilePath);
            if (!file.Exists)
            {
                Error.WriteLine($"Could not find file '{file.FullName}'.");
                return 1;
            }

            var infoFile = new FileInfo($"{file.FullName}.info");
            var fileInformation = FileInformation.Parse(TryReadAllText(infoFile.FullName) ?? "");

            var metadata = ParseMetadata(Metadata) ?? Array.Empty<(string, string)>();

            var client = new TusClient();
            client.UploadProgress += OnUploadProgress;

            try
            {
                var fileUrl = $"{Address}{fileInformation.ServerId}";
                if (string.IsNullOrWhiteSpace(fileInformation.ServerId))
                {
                    fileUrl = client.Create(Address, file, metadata);
                    fileInformation.ServerId = fileUrl.Split('/').Last();
                }

                File.WriteAllText(infoFile.FullName, fileInformation.ToString());
                
                client.Upload(fileUrl, file);
                
                try
                {
                    infoFile.Delete();
                }
                catch
                {
                    // ignored
                }
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
            var progress = bytesTransferred / (double) bytesTotal;
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

        private static string TryReadAllText(string path)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch
            {
                return null;
            }
        }

        private static (string, string)[] ParseMetadata(string metadata) =>
            metadata?
                .Split(',')
                .Select(md =>
                {
                    var parts = md.Split('=');
                    return (parts[0], parts[1]);
                })
                .ToArray();
    }
}