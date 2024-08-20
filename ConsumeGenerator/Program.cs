
using Corvus.Json;
using SourceGenTest2.Model;

FlimFlam flimFlam = JsonAny.ParseValue("[1,2,3]"u8);
Console.WriteLine(flimFlam);
JsonArray array = flimFlam.As<JsonArray>();
Console.WriteLine(array);