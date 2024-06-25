using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Corvus.JsonSchema.Generator
{
    public static class JsonClassGenerator
    {
        public static string GenerateClassFromJson(string filePath, string jsonContent)
        {
            try
            {
                var jsonDocument = JsonDocument.Parse(jsonContent);
                var className = Path.GetFileNameWithoutExtension(filePath);
                var classBuilder = new StringBuilder();

                classBuilder.AppendLine("using System;");
                classBuilder.AppendLine("using System.Collections.Generic;");
                classBuilder.AppendLine();
                classBuilder.AppendLine($"public class {className}");
                classBuilder.AppendLine("{");

                foreach (var property in jsonDocument.RootElement.EnumerateObject())
                {
                    classBuilder.AppendLine(
                        $"    public {GetCSharpType(property.Value.ValueKind)} {property.Name} {{ get; set; }}");
                }

                classBuilder.AppendLine("}");

                return classBuilder.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetCSharpType(JsonValueKind valueKind)
        {
            return valueKind switch
            {
                JsonValueKind.String => "string",
                JsonValueKind.Number => "double",
                JsonValueKind.True => "bool",
                JsonValueKind.False => "bool",
                JsonValueKind.Object => "object",
                JsonValueKind.Array => "List<object>",
                _ => "object"
            };
        }
    }
}