using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Schema.Validation.Functions.Services;

namespace Schema.Validation.Functions.Functions
{
    public class ValidateSchema
    {
        private readonly ISchemaService _schemaService;
        private readonly ILogger _logger;

        public ValidateSchema(ISchemaService schemaService, ILoggerFactory loggerFactory)
        {
            _schemaService = schemaService;

            _logger = loggerFactory.CreateLogger<ValidateSchema>();
        }

        [FunctionName("ValidateSchema")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var validSchema = false;

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                validSchema = _schemaService.IsValidSchema(requestBody);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, e.Message);
            }
            finally
            {
                _logger.LogInformation("Finished processing a request.");
            }

            return new OkObjectResult(validSchema);
        }
    }
}
