
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WordFrequency;
using System.Collections.Generic;
using System.Globalization;

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

        //lab3 etapa1
        Etapa1(outputPath);

        //lab3 etapa2
        string etapa1Path = Path.Combine(
            Path.GetDirectoryName(outputPath)!,
            "rezultat_etapa1.txt");

        Etapa2(etapa1Path, 0.5);

    }




    static double Entropie(List<string> clase)
    {
        if (clase.Count == 0)
            return 0;

        double rezultat = 0;

        foreach (var grup in clase.GroupBy(c => c))
        {
            double p = (double)grup.Count() / clase.Count;
            rezultat -= p * Math.Log2(p);
        }

        return rezultat;
    }

    static void Etapa1(string inputPath)
    {
        string[] linii = File.ReadAllLines(inputPath);

        List<string> atribute = new List<string>();
        List<(HashSet<int> Atribute, string Clasa)> documente =
            new List<(HashSet<int>, string)>();

        bool sectiuneData = false;

        foreach (string linie in linii)
        {
            string text = linie.Trim();

            if (text.StartsWith("@attribute "))
            {
                atribute.Add(text);
                continue;
            }

            if (text == "@data")
            {
                sectiuneData = true;
                continue;
            }

            if (!sectiuneData || text.Length == 0)
                continue;

            string[] parti = text.Split('#');

            if (parti.Length < 4)
                continue;

            
            if (parti[3].Trim() != "Training")
                continue;

            HashSet<int> atributePrezente = new HashSet<int>();

            string[] valori = parti[1].Trim().Split(
                ' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (string valoare in valori)
            {
                string[] pereche = valoare.Split(':');

                if (pereche.Length != 2)
                    continue;

                int pozitie = int.Parse(pereche[0]);
                int frecventa = int.Parse(pereche[1]);

                if (frecventa > 0)
                    atributePrezente.Add(pozitie);
            }

            string clasa = parti[2].Trim();

            documente.Add((atributePrezente, clasa));
        }

        if (documente.Count == 0)
        {
            Console.WriteLine("Nu exista documente Training.");
            return;
        }

        double entropieInitiala =
            Entropie(documente.Select(d => d.Clasa).ToList());

        List<string> rezultat = new List<string>();

        for (int i = 0; i < atribute.Count; i++)
        {
            var prezente = documente
                .Where(d => d.Atribute.Contains(i))
                .Select(d => d.Clasa)
                .ToList();

            var absente = documente
                .Where(d => !d.Atribute.Contains(i))
                .Select(d => d.Clasa)
                .ToList();

            double p1 = (double)prezente.Count / documente.Count;
            double p0 = (double)absente.Count / documente.Count;

            double castig = entropieInitiala
                - p1 * Entropie(prezente)
                - p0 * Entropie(absente);

            castig = Math.Max(0, castig);

            rezultat.Add(
                atribute[i] + " " +
                castig.ToString("F6", CultureInfo.InvariantCulture));
        }

        int pozitieData = Array.FindIndex(
            linii, l => l.Trim() == "@data");

        rezultat.AddRange(linii.Skip(pozitieData));

        string outputPath = Path.Combine(
            Path.GetDirectoryName(inputPath)!,
            "rezultat_etapa1.txt");

        File.WriteAllLines(outputPath, rezultat);

        Console.WriteLine("Etapa 1 terminata!");
        Console.WriteLine("Entropie initiala: " + entropieInitiala);
        Console.WriteLine("Atribute calculate: " + atribute.Count);
        Console.WriteLine("Documente Training: " + documente.Count);
        Console.WriteLine("Fisier rezultat: " + outputPath);
    }


    static void Etapa2(string inputPath, double prag)
    {
        string[] linii = File.ReadAllLines(inputPath);

        List<string> atributeSelectate = new List<string>();
        List<int> pozitiiSelectate = new List<int>();

        int indexAtribut = 0;
        int pozitieData = -1;

        
        for (int i = 0; i < linii.Length; i++)
        {
            string text = linii[i].Trim();

            if (text == "@data")
            {
                pozitieData = i;
                break;
            }

            if (text.StartsWith("@attribute "))
            {
                string[] parti = text.Split(
                    ' ', StringSplitOptions.RemoveEmptyEntries);

                double castig = double.Parse(
                    parti[parti.Length - 1],
                    CultureInfo.InvariantCulture);

                if (castig > prag)
                {
                    atributeSelectate.Add(text);
                    pozitiiSelectate.Add(indexAtribut);
                }

                indexAtribut++;
            }
        }

        if (pozitieData == -1)
        {
            Console.WriteLine("Nu exista sectiunea @data!");
            return;
        }

        
        List<string> training = new List<string>();
        List<string> testing = new List<string>();

        
        training.AddRange(atributeSelectate);
        testing.AddRange(atributeSelectate);

        training.Add("@data");
        testing.Add("@data");

        
        for (int i = pozitieData + 1; i < linii.Length; i++)
        {
            string text = linii[i].Trim();

            if (text.Length == 0)
                continue;

            string[] parti = text.Split('#');

            if (parti.Length < 4)
                continue;

            List<string> vectorNou = new List<string>();

            string[] valori = parti[1].Trim().Split(
                ' ', StringSplitOptions.RemoveEmptyEntries);

           
            foreach (string valoare in valori)
            {
                string[] pereche = valoare.Split(':');

                if (pereche.Length != 2)
                    continue;

                int pozitie = int.Parse(pereche[0]);
                int frecventa = int.Parse(pereche[1]);

                int pozitieNoua = pozitiiSelectate.IndexOf(pozitie);

                if (pozitieNoua != -1)
                {
                    vectorNou.Add(
                        (pozitieNoua + 1) + ":" + frecventa);
                }
            }

            string vectorFinal = string.Join(" ", vectorNou);

           
            string linieNoua =
                parti[0].Trim() + " # " +
                vectorFinal + " # " +
                parti[2].Trim() + " # " +
                parti[3].Trim();

       
            if (parti[3].Trim() == "Training")
            {
                training.Add(linieNoua);
            }
            else if (parti[3].Trim() == "Testing")
            {
                testing.Add(linieNoua);
            }
        }

 
        string director = Path.GetDirectoryName(inputPath)!;

        File.WriteAllLines(
            Path.Combine(director, "rezultat_etapa2_training.txt"),
            training);

        File.WriteAllLines(
            Path.Combine(director, "rezultat_etapa2_testing.txt"),
            testing);

        Console.WriteLine("Etapa 2 terminata!");
        Console.WriteLine("Atribute selectate: " +
            atributeSelectate.Count);
        Console.WriteLine("Fisierele Training si Testing au fost create.");
    }







}
