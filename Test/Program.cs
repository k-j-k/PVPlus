using System;
using System.Collections.Generic;
using System.Linq;
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

            Console.WriteLine(command1);
            Console.WriteLine(command2);
            Console.WriteLine(command3);
            Console.WriteLine(command4);
            Console.WriteLine(command5);

            Console.ReadLine();
        }
    }
}
