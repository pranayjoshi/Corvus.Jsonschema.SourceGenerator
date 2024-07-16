using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace JsonSchema.GSoC2024.PartialAttribute
{
    [Generator]
    public class PartialAttributeGenerator : IIncrementalGenerator
    {
        private static readonly DiagnosticDescriptor AttributeApplied = new(
            id: "JSON001",
            title: "Generated attribute applied",
            messageFormat: "Generated attribute applied to class '{0}' with JSON path '{1}'",
            category: "Usage",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor JsonFileNotFound = new(
            id: "JSON002",
            title: "JSON file not found",
            messageFormat: "JSON file '{0}' not found for class '{1}'",
            category: "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor GenerationComplete = new(
            id: "JSON003",
            title: "Partial class generated",
            messageFormat: "Partial class generated for '{0}' with JSON path '{1}'",
            category: "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "GeneratedAttribute.cs", SourceText.From(GenerateAttributeCode(), Encoding.UTF8)));

            IncrementalValuesProvider<(ClassDeclarationSyntax ClassDeclaration, string JsonPath, string Namespace)> classDeclarations = 
                context.SyntaxProvider
                    .CreateSyntaxProvider(
                        predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                        transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                    .Where(static m => m.ClassDeclaration is not null);

            IncrementalValueProvider<(Compilation Compilation, ImmutableArray<AdditionalText> AdditionalFiles)> compilationAndFiles
                = context.CompilationProvider.Combine(context.AdditionalTextsProvider.Collect());

            context.RegisterSourceOutput(classDeclarations.Combine(compilationAndFiles),
                static (spc, source) => Execute(source.Left, source.Right.AdditionalFiles, spc));
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
            => node is ClassDeclarationSyntax { AttributeLists: { Count: > 0 } };

        private static (ClassDeclarationSyntax ClassDeclaration, string JsonPath, string Namespace) GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
            foreach (var attributeList in classDeclarationSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol)
                    {
                        INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                        string fullName = attributeContainingTypeSymbol.ToDisplayString();

                        if (fullName == "GeneratedAttribute")
                        {
                            var jsonPath = attribute.ArgumentList?.Arguments[0].Expression.ToString().Trim('"');
                            var namespaceName = GetNamespace(classDeclarationSyntax);
                            return (classDeclarationSyntax, jsonPath ?? "", namespaceName);
                        }
                    }
                }
            }
            return (null, "", "");
        }

        private static string GetNamespace(ClassDeclarationSyntax classDeclaration)
        {
            var namespaceDeclaration = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            return namespaceDeclaration?.Name.ToString() ?? "";
        }

        private static void Execute(
    (ClassDeclarationSyntax ClassDeclaration, string JsonPath, string Namespace) item,
    ImmutableArray<AdditionalText> additionalFiles,
    SourceProductionContext context)
{
    var (classDeclaration, jsonPath, namespaceName) = item;
    var className = classDeclaration.Identifier.ToString();

    context.ReportDiagnostic(Diagnostic.Create(AttributeApplied, classDeclaration.GetLocation(), className, jsonPath));

    var jsonFile = additionalFiles.FirstOrDefault(f => f.Path.EndsWith(jsonPath));
    if (jsonFile == null)
    {
        context.ReportDiagnostic(Diagnostic.Create(JsonFileNotFound, classDeclaration.GetLocation(), jsonPath, className));
        return;
    }

    var jsonContent = jsonFile.GetText(context.CancellationToken)?.ToString() ?? "{}";
    context.ReportDiagnostic(Diagnostic.Create(
        new DiagnosticDescriptor("JSON004", "Raw JSON Content", "Raw JSON Content: {0}", "Debug", DiagnosticSeverity.Info, true),
        classDeclaration.GetLocation(), jsonContent));

    string classContent = GeneratePartialClassContent(classDeclaration, jsonPath, jsonContent, namespaceName);
    context.AddSource($"{className}.Generated.cs", SourceText.From(classContent, Encoding.UTF8));

    context.ReportDiagnostic(Diagnostic.Create(GenerationComplete, classDeclaration.GetLocation(), className, jsonPath));
}

        private static string GenerateAttributeCode()
        {
            return """
            using System;

            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            public sealed class GeneratedAttribute : Attribute
            {
                public string JsonPath { get; }

                public GeneratedAttribute(string jsonPath)
                {
                    JsonPath = jsonPath;
                }
            }
            """;
        }

        private static string GeneratePartialClassContent(ClassDeclarationSyntax classDeclaration, string jsonPath, string jsonContent, string namespaceName)
{
    string className = classDeclaration.Identifier.ToString();
    string namespaceDeclaration = string.IsNullOrEmpty(namespaceName) ? "" : $"namespace {namespaceName}\n{{";
    string namespaceClosing = string.IsNullOrEmpty(namespaceName) ? "" : "}";

    List<string> properties = new List<string>();
    try
    {
        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;
        properties = root.EnumerateObject().Select(p => p.Name).ToList();
    }
    catch (JsonException ex)
    {
        properties.Add($"// Error parsing JSON: {ex.Message}");
    }

    var propertyDeclarations = string.Join("\n", properties.Select(p => $"    public JsonElement {p} {{ get; set; }}"));

    string escapedJsonContent = jsonContent.Replace("\"", "\"\"");

    return $$"""
    using System;
    using System.Text.Json;

    {{namespaceDeclaration}}
    public partial class {{className}}
    {
        private const string JsonPath = @"{{jsonPath}}";
        private const string JsonContent = @"{{escapedJsonContent}}";

    {{propertyDeclarations}}

        public void PrintJsonContent()
        {
            Console.WriteLine($"JSON content for {nameof({{className}})}:");
            Console.WriteLine(JsonContent);
        }

        public static {{className}} FromJson()
        {
            return JsonSerializer.Deserialize<{{className}}>(JsonContent);
        }
    }
    {{namespaceClosing}}
    """;
}
    }
}