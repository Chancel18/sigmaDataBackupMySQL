using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

namespace Sigmasoft.Application.Services
{
    public class ZipFileService
    { 
        public string File { get; private set; }

        public string StartPath { get; set; }

        public ZipFileService(string file)
        {
            File = file;
        }

        public void CreateFromDirectory(string zipPath)
        {
            ZipFile.CreateFromDirectory(StartPath, zipPath+".zip");
        }

        public Stream GetEntry()
        {
            Stream stm = new FileStream(this.File, FileMode.Open);
            
            ZipArchive zipArchive = new ZipArchive(stm);

            var entry = zipArchive.GetEntry(this.File);

            return entry.Open();
        }
       
    }
}
