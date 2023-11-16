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

            string command1 = $@"nuget pack PVPlus.nuspec -Version {ver} -Properties Configuration=Release -BasePath PVPlus\bin\Release";
            string command2 = $@"squirrel --releasify PVPlus.{ver}.nupkg";


            Console.WriteLine(command1);
            Console.WriteLine(command2);

            Console.ReadLine();
        }
    }
}
