using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Text.Json;

[Generator]
public class JsonClassGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var jsonFiles = context.AdditionalTextsProvider
            .Where(at => at.Path.EndsWith(".json"))
            .Select((text, cancellationToken) => new { text.Path, text.GetText(cancellationToken).ToString() });

        var compilationAndJsonFiles = context.CompilationProvider.Combine(jsonFiles.Collect());
        
        context.RegisterSourceOutput(compilationAndJsonFiles, (sourceProductionContext, source) =>
        {
            var (compilation, jsonFiles) = source;

            foreach (var jsonFile in jsonFiles)
            {
                var classCode = GenerateClassFromJson(jsonFile.Path, jsonFile.ToString());
                if (classCode != null)
                {
                    sourceProductionContext.AddSource($"{Path.GetFileNameWithoutExtension(jsonFile.Path)}_Generated.cs", SourceText.From(classCode, Encoding.UTF8));
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
            classBuilder.AppendLine();
            classBuilder.AppendLine($"public class {className}");
            classBuilder.AppendLine("{");

            foreach (var property in jsonDocument.RootElement.EnumerateObject())
            {
                classBuilder.AppendLine($"    public {GetCSharpType(property.Value.ValueKind)} {property.Name} {{ get; set; }}");
            }

            classBuilder.AppendLine("}");

            return classBuilder.ToString();
        }
        catch (Exception ex)
        {
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
