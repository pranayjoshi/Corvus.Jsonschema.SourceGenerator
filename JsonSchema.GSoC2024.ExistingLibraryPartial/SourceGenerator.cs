// PartialAttributeGenerator.cs
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace JsonSchema.GSoC2024.PartialAttribute
{
    [Generator]
    public class PartialAttributeGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Register the attribute generation
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "GeneratedAttribute.cs", SourceText.From(GenerateAttributeCode(), Encoding.UTF8)));

            // Register the main generation
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null)!;

            context.RegisterSourceOutput(classDeclarations,
                static (spc, source) => Execute(source, spc));
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
            => node is ClassDeclarationSyntax { AttributeLists: { Count: > 0 } };

        private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
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
                            return classDeclarationSyntax;
                        }
                    }
                }
            }
            return null;
        }

        private static void Execute(ClassDeclarationSyntax classDeclaration, SourceProductionContext context)
        {
            string classContent = GeneratePartialClassContent(classDeclaration);
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
                public string Qualification { get; }

                public GeneratedAttribute(string jsonPath, string qualification)
                {
                    JsonPath = jsonPath;
                    Qualification = qualification;
                }

                public void PrintDetails()
                {
                    Console.WriteLine($"JSON Path: {JsonPath}");
                    Console.WriteLine($"Qualification: {Qualification}");
                }
            }
            """;
        }

        private static string GeneratePartialClassContent(ClassDeclarationSyntax classDeclaration)
        {
            string className = classDeclaration.Identifier.ToString();
            return $$"""
            public partial class {{className}}
            {
                public void GeneratedMethod()
                {
                    Console.WriteLine("This is a generated method in a partial class!");
                }
            }
            """;
        }
    }
}