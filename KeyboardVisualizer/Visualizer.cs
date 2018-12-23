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
                    //Thread t = new Thread(new ThreadStart(() =>
                    //{
                    //    displayWindow = new DisplayWindow(1280, 720, settings);
                    //    displayWindow.Closed += new EventHandler<EventArgs>(Close);
                    //    displayWindow.Run(60, 60);
                    //}));
                    //t.Start();
                }
                else
                {
                    //displayWindow.Close();
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
            currentState = e;
            if (keyboardIO != null)
            {
                keyboardIO.currentState = currentState;
                //Task update = new Task(new Action(() =>
                //{
                //    //displayWindow.Update();
                //    //displayWindow.Render();
                //}));
                //update.Start();
                latencyMax -= 0.1;
                keyboardIO.Update();
                //if (e.BufferMilliseconds > latencyMax) latencyMax = e.BufferMilliseconds;
                //if (currentState.BufferLength == 0) keyboardIO.Title = (int)latencyMax + "ms*";
                //else keyboardIO.Title = (int)latencyMax + "ms";
            }
        }
    }
}
