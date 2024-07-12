using System;

class Program
{
    static void Main(string[] args)
    {
        var printInstance = new PrintClass();
        printInstance.ReadAndPrintJson();
    }
}

[Generated("person.json")]
public partial class PrintClass
{
}