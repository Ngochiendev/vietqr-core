using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using VietQR.Api.Models;

namespace VietQR.Api.Extensions;

/// <summary>
/// Mô tả request body cho endpoint DecodeQr, vốn nhận cả multipart/form-data (trường `file`)
/// lẫn application/json (trường `imageBase64`) mà không dùng model binding trực tiếp.
/// </summary>
public sealed class DecodeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var descriptor = context.ApiDescription.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
        if (descriptor?.ActionName != nameof(Controllers.QrCodesController.DecodeQr))
        {
            return;
        }

        operation.RequestBody ??= new OpenApiRequestBody();
        var requestBody = operation.RequestBody;

        requestBody.Content.Clear();

        // multipart/form-data: trường "file", kiểu binary
        requestBody.Content["multipart/form-data"] = new OpenApiMediaType
        {
            Schema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["file"] = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary",
                        Description = "File ảnh chứa mã QR (PNG, JPG, WebP, BMP)",
                    },
                },
                Required = new HashSet<string> { "file" },
            },
        };

        // application/json: schema đầy đủ của VietQrDecodeRequest
        requestBody.Content["application/json"] = new OpenApiMediaType
        {
            Schema = context.SchemaGenerator.GenerateSchema(
                typeof(VietQrDecodeRequest),
                context.SchemaRepository),
        };

        requestBody.Required = true;
    }
}
