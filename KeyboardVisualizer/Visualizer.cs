using Background_FFT.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KeyboardVisualizer
{
    class Visualizer
    {
        NextFftEventArgs currentState = null;

        double latencyMax = 0;

        public KeyboardIO keyboardIO = null;

        bool enabled = false;

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
                if (enabled)
                {
                    keyboardIO = new KeyboardIO();
                }
                else
                {
                }
            }
        }

        void Close(object s, EventArgs e)
        {
            Enabled = false;
        }

        public void ShowSettings()
        {
            throw new NotImplementedException();
        }

        public void TransferFftData(NextFftEventArgs e)
        {
            try
            {
                currentState = e;
                if (keyboardIO != null)
                {
                    keyboardIO.currentState = currentState;
                    latencyMax -= 0.1;
                    keyboardIO.Update();
                }
            }
            catch (Exception _e)
            {
                Console.WriteLine(_e.Message);
                Console.WriteLine(_e.Source);
                Console.WriteLine(_e.StackTrace);
                Console.ReadLine();
            }
        }
    }
}
