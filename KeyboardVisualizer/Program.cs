using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Threading;
using Background_FFT.Base;

namespace KeyboardVisualizer
{
    class Program
    {
        static Worker bg_worker = new Worker(120);

        static Visualizer visualizer;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Opened");

                visualizer = new Visualizer();
                visualizer.Enabled = true;

                Console.WriteLine("Visualizer Created");

                bg_worker.NextFftEvent += NextFft;
                Console.WriteLine("Starting Background Worker");
                bg_worker.Start();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadLine();
            }
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
