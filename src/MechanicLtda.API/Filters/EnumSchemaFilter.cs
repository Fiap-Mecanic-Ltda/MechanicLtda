using System.ComponentModel.DataAnnotations;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MechanicLtda.API.Filters
{
    public class EnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                var enumType = context.Type;
                var enumValues = new List<IOpenApiAny>();
                var enumDescriptions = new Dictionary<string, string>();

                foreach (var enumValue in enumType.GetEnumValues())
                {
                    var enumName = enumValue.ToString();
                    var fieldInfo = enumType.GetField(enumName);
                    var displayAttribute = fieldInfo?.GetCustomAttributes(typeof(DisplayAttribute), false)
                        .FirstOrDefault() as DisplayAttribute;

                    var displayName = displayAttribute?.Name ?? enumName;
                    enumValues.Add(new OpenApiString(displayName));
                    enumDescriptions[displayName] = displayAttribute?.Description ?? enumName;
                }

                schema.Enum = enumValues;
                schema.Description = string.Join("; ", enumDescriptions.Select(x => $"{x.Key}: {x.Value}"));
            }
        }
    }
}
