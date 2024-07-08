using System;

class Program
{
    static void Main(string[] args)
    {
        var printInstance = new PrintClass();
        printInstance.ReadAndPrintJson();
    }
}

[Generated("person.json", "SampleData")]
public partial class PrintClass
{
}