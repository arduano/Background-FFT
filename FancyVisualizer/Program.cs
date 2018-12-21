using Background_FFT.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FancyVisualizer
{
    class Program
    {
        static Worker bg_worker = new Worker(120);

        static Visualizer visualizer;

        static void Main(string[] args)
        {
            var stdIn = Console.OpenStandardInput((int)Math.Pow(2, 20));

            visualizer = new Visualizer();

            foreach (string s in args)
            {
                var _s = s.ToLower().Split('=');
                if (_s[0] == "mirror")
                {
                    if (_s[1] == "true")
                    {
                        visualizer.settings.Mirror = true;
                    }
                }
            }

            visualizer.Enabled = true;

            bg_worker.NextFftEvent += NextFft;
            bg_worker.Start();
        }

        static void NextFft(object sender, NextFftEventArgs e)
        {
            visualizer.TransferFftData(e);
            if (!visualizer.Enabled)
            {
                bg_worker.Dispose();
            }
        }
    }
}
