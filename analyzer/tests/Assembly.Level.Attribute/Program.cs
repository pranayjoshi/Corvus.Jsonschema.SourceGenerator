using System;
using GeneratedNamespace;

namespace SourceGeneratorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var gen = new GeneratedAttribute("adawdawd", "string qualification");
            var generatedClass = new GeneratedClass();
            generatedClass.PrintMessage();
            gen.PrintDetails();
        }
    }
}