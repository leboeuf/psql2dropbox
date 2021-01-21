namespace psql2dropbox
{
    public class CommandLineArguments
    {
        public string ApiToken { get; set; }
        public string ConnectionString { get; set; }
        public string DropboxFolderName { get; set; }
    }
}