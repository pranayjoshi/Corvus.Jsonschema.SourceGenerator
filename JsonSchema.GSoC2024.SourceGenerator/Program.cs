using Corvus.Json.CodeGeneration;
using Corvus.Json;
using Corvus.Json.SchemaGenerator;
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
    PartialAttributeGenerator generator = new PartialAttributeGenerator();
    if (jsonFile == null)
    {
        context.ReportDiagnostic(Diagnostic.Create(JsonFileNotFound, classDeclaration.GetLocation(), jsonPath, className));
        return;
    }

    if (additionalFiles.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (AdditionalText schemaFile in additionalFiles)
            {
                string schemaContent = schemaFile.GetText(context.CancellationToken)?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(schemaContent))
                {
                    continue;
                }

                try
                {
                    generator.GenerateTypes(schemaFile.Path, namespaceName, null, false, null, null, null, SchemaVariant.NotSpecified, true, context);
                    context.ReportDiagnostic(Diagnostic.Create(
        new DiagnosticDescriptor("JSON004", "Raw JSON Content", "Raw JSON Content: {0}", "Debug", DiagnosticSeverity.Info, true),
        classDeclaration.GetLocation(), schemaContent));
                }
                catch (Exception ex)
                {
                           // $"Error generating code for schema {schemaFile.Path}: {ex.Message}",
                }
            }
    
    // context.AddSource($"{className}.Generated.cs", SourceText.From(classContent, Encoding.UTF8));

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

        private void GenerateTypes(string schemaFile, string rootNamespace, string? rootPath, bool rebaseToRootPath, string? outputPath, string? outputMapFile, string? rootTypeName, SchemaVariant schemaVariant, bool assertFormat, SourceProductionContext context)
        {
            var typeBuilder = new JsonSchemaTypeBuilder(new CompoundDocumentResolver(new FileSystemDocumentResolver(), new HttpClientDocumentResolver(new HttpClient())));
            JsonReference reference = new(schemaFile, rootPath ?? string.Empty);
            SchemaVariant sv = ValidationSemanticsToSchemaVariant(typeBuilder.GetValidationSemantics(reference, rebaseToRootPath).Result);

            if (sv == SchemaVariant.NotSpecified)
            {
                sv = schemaVariant;
            }

            IJsonSchemaBuilder builder = GetJsonSchemaBuilder(sv, typeBuilder);

            (string RootType, ImmutableDictionary<JsonReference, TypeAndCode> GeneratedTypes) = builder.BuildTypesFor(reference, rootNamespace, rebaseToRootPath, rootTypeName: rootTypeName, validateFormat: assertFormat).Result;

            foreach (KeyValuePair<JsonReference, TypeAndCode> generatedType in GeneratedTypes)
            {
                foreach (CodeAndFilename typeAndCode in generatedType.Value.Code)
                {
                    try
                    {
                        string source = typeAndCode.Code;
                        context.AddSource($"{typeAndCode.Filename}", SourceText.From(source, Encoding.UTF8));
                    }
                    catch (Exception ex)
                    {
                            //    $"Unable to generate code for type {generatedType.Value.DotnetTypeName} from location {generatedType.Key}: {ex.Message}",

                    }
                }
            }
        }

        private SchemaVariant GetSchemaVariant(JsonSchemaTypeBuilder typeBuilder, JsonReference reference)
        {
            ValidationSemantics validationSemantics = typeBuilder.GetValidationSemantics(reference, false).Result;
            return ValidationSemanticsToSchemaVariant(validationSemantics);
        }

        private IJsonSchemaBuilder GetJsonSchemaBuilder(SchemaVariant sv, JsonSchemaTypeBuilder typeBuilder)
        {
            return sv switch
            {
                SchemaVariant.Draft4 => new Corvus.Json.CodeGeneration.Draft4.JsonSchemaBuilder(typeBuilder),
                SchemaVariant.Draft6 => new Corvus.Json.CodeGeneration.Draft6.JsonSchemaBuilder(typeBuilder),
                SchemaVariant.Draft7 => new Corvus.Json.CodeGeneration.Draft7.JsonSchemaBuilder(typeBuilder),
                SchemaVariant.Draft202012 => new Corvus.Json.CodeGeneration.Draft202012.JsonSchemaBuilder(typeBuilder),
                SchemaVariant.Draft201909 => new Corvus.Json.CodeGeneration.Draft201909.JsonSchemaBuilder(typeBuilder),
                SchemaVariant.OpenApi30 => new Corvus.Json.CodeGeneration.OpenApi30.JsonSchemaBuilder(typeBuilder),
                _ => new Corvus.Json.CodeGeneration.Draft202012.JsonSchemaBuilder(typeBuilder)
            };
        }

        private static SchemaVariant ValidationSemanticsToSchemaVariant(ValidationSemantics validationSemantics)
        {
            if (validationSemantics == ValidationSemantics.Unknown)
            {
                return SchemaVariant.NotSpecified;
            }

            if ((validationSemantics & ValidationSemantics.Draft4) != 0)
            {
                return SchemaVariant.Draft4;
            }

            if ((validationSemantics & ValidationSemantics.Draft6) != 0)
            {
                return SchemaVariant.Draft6;
            }

            if ((validationSemantics & ValidationSemantics.Draft7) != 0)
            {
                return SchemaVariant.Draft7;
            }

            if ((validationSemantics & ValidationSemantics.Draft201909) != 0)
            {
                return SchemaVariant.Draft201909;
            }

            if ((validationSemantics & ValidationSemantics.Draft202012) != 0)
            {
                return SchemaVariant.Draft202012;
            }

            if ((validationSemantics & ValidationSemantics.OpenApi30) != 0)
            {
                return SchemaVariant.OpenApi30;
            }

            return SchemaVariant.NotSpecified;
        }

    }
}