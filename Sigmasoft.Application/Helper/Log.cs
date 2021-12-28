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
        private const string PATH = "\\logs\\file.log";


        public static void WriteToFile(string msg)
        {
            if (File.Exists(Directory.GetCurrentDirectory() + PATH))
            {
                fw = new StreamWriter(new FileStream(Directory.GetCurrentDirectory() + PATH, FileMode.Append));
                fw.WriteLine("[" + DateTime.Now + "] [INFO] " + msg);
                fw.Close();
            }
            else
            {
                fw = new StreamWriter(new FileStream(Directory.GetCurrentDirectory() + PATH, FileMode.Create));
                fw.WriteLine("[" + DateTime.Now + "] [INFO] " + msg);
                fw.Close();
            }
        }
    }
}
