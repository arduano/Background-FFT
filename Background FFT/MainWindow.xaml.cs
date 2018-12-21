using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Background_FFT.Base;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace Background_FFT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool Running = false;

        Worker bg_worker = new Worker();

        string LogText = "";

        NextFftEventArgs currentState = null;

        string[] ModuleNames;

        List<IVisualizer> visualizers = new List<IVisualizer>();

        float latencyAvg = 0;

        double amplitudeMax = 0;
        double amplitudeDecay = 0.99;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void Log(string s)
        {
            LogText += "\n" + s;
            this.Dispatcher.Invoke(() =>
            {
                Log_Box.Text = LogText;
                Log_Box.ScrollToEnd();
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var l = new Label();
            l.Content = "BasicBarWindow";
            AddItemType.Items.Add(l);
            Log("Program Started");
            bg_worker.NextFftEvent += NextFft;
            bg_worker.Start();
            Log("Background FFT worker started");

            string p = System.Reflection.Assembly.GetExecutingAssembly().Location;

            ModuleNames = Module.GetModules(System.IO.Path.GetDirectoryName(p), p);


        }



        public void NextFft(object sender, NextFftEventArgs e)
        {
            currentState = e;
            foreach (IVisualizer vis in visualizers)
            {
                vis.TransferFftData(e);
            }
            amplitudeMax *= amplitudeDecay;
            if (amplitudeMax < e.Volume) amplitudeMax = e.Volume;
            double h = (Math.Sqrt(amplitudeMax) * -150 + 240) % 360;
            if (h < 0) h += 360;
            latencyAvg = (latencyAvg * 20 + e.BufferMilliseconds) / 21;
            this.Dispatcher.Invoke(() =>
            {
                ((SolidColorBrush)MainWpfWindow.Resources["BackgroundColor"]).Color = ColorFromHsb((int)(h), 1, .5f);
                if (e.BufferLength == 0) LatencyLabel.Content = (int)latencyAvg + "ms*";
                else LatencyLabel.Content = (int)latencyAvg + "ms";
            });
            int index = 0;
            double max = 0; ;
            for (int i = 1; i < e.ResultLength; i++)
            {
                if (e.FftData[i] > max)
                {
                    max = e.FftData[i];
                    index = i;
                }
            }
            var process = new Process();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bg_worker.Dispose();
        }

        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            if (bg_worker.Paused)
            {
                Stop_Button.Content = "Stop";
                Log("Background worker started");
            }
            else
            {
                Stop_Button.Content = "Start";
                Log("Background worker paused");
            }
            bg_worker.Paused = !bg_worker.Paused;
        }

        private void Remove_Button_Click(object sender, RoutedEventArgs e)
        {
            if (VisualizersListBox.Items.Count == 0 || VisualizersListBox.SelectedItem == null) return;
            int i = VisualizersListBox.SelectedIndex;
            VisualizersListBox.Items.RemoveAt(i);
            visualizers.RemoveAt(i);
        }

        private void Edit_Button_Click(object sender, RoutedEventArgs e)
        {
            if (VisualizersListBox.SelectedItem == null) return;
        }

        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {
            if (AddItemType.SelectedItem == null) return;
            string name = (string)((Label)AddItemType.SelectedItem).Content;
            if (name == "BasicBarWindow")
            {
                visualizers.Add(new BasicBarsWindow.Visualizer());
                Label l = new Label();
                l.Content = "BasicBarWindow";
                l.IsEnabled = false;
                l.Style = (Style)MainWpfWindow.Resources["ListLabel"];
                VisualizersListBox.Items.Add(l);
            }
        }

        private void Enable_Disable_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!Enable_Disable_Button.IsEnabled) return;

            if (visualizers[VisualizersListBox.SelectedIndex].Enabled)
            {
                Enable_Disable_Button.Content = "Enable";
                visualizers[VisualizersListBox.SelectedIndex].Enabled = false;
                ((Label)VisualizersListBox.SelectedItem).IsEnabled = false;
            }
            else
            {
                Enable_Disable_Button.Content = "Disable";
                visualizers[VisualizersListBox.SelectedIndex].Enabled = true;
                ((Label)VisualizersListBox.SelectedItem).IsEnabled = true;
            }
        }

        private void VisualizersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VisualizersListBox.SelectedItem == null)
            {
                Enable_Disable_Button.IsEnabled = false;
            }
            else
            {
                Enable_Disable_Button.IsEnabled = true;
                if (visualizers[VisualizersListBox.SelectedIndex].Enabled) Enable_Disable_Button.Content = "Disable";
                else Enable_Disable_Button.Content = "Enable";
            }
        }

        public static Color ColorFromHsb(float h, float s, float b)
        {
            if (0 == s)
            {
                return Color.FromArgb(255, Convert.ToByte(b * 255),
                  Convert.ToByte(b * 255), Convert.ToByte(b * 255));
            }

            float fMax, fMid, fMin;
            byte iSextant, iMax, iMid, iMin;

            if (0.5 < b)
            {
                fMax = b - (b * s) + s;
                fMin = b + (b * s) - s;
            }
            else
            {
                fMax = b + (b * s);
                fMin = b - (b * s);
            }

            iSextant = (byte)Math.Floor(h / 60f);
            if (300f <= h)
            {
                h -= 360f;
            }
            h /= 60f;
            h -= 2f * (float)Math.Floor(((iSextant + 1f) % 6f) / 2f);
            if (0 == iSextant % 2)
            {
                fMid = h * (fMax - fMin) + fMin;
            }
            else
            {
                fMid = fMin - h * (fMax - fMin);
            }

            iMax = Convert.ToByte(fMax * 255);
            iMid = Convert.ToByte(fMid * 255);
            iMin = Convert.ToByte(fMin * 255);

            switch (iSextant)
            {
                case 1:
                    return Color.FromArgb(255, iMid, iMax, iMin);
                case 2:
                    return Color.FromArgb(255, iMin, iMax, iMid);
                case 3:
                    return Color.FromArgb(255, iMin, iMid, iMax);
                case 4:
                    return Color.FromArgb(255, iMid, iMin, iMax);
                case 5:
                    return Color.FromArgb(255, iMax, iMin, iMid);
                default:
                    return Color.FromArgb(255, iMax, iMid, iMin);
            }
        }
    }
}
