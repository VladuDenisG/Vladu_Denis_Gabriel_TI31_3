
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

class Program
{
    static void Main(string[] args)
    {
        string folderStiri = @"C:\Users\vlady\source\repos\Vladu_Denis_Gabriel_TI31_3\Vladu_Denis_Gabriel_TI31_3\Stiri";

        var fisiere = Directory.GetFiles(
            folderStiri,
            "*.xml",
            SearchOption.AllDirectories
        );

        Console.WriteLine("Numar stiri: " + fisiere.Length);

        foreach (string fisier in fisiere)
        {
            try
            {
                XDocument xml = XDocument.Load(fisier);

                string titlu = xml.Root?
                    .Element("title")?.Value ?? "";

                string text = xml.Root?
                    .Element("text")?.Value ?? "";

                Console.WriteLine("Fisier: " +
                    Path.GetFileName(fisier));

            }
            catch (Exception ex)
            {
                Console.WriteLine("Eroare la " +
                    fisier + ": " + ex.Message);
            }
        }

        Console.WriteLine("Procesare terminata!");
    }
}
