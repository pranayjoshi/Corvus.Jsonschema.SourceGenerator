using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

namespace JsonSchema.GSoC2024.ExistingLibrary
{
    [Generator]
    public class JsonSchemaGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.CompilationProvider, (spc, compilation) => Execute(spc, compilation));
        }

        private static void Execute(SourceProductionContext context, Compilation compilation)
        {
            string attributeSource = GenerateAttributeSourceCode();
            context.AddSource("GeneratedAttribute.cs", SourceText.From(attributeSource, Encoding.UTF8));

            if (compilation.Assembly.GetAttributes().Any(attr => attr.AttributeClass?.Name == "GeneratedAttribute"))
            {
                string classSource = GenerateClassSourceCode();
                context.AddSource("GeneratedClass.cs", SourceText.From(classSource, Encoding.UTF8));
            }
        }

        private static string GenerateAttributeSourceCode()
        {
            return """
            using System;

            [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
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

    private static string GenerateClassSourceCode()
    {
        return """
        using System;

        namespace GeneratedNamespace
        {
            public class GeneratedClass
            {
                public void PrintDetails()
                {
                    Console.WriteLine("GeneratedClass instance");
                }
            }
        }
        """;
    }
    }
}
