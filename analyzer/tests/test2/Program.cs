using Corvus.JsonSchema.Generator;
// using file;
// using GeneratedNamespace;
using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        var jsonFilePath = "ExampleJson.json";
        
        var jsonContent = File.ReadAllText(jsonFilePath);
        
        var generatedClassCode = JsonClassGenerator.GenerateClassFromJson(jsonFilePath, jsonContent);
        
        Console.WriteLine(generatedClassCode);
        // var example = new ExampleJson
        // {
        //     Name = "John Doe",
        //     Age = 30,
        //     IsActive = true
        // };
        // //
        // Console.WriteLine($"Name: {example.Name}, Age: {example.Age}, Active: {example.IsActive}");

    }
}