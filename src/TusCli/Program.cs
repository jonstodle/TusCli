using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TusDotNetClient;
using static System.Console;

namespace TusCli
{
    [Command("tus", Description = "A cli tool for interacting with a Tus enabled server.")]
    class Program
    {
        private static Task Main(string[] args) => CommandLineApplication.ExecuteAsync<Program>(args);

        // ReSharper disable UnassignedGetOnlyAutoProperty
        [Argument(0, "address", "The endpoint of the Tus server")]
        [Required]
        public string Address { get; }

        [Argument(1, "file", "File to upload")]
        [Required]
        public string FilePath { get; }

        [Option("-m|--metadata",
            "Additional metadata to submit. Can be specified multiple times. Format: key=value",
            CommandOptionType.MultipleValue)]
        public string[] Metadata { get; } = Array.Empty<string>();

        [Option("-c|--chunk-size",
            "The size (in MB) of each chunk when uploading. Default: 5",
            CommandOptionType.SingleValue)]
        public double ChunkSize { get; } = 5;

        [Option("-h|--header",
            "Specify additional HTTP header to send. Can be specified multiple times. Format: Header-Name=HeaderValue",
            CommandOptionType.MultipleValue)]
        public string[] Headers { get; } = Array.Empty<string>();
        // ReSharper restore UnassignedGetOnlyAutoProperty

        public async Task<int> OnExecuteAsync()
        {
            var file = new FileInfo(FilePath);
            if (!file.Exists)
            {
                Error.WriteLine($"Could not find file '{file.FullName}'.");
                return 1;
            }

            var infoFile = new FileInfo($"{file.FullName}.info");
            var fileInformation = FileInformation.Parse(TryReadAllText(infoFile.FullName) ?? "");

            var client = new TusClient();
            foreach (var (name, value) in ParseKeyValuePairs(Headers))
                client.AdditionalHeaders.Add(name, value);

            try
            {
                if (string.IsNullOrWhiteSpace(fileInformation.ServerId))
                {
                    var metadata = ParseKeyValuePairs(Metadata);
                    var uploadUrl = await client.CreateAsync(Address, file.Length, metadata);
                    fileInformation.ServerId = uploadUrl.Split('/').Last();
                }

                var fileUrl = $"{Address}{fileInformation.ServerId}";

                File.WriteAllText(infoFile.FullName, fileInformation.ToString());

                var operation = client.UploadAsync(fileUrl, file, ChunkSize);
                operation.Progressed += OnUploadProgress;
                await operation;

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

        private static (string, string)[] ParseKeyValuePairs(string[] keyValuePairs) =>
            keyValuePairs
                .Select(kvp =>
                {
                    var parts = kvp.Split('=', 2);
                    if (parts.Length == 2)
                        return (parts[0], parts[1]);

                    var response =
                        Prompt.GetString($"Unable to parse '{kvp}'. Do you want to [s]kip it or [a]bort?")
                        ?? "a";
                    if (!"skip".StartsWith(response, StringComparison.OrdinalIgnoreCase))
                        throw new Exception("Aborted by user request.");

                    return (null, null);
                })
                .Where(kvp => kvp.Item1 != null && kvp.Item2 != null)
                .ToArray();
    }
}