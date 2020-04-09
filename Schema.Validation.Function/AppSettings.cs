using System;
using System.Reflection;

namespace Schema.Validation.Functions
{
    public interface IAppSettings
    {
        public string BlobStorageAccount { get; }

        public string BlobStorageConnectionString { get; }

        public string BlobStorageContainerName { get; }

        public string BlobName { get; }
        
        public Uri BlobUrl { get; }

        public string Version { get; }
    }

    public class AppSettings : IAppSettings
    {
        public string BlobStorageAccount => GetSetting("AzureStorageBlobOptions:AccountName");

        public string BlobStorageConnectionString => GetSetting("AzureStorageBlobOptions:ConnectionString");

        public string BlobStorageContainerName => GetSetting("AzureStorageBlobOptions:FilePath");

        public string BlobName => GetSetting("AzureStorageBlobOptions:BlobName");

        public Uri BlobUrl => new Uri(GetSetting("AzureStorageBlobOptions:BlobUrl"));

        public string Version => GetVersion();

        protected static string GetSetting(string key)
        {
            var setting = System.Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);

            if (!string.IsNullOrWhiteSpace(setting))
            {
                return setting;
            }

            throw new Exception($"App setting '{key}' is not set");
        }

        private static string GetVersion()
        {
            return typeof(AppSettings)
                .GetTypeInfo()
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }
    }
}
