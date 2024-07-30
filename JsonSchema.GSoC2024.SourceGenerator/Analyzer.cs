// using System.Collections.Immutable;
// using System.ComponentModel;
// using System.Diagnostics.CodeAnalysis;
// using Corvus.Json.CodeGeneration;
// using System.Text.Json;
// using Microsoft.CodeAnalysis.CSharp;
// using Spectre.Console.Cli;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.Text;
// using System.Text;

// namespace Corvus.Json.SchemaGenerator
// {
//     [Generator]
//     public class JsonSchemaSourceGenerator : IIncrementalGenerator
//     {
//         public void Initialize(IncrementalGeneratorInitializationContext context)
//         {
//             IncrementalValuesProvider<AdditionalText> schemaFiles = context.AdditionalTextsProvider
//                 .Where(file => file.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

//             IncrementalValueProvider<(Compilation, ImmutableArray<AdditionalText>)> compilationAndSchemas
//                 = context.CompilationProvider.Combine(schemaFiles.Collect());

//             context.RegisterSourceOutput(compilationAndSchemas, 
//                 (spc, source) => Execute(source.Item1, source.Item2, spc));
//         }

//          private void Execute(Compilation compilation, ImmutableArray<AdditionalText> schemaFiles, SourceProductionContext context)
//         {
//             if (schemaFiles.IsDefaultOrEmpty)
//             {
//                 return;
//             }

//             foreach (AdditionalText schemaFile in schemaFiles)
//             {
//                 string schemaContent = schemaFile.GetText(context.CancellationToken)?.ToString() ?? string.Empty;
//                 if (string.IsNullOrEmpty(schemaContent))
//                 {
//                     continue;
//                 }

//                 try
//                 {
//                     GenerateTypes(schemaFile.Path, compilation.AssemblyName, null, false, null, null, null, SchemaVariant.NotSpecified, true, context);
//                 }
//                 catch (Exception ex)
//                 {
//                            // $"Error generating code for schema {schemaFile.Path}: {ex.Message}",
//                 }
//             }
//         }

//         private void GenerateTypes(string schemaFile, string rootNamespace, string? rootPath, bool rebaseToRootPath, string? outputPath, string? outputMapFile, string? rootTypeName, SchemaVariant schemaVariant, bool assertFormat, SourceProductionContext context)
//         {
//             var typeBuilder = new JsonSchemaTypeBuilder(new CompoundDocumentResolver(new FileSystemDocumentResolver(), new HttpClientDocumentResolver(new HttpClient())));
//             JsonReference reference = new(schemaFile, rootPath ?? string.Empty);
//             SchemaVariant sv = ValidationSemanticsToSchemaVariant(typeBuilder.GetValidationSemantics(reference, rebaseToRootPath).Result);

//             if (sv == SchemaVariant.NotSpecified)
//             {
//                 sv = schemaVariant;
//             }

//             IJsonSchemaBuilder builder = GetJsonSchemaBuilder(sv, typeBuilder);

//             (string RootType, ImmutableDictionary<JsonReference, TypeAndCode> GeneratedTypes) = builder.BuildTypesFor(reference, rootNamespace, rebaseToRootPath, rootTypeName: rootTypeName, validateFormat: assertFormat).Result;

//             foreach (KeyValuePair<JsonReference, TypeAndCode> generatedType in GeneratedTypes)
//             {
//                 foreach (CodeAndFilename typeAndCode in generatedType.Value.Code)
//                 {
//                     try
//                     {
//                         string source = typeAndCode.Code;
//                         context.AddSource($"{typeAndCode.Filename}", SourceText.From(source, Encoding.UTF8));
//                     }
//                     catch (Exception ex)
//                     {
//                             //    $"Unable to generate code for type {generatedType.Value.DotnetTypeName} from location {generatedType.Key}: {ex.Message}",

//                     }
//                 }
//             }
//         }

//         private SchemaVariant GetSchemaVariant(JsonSchemaTypeBuilder typeBuilder, JsonReference reference)
//         {
//             ValidationSemantics validationSemantics = typeBuilder.GetValidationSemantics(reference, false).Result;
//             return ValidationSemanticsToSchemaVariant(validationSemantics);
//         }

//         private IJsonSchemaBuilder GetJsonSchemaBuilder(SchemaVariant sv, JsonSchemaTypeBuilder typeBuilder)
//         {
//             return sv switch
//             {
//                 SchemaVariant.Draft4 => new CodeGeneration.Draft4.JsonSchemaBuilder(typeBuilder),
//                 SchemaVariant.Draft6 => new CodeGeneration.Draft6.JsonSchemaBuilder(typeBuilder),
//                 SchemaVariant.Draft7 => new CodeGeneration.Draft7.JsonSchemaBuilder(typeBuilder),
//                 SchemaVariant.Draft202012 => new CodeGeneration.Draft202012.JsonSchemaBuilder(typeBuilder),
//                 SchemaVariant.Draft201909 => new CodeGeneration.Draft201909.JsonSchemaBuilder(typeBuilder),
//                 SchemaVariant.OpenApi30 => new CodeGeneration.OpenApi30.JsonSchemaBuilder(typeBuilder),
//                 _ => new CodeGeneration.Draft202012.JsonSchemaBuilder(typeBuilder)
//             };
//         }

//         private static SchemaVariant ValidationSemanticsToSchemaVariant(ValidationSemantics validationSemantics)
//         {
//             if (validationSemantics == ValidationSemantics.Unknown)
//             {
//                 return SchemaVariant.NotSpecified;
//             }

//             if ((validationSemantics & ValidationSemantics.Draft4) != 0)
//             {
//                 return SchemaVariant.Draft4;
//             }

//             if ((validationSemantics & ValidationSemantics.Draft6) != 0)
//             {
//                 return SchemaVariant.Draft6;
//             }

//             if ((validationSemantics & ValidationSemantics.Draft7) != 0)
//             {
//                 return SchemaVariant.Draft7;
//             }

//             if ((validationSemantics & ValidationSemantics.Draft201909) != 0)
//             {
//                 return SchemaVariant.Draft201909;
//             }

//             if ((validationSemantics & ValidationSemantics.Draft202012) != 0)
//             {
//                 return SchemaVariant.Draft202012;
//             }

//             if ((validationSemantics & ValidationSemantics.OpenApi30) != 0)
//             {
//                 return SchemaVariant.OpenApi30;
//             }

//             return SchemaVariant.NotSpecified;
//         }

//         }
// }