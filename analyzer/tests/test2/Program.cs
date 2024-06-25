using JsonClassGenerator;
using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        // Path to your JSON file
        var jsonFilePath = "file.json";
        
        // Read the content of the JSON file
        var jsonContent = File.ReadAllText(jsonFilePath);
        
        // Generate the C# class from the JSON content
        var generatedClassCode = JsonClassGeneratorHelper.GenerateClassFromJson(jsonFilePath, jsonContent);
        
        // Output the generated class code (or save it to a file, use it, etc.)
        Console.WriteLine(generatedClassCode);
    }
}