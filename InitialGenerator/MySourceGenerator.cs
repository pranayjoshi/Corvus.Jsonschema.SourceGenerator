using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
namespace JsonSchema.SourceGenerator
    
public class MySourceGenerator
{
    [Generator(LanguageNames.CSharp)]
    internal sealed partial class MySourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static void (IncrementalGeneratorPostInitializationContext context) =>
            {
                context.AddSource("JsonSchema.GSoC2024.ExistingLibrary.GeneratedAttribute.g.cs", SourceText.From(_attributeSource, Encoding.UTF8));
            });
        }
    }
}