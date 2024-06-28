using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace JsonSchema.GSoC2024.ExistingLibrary
{
    [Generator]
    public class JsonSchemaGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(
                context.CompilationProvider,
                (spc, compilation) => Execute(spc, compilation)
            );
        }

        private static void Execute(SourceProductionContext context, Compilation compilation)
        {
            string sourceCode = GenerateSourceCode();
            context.AddSource("GeneratedSource.cs", SourceText.From(sourceCode, Encoding.UTF8));
        }

        private static string GenerateSourceCode()
        {
            return @"
                using System;

                namespace GeneratedNamespace
                {
                    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
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
                            Console.WriteLine($""JSON Path: {JsonPath}"");
                            Console.WriteLine($""Qualification: {Qualification}"");
                        }
                    }

                    public class GeneratedClass
                    {
                        public void PrintMessage()
                        {
                            Console.WriteLine(""This is a generated class!"");
                        }
                    }
                }
            ";
        }
    }
}
