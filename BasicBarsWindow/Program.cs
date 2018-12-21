using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Background_FFT.Base;

namespace BasicBarsWindow
{
    static class Program
    {
        static Worker bg_worker = new Worker();

        static Visualizer visualizer;

        static void Main(string[] args)
        {
            var stdIn = Console.OpenStandardInput((int)Math.Pow(2, 20));

            visualizer = new Visualizer();
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
