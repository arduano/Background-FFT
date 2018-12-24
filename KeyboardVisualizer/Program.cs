using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUE.NET;
using CUE.NET.Devices.Generic.Enums;
using CUE.NET.Devices.Generic;
using CUE.NET.Devices;
using CUE.NET.Brushes;
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
                //Console.WriteLine(e.Message);
                //Console.WriteLine(e.Source);
                //Console.WriteLine(e.StackTrace);
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
