using System;
using GeneratedNamespace;

[assembly: GeneratedAttribute("path/to/json", "Assembly")]

namespace AssemblyAttributeExample
{
    class Program
    {
        static void Main(string[] args)
        {
            var generatedClass = new GeneratedClass();
            generatedClass.PrintDetails();
            var generatedAttribute = new GeneratedAttribute("path/to/json", "Assemblyi");
            generatedAttribute.PrintDetails();
        }
    }
}
