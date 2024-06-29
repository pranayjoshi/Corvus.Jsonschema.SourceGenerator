using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using Corvus.Json.CodeGeneration;
using System.Text.Json;

namespace Corvus.Json.SchemaGenerator
{
    [Generator]
    public class JsonSchemaSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<AdditionalText> schemaFiles = context.AdditionalTextsProvider
                .Where(file => file.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

            IncrementalValueProvider<(Compilation, ImmutableArray<AdditionalText>)> compilationAndSchemas
                = context.CompilationProvider.Combine(schemaFiles.Collect());

            context.RegisterSourceOutput(compilationAndSchemas, 
                (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private void Execute(Compilation compilation, ImmutableArray<AdditionalText> schemaFiles, SourceProductionContext context)
        {
            if (schemaFiles.IsDefaultOrEmpty)
            {
                return;
            }

            var typeBuilder = new JsonSchemaTypeBuilder(new CompoundDocumentResolver(new FileSystemDocumentResolver(), new HttpClientDocumentResolver(new HttpClient())));

            foreach (AdditionalText schemaFile in schemaFiles)
            {
                string schemaContent = schemaFile.GetText(context.CancellationToken)?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(schemaContent))
                {
                    continue;
                }

                try
                {
                    JsonReference reference = new(schemaFile.Path, string.Empty);
                    SchemaVariant sv = GetSchemaVariant(typeBuilder, reference);

                    IJsonSchemaBuilder builder = GetJsonSchemaBuilder(sv, typeBuilder);

                    var (RootType, GeneratedTypes) = builder.BuildTypesFor(reference, compilation.AssemblyName, false).Result;

                    foreach (var generatedType in GeneratedTypes)
                    {
                        foreach (var typeAndCode in generatedType.Value.Code)
                        {
                            context.AddSource($"{typeAndCode.Filename}", SourceText.From(typeAndCode.Code, Encoding.UTF8));
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "JSSG001",
                            "JSON Schema Generation Error",
                            $"Error generating code for schema {schemaFile.Path}: {ex.Message}",
                            "JsonSchemaSourceGenerator",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        Location.None));
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
                SchemaVariant.Draft4 => new CodeGeneration.Draft4.JsonSchemaBuilder(typeBuilder),
                SchemaVariant.Draft6 => new CodeGeneration.Draft6.JsonSchemaBuilder(typeBuilder),
                SchemaVariant.Draft7 => new CodeGeneration.Draft7.JsonSchemaBuilder(typeBuilder),
                SchemaVariant.Draft202012 => new CodeGeneration.Draft202012.JsonSchemaBuilder(typeBuilder),
                SchemaVariant.Draft201909 => new CodeGeneration.Draft201909.JsonSchemaBuilder(typeBuilder),
                SchemaVariant.OpenApi30 => new CodeGeneration.OpenApi30.JsonSchemaBuilder(typeBuilder),
                _ => new CodeGeneration.Draft202012.JsonSchemaBuilder(typeBuilder)
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