using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Background_FFT;
using Background_FFT.Base;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace BasicBarsWindow
{
    public class Visualizer : IVisualizer
    {
        NextFftEventArgs currentState = null;
        private Dictionary<string, string> settings_dict;

        Settings settings = new Settings();

        double latencyMax = 0;

        public Dictionary<string, string> Settings
        {
            get { return settings_dict; }
        }

        public DisplayWindow displayWindow = null;

        bool enabled = false;

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
                if (enabled)
                {
                    Thread t = new Thread(new ThreadStart(() =>
                    {
                        displayWindow = new DisplayWindow(1280, 720, settings);
                        displayWindow.Closed += new EventHandler<EventArgs>(Close);
                        displayWindow.Run(60, 60);
                    }));
                    t.Start();
                }
                else
                {
                    displayWindow.Close();
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
            if (displayWindow != null)
            {
                displayWindow.currentState = currentState;
                displayWindow.Update();

                //latencyMax -= 0.1;
                //if (e.BufferMilliseconds > latencyMax) latencyMax = e.BufferMilliseconds;
                latencyMax = e.BufferMilliseconds;

                if (currentState.BufferLength == 0) displayWindow.Title = (int)latencyMax + "ms*";
                    else displayWindow.Title = (int)latencyMax + "ms";
            }
        }
    }
}
