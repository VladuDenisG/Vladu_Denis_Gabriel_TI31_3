
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WordFrequency;

class Program
{
    static void Main(string[] args)
    {
        
        string folderStiri = @"C:\Users\vlady\source\repos\Vladu_Denis_Gabriel_TI31_3\Vladu_Denis_Gabriel_TI31_3\Stiri\Reuters\Reuters_34";

        string[] fisiere = Directory.GetFiles(
            folderStiri,
            "*.xml",
            SearchOption.AllDirectories
        );

        int procesate = 0;
        int erori = 0;



        string stopWordsPath = Path.Combine(
            AppContext.BaseDirectory, "stopwords.txt");

        if (!File.Exists(stopWordsPath))
        {
            Console.WriteLine("Nu gasesc stopwords.txt");
            return;
        }

        HashSet<string> stopWords = new HashSet<string>(
            File.ReadLines(stopWordsPath)
                .Select(w => w.Trim().ToLowerInvariant())
                .Where(w => w.Length > 0)
        );

        PorterStemmer stemmer = new PorterStemmer();

        Console.WriteLine("Stopwords incarcate: " + stopWords.Count);


        Dictionary<string, int> dictionarGlobal =
            new Dictionary<string, int>();

        List<Dictionary<int, int>> vectoriDocumente =
            new List<Dictionary<int, int>>();



        List<string> numeDocumente = new List<string>();

        List<List<string>> topicuriDocumente =
            new List<List<string>>();

        List<string> provenientaDocumente =
            new List<string>();






        foreach (string fisier in fisiere)
        {
            try
            {
                XDocument xml = XDocument.Load(fisier);

                string titlu = xml.Root?
                    .Element("title")?.Value ?? "";

                var nodText = xml.Root?.Element("text");

                string text = nodText == null ? "" :
                    string.Join(" ",
                        nodText.DescendantNodes()
                        .OfType<XText>()
                        .Select(x => x.Value));

                string continut = titlu + " " + text;


                continut = continut.ToLowerInvariant();


                var cuvinte = Regex.Matches(
                    continut, @"\p{L}+"
                ).Cast<Match>()
                 .Select(m => m.Value)
                 .Where(w => !stopWords.Contains(w))
                 .Select(w => stemmer.StemWord(w))
                 .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length > 1)
                 .ToList();


                Dictionary<int, int> frecvente =
                    new Dictionary<int, int>();

                foreach (string cuvant in cuvinte)
                {
                    if (!dictionarGlobal.TryGetValue(
                        cuvant, out int pozitie))
                    {
                        pozitie = dictionarGlobal.Count;
                        dictionarGlobal[cuvant] = pozitie;
                    }

                    if (frecvente.ContainsKey(pozitie))
                        frecvente[pozitie]++;
                    else
                        frecvente[pozitie] = 1;
                }

                vectoriDocumente.Add(frecvente);



                var topicuri = xml.Descendants("codes")
                    .Where(c => (string)c.Attribute("class")
                        == "bip:topics:1.0")
                    .SelectMany(c => c.Descendants("code"))
                    .Select(c => (string)c.Attribute("code")
                        ?? c.Value)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToList();


                numeDocumente.Add(
                    Path.GetFileNameWithoutExtension(fisier)
                );

                topicuriDocumente.Add(topicuri);

                string provenienta = Path.GetFileName(Path.GetDirectoryName(fisier));

                provenientaDocumente.Add(provenienta);



                procesate++;

              
            }
            catch (Exception ex)
            {
                erori++;
                Console.WriteLine("Eroare: " + ex.Message);
            }
        }



        string outputPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory,
                "..", "..", "..", "rezultat.txt")
        );


        using (StreamWriter writer = new StreamWriter(outputPath))
        {
           
            foreach (var cuvant in dictionarGlobal.OrderBy(x => x.Value))
            {
                writer.WriteLine("@attribute " + cuvant.Key);
            }

            writer.WriteLine("@data");

            
            for (int i = 0; i < vectoriDocumente.Count; i++)
            {
                string vector = string.Join(" ",
                    vectoriDocumente[i]
                        .OrderBy(x => x.Key)
                        .Select(x => x.Key + ":" + x.Value)
                );

                string topicuri = string.Join(" ",
                    topicuriDocumente[i]
                );

                writer.WriteLine(
                    numeDocumente[i] + " # " +
                    vector + " # " +
                    topicuri + " # " +
                    provenientaDocumente[i]
                );
            }
        }

        


        Console.WriteLine("Cuvinte distincte: " +
            dictionarGlobal.Count);

        Console.WriteLine("Vectori creati: " +
            vectoriDocumente.Count);

        if (vectoriDocumente.Count > 0)
        {
            var exemplu = vectoriDocumente[0]
                .OrderBy(p => p.Key)
                .Take(15)
                .Select(p => $"{p.Key}:{p.Value}");

            Console.WriteLine("Exemplu vector rar: " +
                string.Join(" ", exemplu));
        }



        Console.WriteLine("Stiri gasite: " + fisiere.Length);
        Console.WriteLine("Stiri procesate: " + procesate);
        Console.WriteLine("Erori: " + erori);
        Console.WriteLine("Procesare terminata");
    }
}
