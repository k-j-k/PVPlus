using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string ver = typeof(PVPlus.MainPVForm).Assembly.GetName().Version.ToString(3);

            string command1 = $@"nuget pack PVPlus.nuspec -Version {ver} -Properties Configuration=Release -BasePath PVPlus\bin\Release\;";
            string command2 = $@"squirrel --releasify PVPlus.{ver}.nupkg;";
            string command3 = $@"git add Releases\RELEASES -f;";
            string command4 = $@"git add Releases\PVPlus-{ver}-full.nupkg -f;";
            string command5 = $@"git add Releases\PVPlus-{ver}-delta.nupkg -f;";
            string command6 = $"git commit -m \"updated\";";
            string command7 = $"git push origin master;";

            Console.WriteLine(command1);
            Console.WriteLine(command2);
            Console.WriteLine(command3);
            Console.WriteLine(command4);
            Console.WriteLine(command5);
            Console.WriteLine(command6);
            Console.WriteLine(command7);

            Console.ReadLine();

        }

        [Conditional("DEBUG")]
        static void a()
        {
            WebClient web = new WebClient();
            System.IO.Stream stream = web.OpenRead("https://raw.githubusercontent.com/k-j-k/PVPlus/master/Releases/RELEASES");
            using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
            {
                String text = reader.ReadToEnd();
            }
        }
    }
}
