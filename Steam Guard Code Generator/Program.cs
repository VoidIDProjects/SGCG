using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using Steam_Guard_Code_Generator.Model;

namespace Steam_Guard_Code_Generator
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string file;
            SteamGuardData account;
            string code;

            file = getMaFile();
            if (file == null) { Console.WriteLine("END"); Console.ReadKey(); return; }
            account = getAccount(file);
            code = account.GenerateCode();
            Console.WriteLine($"Code: {code}");
            Clipboard.Clear();
            Clipboard.SetText(code);
            Console.WriteLine("Code copied to buffer.\nEND.");
            Console.ReadKey();
        }

        private static string getMaFile()
        {
            string path;
            Console.Write("MaFile: ");
            Thread.Sleep(TimeSpan.FromSeconds(1));
            path = FileIO.GetFilePath(FileIO.FILTER_MAFILE);
            Console.Write($"{(path != null ? path : "null")}\n");
            if (path == null) return null;
            return path;
        }
        private static SteamGuardData getAccount(string pathFile)
        {
            return JsonConvert.DeserializeObject<SteamGuardData>(File.ReadAllText(pathFile));
        }
    }
}
