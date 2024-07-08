using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.IO;
using System.Text.Json;

namespace JsonSchema.GSoC2024.PartialAttribute
{
    [Generator]
    public class PartialAttributeGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "GeneratedAttribute.cs", SourceText.From(GenerateAttributeCode(), Encoding.UTF8)));

            IncrementalValuesProvider<(ClassDeclarationSyntax, string, string)> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m.Item1 is not null)!;

            context.RegisterSourceOutput(classDeclarations,
                static (spc, source) => Execute(source.Item1, source.Item2, source.Item3, spc));
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
            => node is ClassDeclarationSyntax { AttributeLists: { Count: > 0 } };

        private static (ClassDeclarationSyntax?, string, string) GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
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
                            var class_name = attribute.ArgumentList?.Arguments[1].Expression.ToString().Trim('"');
                            return (classDeclarationSyntax, jsonPath ?? "", class_name ?? "");
                        }
                    }
                }
            }
            return (null, "", "");
        }

        private static void Execute(ClassDeclarationSyntax classDeclaration, string jsonPath, string class_name, SourceProductionContext context)
        {
            string classContent = GeneratePartialClassContent(classDeclaration, jsonPath, class_name);
            context.AddSource($"{classDeclaration.Identifier}.Generated.cs", SourceText.From(classContent, Encoding.UTF8));
        }

        private static string GenerateAttributeCode()
        {
            return """
            using System;

            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            public sealed class GeneratedAttribute : Attribute
            {
                public string JsonPath { get; }
                public string class_name { get; }

                public GeneratedAttribute(string jsonPath, string class_name)
                {
                    JsonPath = jsonPath;
                    class_name = class_name;
                }

                public void PrintDetails()
                {
                    Console.WriteLine($"JSON Path: {JsonPath}");
                    Console.WriteLine($"class_name: {class_name}");
                }
            }
            """;
        }

        private static string GeneratePartialClassContent(ClassDeclarationSyntax classDeclaration, string jsonPath, string class_name)
        {
            string className = classDeclaration.Identifier.ToString();
            return $$"""
            using System;
            using System.IO;
            using System.Text.Json;

            public partial class {{className}}
            {
                public void ReadAndPrintJson()
                {
                    string jsonPath = @"{{jsonPath}}";
                    string class_name = "{{class_name}}";

                    if (File.Exists(jsonPath))
                    {
                        string jsonContent = File.ReadAllText(jsonPath);
                        var jsonDocument = JsonDocument.Parse(jsonContent);
                        
                        Console.WriteLine($"JSON content for {class_name}:");
                        Console.WriteLine(JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    else
                    {
                        Console.WriteLine($"JSON file not found at path: {jsonPath}");
                    }
                }
            }
            """;
        }
    }
}