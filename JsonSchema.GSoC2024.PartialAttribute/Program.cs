[Generated("person.json")]
public partial class Person
{
}

class Program
{
    static void Main(string[] args)
    {
        var person = Person.FromJson();
        // person.PrintJsonContent();
        // Console.WriteLine($"Name: {person.Name}");
    }
}

