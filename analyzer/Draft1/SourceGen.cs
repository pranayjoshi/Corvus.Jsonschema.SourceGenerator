using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Corvus.Json.SchemaGenerator
{
    [Generator]
    public class JsonSchemaGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Set up code generation pipeline
            var syntaxProvider = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(symbol => symbol != null);

            var compilationAndSymbols = context.CompilationProvider.Combine(syntaxProvider.Collect());

            context.RegisterSourceOutput(compilationAndSymbols, (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        {
            // For this example, we'll generate code for all classes
            return node is ClassDeclarationSyntax;
        }

        private static ISymbol GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            // We only care about class declarations
            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

            // Get the declared symbol for the class, and ensure it's actually a class symbol
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
            return classSymbol;
        }

        private static void Execute(Compilation compilation, ImmutableArray<ISymbol> symbols, SourceProductionContext context)
        {
            // Generate a simple source code
            string sourceCode = GenerateSourceCode(compilation, symbols);
            context.AddSource("GeneratedSource.cs", SourceText.From(sourceCode, Encoding.UTF8));
        }

        private static string GenerateSourceCode(Compilation compilation, ImmutableArray<ISymbol> symbols)
        {
            return """
                   using System;

                   namespace GeneratedNamespace
                   {
                   
                       sealed class GeneratedAttribute : Attribute
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
                       public class GeneratedClass
                       {
                           public void PrintMessage()
                           {
                               Console.WriteLine("This is a generated class!");
                           }
                       }
                   }
                   """;
        }
    }
}
