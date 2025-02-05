using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NotificationService.ErrorHandling
{
    public class ErrorResponseOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var errorResponse = new OpenApiResponse
            {
                Description = "Error Response",
                Content = {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = context.SchemaGenerator.GenerateSchema(typeof(ErrorResponse), context.SchemaRepository)
                    }
                }
            };

            // Only add responses that are required by the controller methods
            if (operation.Responses.ContainsKey("400"))
            {
                operation.Responses["400"] = errorResponse;
            }
            if (operation.Responses.ContainsKey("404"))
            {
                operation.Responses["404"] = errorResponse;
            }
            if (operation.Responses.ContainsKey("500"))
            {
                operation.Responses["500"] = errorResponse;
            }
        }
    }
}
