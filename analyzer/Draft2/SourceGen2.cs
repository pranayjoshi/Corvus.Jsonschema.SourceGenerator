using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Corvus.JsonSchema.Generator
{
    [Generator]
    public class JsonClassGeneratorI : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var jsonFiles = context.AdditionalTextsProvider
                .Where(at => at.Path.EndsWith(".json"))
                .Select((text, cancellationToken) => new { Path = text.Path, Content = text.GetText(cancellationToken).ToString() });

            var compilationAndJsonFiles = context.CompilationProvider.Combine(jsonFiles.Collect());

            context.RegisterSourceOutput(compilationAndJsonFiles, (sourceProductionContext, source) =>
            {
                var (compilation, jsonFiles) = source;

                foreach (var jsonFile in jsonFiles)
                {
                    var classCode = GenerateClassFromJson(jsonFile.Path, jsonFile.Content);
                    if (classCode != null)
                    {
                        sourceProductionContext.AddSource(
                            $"{Path.GetFileNameWithoutExtension(jsonFile.Path)}_Generated.cs",
                            SourceText.From(classCode, Encoding.UTF8));
                    }
                }
            });
        }

        private string GenerateClassFromJson(string filePath, string jsonContent)
        {
            try
            {   
                
                var jsonDocument = JsonDocument.Parse(jsonContent);
                var className = Path.GetFileNameWithoutExtension(filePath);
                var classBuilder = new StringBuilder();

                classBuilder.AppendLine("using System;");
                classBuilder.AppendLine("using System.Collections.Generic;");
                classBuilder.AppendLine($"public class {className}");
                classBuilder.AppendLine("{");

                foreach (var property in jsonDocument.RootElement.EnumerateObject())
                {
                    classBuilder.AppendLine(
                        $"    public {GetCSharpType(property.Value.ValueKind)} {property.Name} {{ get; set; }}");
                }

                classBuilder.AppendLine("}");
                classBuilder.AppendLine("}");

                return classBuilder.ToString();
            }
            catch (Exception)
            {
                // Handle parsing exceptions (optional)
                return null;
            }
        }

        private string GetCSharpType(JsonValueKind valueKind)
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
