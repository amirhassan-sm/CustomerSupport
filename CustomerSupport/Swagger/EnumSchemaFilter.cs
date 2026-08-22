using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CustomerSupport.Swagger
{
    public sealed class EnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var enumType = Nullable.GetUnderlyingType(context.Type) ?? context.Type;
            if (!enumType.IsEnum)
                return;

            var names = Enum.GetNames(enumType);
            schema.Enum.Clear();
            schema.Type = "string";
            schema.Format = null;

            var parts = new List<string>(names.Length);
            foreach (var name in names)
            {
                var value = Convert.ToInt64(Enum.Parse(enumType, name));
                parts.Add($"{name} = {value}");
                schema.Enum.Add(new OpenApiString(name));
            }

            schema.Example = new OpenApiString(names[0]);
            schema.Default = new OpenApiString(names[0]);

            var enumDescription = "Allowed values: " + string.Join(", ", parts);
            schema.Description = string.IsNullOrWhiteSpace(schema.Description)
                ? enumDescription
                : $"{schema.Description}. {enumDescription}";
        }
    }
}
