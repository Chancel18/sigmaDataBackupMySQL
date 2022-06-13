using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sigmasoft.Application.Helper
{
    public static class Log
    {
        private static StreamWriter fw;

        private static string directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        private const string PATH = "\\logs\\file.log";


        public static void WriteToFile(string msg)
        {
            //var chemin = directory + PATH;
            var chemin = Directory.GetCurrentDirectory() + PATH;

            if (File.Exists(chemin))
            {
                fw = new StreamWriter(new FileStream(chemin, FileMode.Append));
                fw.WriteLine("[" + DateTime.Now + "] [INFO] " + msg);
                fw.Close();
            }
            else
            {
                fw = new StreamWriter(new FileStream(chemin, FileMode.Create));
                fw.WriteLine("[" + DateTime.Now + "] [INFO] " + msg);
                fw.Close();
            }
        }
    }
}
