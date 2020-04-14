using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Schema.Validation.Functions.Services
{
    public interface ISchemaService
    {
        IList<string> IsValidSchema(string request);
    }

    public class SchemaService : ISchemaService
    {
        private readonly CloudBlobContainer _cloudBlobContainer;

        private readonly IAppSettings _appSettings;
        private readonly ILogger _logger;
       
        private readonly string _orderServiceSchemas;

        public SchemaService(IAppSettings appSettings, ILoggerFactory loggerFactory)
        {
            _appSettings = appSettings;

            _logger = loggerFactory.CreateLogger<SchemaService>();

            var cloudBlobClient = CloudStorageAccount.Parse(_appSettings.BlobStorageConnectionString).CreateCloudBlobClient();

            _cloudBlobContainer = cloudBlobClient.GetContainerReference(_appSettings.BlobStorageContainerName);

            // Change $ref to refer to root 'properties'
            _orderServiceSchemas = ExtractSchemasFromOpenApiSpecificationAsync().Result.Replace("components/schemas", "properties");
        }

        public IList<string> IsValidSchema(string request)
        {
            _logger.LogInformation("Start validating request.");

            try
            {
                var resolver = new JSchemaUrlResolver();

                var jSchema = JSchema.Parse(_orderServiceSchemas, resolver);

                var requestJToken = JToken.Parse(request);

                // JSON Schema properties are case sensitive
                var isValid = requestJToken.IsValid(jSchema, out IList<string> errorMessages);

                if (isValid)
                {
                    return new List<string> { "Ok" };
                }

                _logger.LogError("Request failed validation", errorMessages);

                return errorMessages;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, e.Message);
            }

            return new List<string>();
        }

        private async Task<string> ExtractSchemasFromOpenApiSpecificationAsync()
        {
            var openApiJson = await GetOpenApiSpecificationFromBlobStorageAsync();

            // Needs to be called properties not schema
            var properties = new JProperty("properties") { Value = openApiJson.SelectToken("components.schemas") };

            return $"{{\r\n { properties } \r\n}}";
        }

        private async Task<JObject> GetOpenApiSpecificationFromBlobStorageAsync()
        {
            var openApiDocument = new OpenApiDocument();

            _logger.LogInformation("Retrieving OpenAPI Specification from blob storage.");

            try
            {
                var cloudBlockBlob = _cloudBlobContainer.GetBlockBlobReference(_appSettings.BlobName);

                var ms = new MemoryStream();

                await cloudBlockBlob.DownloadToStreamAsync(ms);

                ms.Seek(0, SeekOrigin.Begin);

                // Only using OpenApiDocument to allow validation and conversion of yaml into json.
                openApiDocument = new OpenApiStreamReader().Read(ms, out var diagnostic);

                if (diagnostic.Errors.Any())
                {
                    _logger.LogWarning("Diagnostic error reading open api document.", args: diagnostic.Errors);
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, e.Message);
            }
            finally
            {
                _logger.LogInformation("Finished retrieving OpenAPI Specification.");
            }

            return JObject.Parse(openApiDocument.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json));
        }
    }
}
