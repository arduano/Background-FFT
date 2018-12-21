using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Background_FFT
{
    class Module
    {
        public Module(string name)
        {

        }

        public static string[] GetModules(string path, string thisPath)
        {
            List<string> modules = new List<string>();
            foreach (string f in Directory.GetFiles(path))
            {
                if (Path.GetExtension(f) == ".exe" && thisPath != f && thisPath != Path.GetFileName(f)) modules.Add(f);
            }
            return modules.ToArray();
        }
    }
}
