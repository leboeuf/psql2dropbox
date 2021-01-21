using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace psql2dropbox
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var commandLineArguments = HandleCommandLineArguments(args);
            var tempFolder = Path.GetTempPath();

            // Create dump
            var dumpFile = CreateDump(commandLineArguments, tempFolder);

            // Upload
            await Upload(commandLineArguments, tempFolder, dumpFile);

            // Remove temporary files
            File.Delete(dumpFile);
        }

        private static string CreateDump(CommandLineArguments commandLineArguments, string tempFolder)
        {
            var dumpFile = $"psql{DateTime.UtcNow:yyyMMddhhmmss}.dump";
            
            var connectionString = ParseConnectionString(commandLineArguments.ConnectionString);
            
            var command = $"PGPASSWORD=\"{connectionString.Password}\" pg_dump -Fc -h {connectionString.Server} -p {connectionString.Port} -d {connectionString.Database} -U {connectionString.UserId} > {tempFolder}{dumpFile}";

            var commandResult = command.ExecuteInShell();

            return dumpFile;
        }

        private static Task Upload(CommandLineArguments commandLineArguments, string tempFolder, string dumpFile)
        {
            var dumpSize = new FileInfo(Path.Combine(tempFolder, dumpFile)).Length;
            if (dumpSize == 0)
            {
                throw new InvalidOperationException("Dump file is empty.");
            }
            
            // If size >= 150 MB, use session upload
            // Note: conservatively using decimal megabytes instead 
            // of binary mebibytes just to make sure we're not over threshold
            if (dumpSize >= 150000000)
            {
                return UploadLargeFile(commandLineArguments, tempFolder, dumpFile);
            }
            else
            {
                return UploadSmallFile(commandLineArguments, tempFolder, dumpFile);
            }
        }

        private static async Task UploadSmallFile(CommandLineArguments commandLineArguments, string tempFolder, string dumpFile)
        {
            var bytesToUpload = File.ReadAllBytes(Path.Combine(tempFolder, dumpFile));

            using var httpClient = new HttpClient();
            using var content = new ByteArrayContent(bytesToUpload);

            var request = new HttpRequestMessage
            {
                Content = content,
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://content.dropboxapi.com/2/files/upload"),
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", commandLineArguments.ApiToken);
            request.Headers.Add("Dropbox-API-Arg", "{ \"path\": \"/" + dumpFile + "\", \"mode\": \"overwrite\", \"mute\": false }");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            using var response = await httpClient.SendAsync(request);

            var isSuccess = response.IsSuccessStatusCode;
            if (!isSuccess)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Upload failed with the following error: {responseContent}");
            }
        }

        private static async Task UploadLargeFile(CommandLineArguments commandLineArguments, string tempFolder, string dumpFile)
        {
            throw new NotImplementedException("Large file upload (>150MB) not supported.");
        }

        private static CommandLineArguments HandleCommandLineArguments(string[] args)
        {
            const string ApiTokenFlag = "--apitoken=";
            const string ConnectionStringFlag = "--connectionstring=";
            const string DropboxFolderNameFlag = "--dropboxfoldername=";

            var apiToken = args.FirstOrDefault(a => a.StartsWith(ApiTokenFlag, StringComparison.InvariantCultureIgnoreCase))?.Substring(ApiTokenFlag.Length);
            var connectionString = args.FirstOrDefault(a => a.StartsWith(ConnectionStringFlag, StringComparison.InvariantCultureIgnoreCase))?.Substring(ConnectionStringFlag.Length);
            var dropboxFolderName = args.FirstOrDefault(a => a.StartsWith(DropboxFolderNameFlag, StringComparison.InvariantCultureIgnoreCase))?.Substring(DropboxFolderNameFlag.Length);

            if (string.IsNullOrEmpty(apiToken))
            {
                throw new ArgumentNullException($"Missing flag: {ApiTokenFlag}");
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException($"Missing flag: {ConnectionStringFlag}");
            }

            if (string.IsNullOrEmpty(dropboxFolderName))
            {
                throw new ArgumentNullException($"Missing flag: {DropboxFolderNameFlag}");
            }

            return new CommandLineArguments
            {
                ApiToken = apiToken,
                ConnectionString = connectionString,
                DropboxFolderName = dropboxFolderName
            };
        }
        
        private static ConnectionString ParseConnectionString(string connectionString)
        {
            var decodedConnectionString = Base64Decode(connectionString);
            var split = decodedConnectionString.Split(';')
                .Select(s => s.Trim())
                .ToList();

            const string ServerParameter = "Server=";
            var server = split.FirstOrDefault(s => s.StartsWith(ServerParameter, StringComparison.InvariantCultureIgnoreCase))?.Substring(ServerParameter.Length);

            const string PortParameter = "Port=";
            var port = split.FirstOrDefault(s => s.StartsWith(PortParameter, StringComparison.InvariantCultureIgnoreCase))?.Substring(PortParameter.Length);

            const string DatabaseParameter = "Database=";
            var database = split.FirstOrDefault(s => s.StartsWith(DatabaseParameter, StringComparison.InvariantCultureIgnoreCase))?.Substring(DatabaseParameter.Length);

            const string UserIdParameter = "User Id=";
            var userId = split.FirstOrDefault(s => s.StartsWith(UserIdParameter, StringComparison.InvariantCultureIgnoreCase))?.Substring(UserIdParameter.Length);

            const string PasswordParameter = "Password=";
            var password = split.FirstOrDefault(s => s.StartsWith(PasswordParameter, StringComparison.InvariantCultureIgnoreCase))?.Substring(PasswordParameter.Length);

            return new ConnectionString
            {
                Server = server,
                Port = port,
                Database = database,
                UserId = userId,
                Password = password
            };
        }

        public static string Base64Decode(string base64)
        {
            var base64bytes = System.Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(base64bytes);
        }
    }
}
