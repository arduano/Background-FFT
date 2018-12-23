using Background_FFT.Base;
using CUE.NET;
using CUE.NET.Brushes;
using CUE.NET.Devices;
using CUE.NET.Devices.Generic;
using CUE.NET.Devices.Generic.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KeyboardVisualizer
{
    class KeyboardIO
    {
        public NextFftEventArgs currentState;

        ICueDevice[] devices;

        double[] displayBars = new double[0];
        double fluidMax = 0;

        double[] barStrengthMap = new double[0];

        double Hue = 0;

        public KeyboardIO()
        {
            CueSDK.Initialize();
            devices = CueSDK.InitializedDevices.Where(a => a.DeviceInfo.Type == CorsairDeviceType.Keyboard).ToArray();
            CueSDK.UpdateMode = UpdateMode.Manual;
            double offset = 4;
            double offset2 = 0;
            foreach (var kb in devices)
            {
                kb.Brush = (SolidColorBrush)Color.Transparent;
            }
        }

        double[] shiftArr(double[] x, int e)
        {
            var z = new double[x.Length];
            int _i;
            for (int i = 0; i < x.Length; i++)
            {
                _i = (i + e) % x.Length;
                while (_i < 0) _i += x.Length;
                z[_i] = x[i];
            }
            return z;
        }

        double[] PartialInverseInterpolate(double[] x, int width, double smoothness)
        {
            double f(double i)
            {
                var a = i / smoothness;
                return 1 / (a * a + 1);
            }
            var div = 0.0;
            for (int i = -width; i <= width; i++) div += f(i);
            var z = new double[x.Length];
            for (int i = -width; i <= width; i++)
            {
                var shifty = shiftArr(x, i);
                var _f = f(i);
                Parallel.For(0, x.Length, new Action<int>(j =>
                {
                    z[j] += shifty[j] * _f / div;
                }));
            }
            return z;
        }

        static void setBrightness(ICueDevice kb, double[] heights, double[] reds, double[] greens, double[] blues)
        {
            float minx = kb.Leds.Select(l => l.LedRectangle.X).Min();
            float miny = kb.Leds.Select(l => l.LedRectangle.Y).Min();
            float maxx = kb.Leds.Select(l => l.LedRectangle.X + l.LedRectangle.Width).Max();
            float maxy = kb.Leds.Select(l => l.LedRectangle.Y + l.LedRectangle.Height).Max();
            float kwidth = maxx - minx;
            float kheight = maxy - miny;
            var width = heights.Length + 1;
            var height = 1;
            foreach (var key in kb.Leds)
            {
                double x0 = (key.LedRectangle.X - minx) / kwidth * width;
                double y0 = height - (key.LedRectangle.Y - miny) / kheight * height;
                double w = key.LedRectangle.Width / kwidth * width;
                double h = key.LedRectangle.Height / kheight * height;

                double x1 = x0 + w;
                double y1 = y0 - h;

                double strength = 0;
                double red = 0;
                double green = 0;
                double blue = 0;
                if (x1 > heights.Length) x1 = heights.Length;
                for (int i = (int)x0; i < (int)x1; i++) //not the most efficent solution but faster
                {
                    double _h = heights[i];
                    if (_h > y0)
                    {
                        strength += 1 / w;
                        red += reds[i];
                        green += greens[i];
                        blue += blues[i];
                    }
                    else if (_h > y1)
                    {
                        var scale = (_h - y1) / h;
                        strength += 1 / w * scale;
                        red += reds[i] * scale;
                        green += greens[i] * scale;
                        blue += blues[i] * scale;
                    }
                }
                if (strength > 1) strength = 1;
                var max = Math.Max(Math.Max(red, green), blue);
                max /= strength;
                red /= max;
                green /= max;
                blue /= max;
                key.Color = new CorsairColor((byte)(255 * red) , (byte)(255 * green), (byte)(255 * blue));
            }
            kb.Update();
        }

        public void Update()
        {
            if (currentState == null) return;
            var fftstate = (double[])currentState.FftData.Clone();
            //for (int i = 0; i < fftstate.Length; i++) fftstate[i] *= volumeSlider.CurrVal;
            int bars = 256;
            if (displayBars.Length != bars) displayBars = new double[bars];
            if (barStrengthMap.Length != bars) barStrengthMap = new double[bars];

            var vals = new double[bars];
            double starthz = 0;
            double endhz = 500;
            double hzstep = (endhz - starthz) / bars;

            Parallel.For(0, bars, new Action<int>(i =>
            {
                double hz = starthz + hzstep * i;
                double index = currentState.FrequencyToIndex(hz);
                vals[i] = fftstate[(int)Math.Floor(index)];
            }));

            Parallel.For(0, bars, new Action<int>(i =>
            {
                if (vals[i] < 0.00005) vals[i] = 0.00005;
                vals[i] = Math.Pow(vals[i], .7) * 2;
            }));
            
            vals = PartialInverseInterpolate(vals, bars / 16, 10);

            //Console.WriteLine(bass);

            Parallel.For(0, bars, new Action<int>(i =>
            {
                if (vals[i] > barStrengthMap[i])
                    barStrengthMap[i] = vals[i];
                else
                    barStrengthMap[i] = (barStrengthMap[i] * 50 + vals[i]) / 51;
            }));
            var mapSmooth = PartialInverseInterpolate(barStrengthMap, bars / 2, 50);
            var max = vals.Max();
            if (max > fluidMax)
                fluidMax = (fluidMax * 10 + max) / 11;
            else
                fluidMax = (fluidMax * 50 + max) / 51;
            Parallel.For(0, bars, new Action<int>(i =>
            {
                vals[i] = (vals[i] / mapSmooth[i] + vals[i]) / 2;
            }));

            var barVolume = (mapSmooth.Sum() / bars * 15 + currentState.Volume / 2);
            double bass = Math.Max(0, barVolume - 1);
            {
                int b = (int)(bass * 20);
                string s = "";
                for (int i = 0; i < b; i++) s += '#';
                Console.WriteLine(s);
            }
            Hue += bass;

            var reds = new double[bars];
            var greens = new double[bars];
            var blues = new double[bars];
            Parallel.For(0, bars, new Action<int>(i =>
            {
                //HlsToRgb(i * 360.0 / bars, 0.5, 1, out reds[i], out greens[i], out blues[i]);
                var hue = mapSmooth[i] * 700 + Hue;
                HlsToRgb(hue % 360, 0.5, 1, out reds[i], out greens[i], out blues[i]);
            }));

            foreach (var kb in devices)
            {
                setBrightness(kb, vals, reds, greens, blues);
            }
        }
        
        public static void RgbToHls(double r, double g, double b,
            out double h, out double l, out double s)
        {
            double double_r = r / 255.0;
            double double_g = g / 255.0;
            double double_b = b / 255.0;
            
            double max = double_r;
            if (max < double_g) max = double_g;
            if (max < double_b) max = double_b;

            double min = double_r;
            if (min > double_g) min = double_g;
            if (min > double_b) min = double_b;

            double diff = max - min;
            l = (max + min) / 2;
            if (Math.Abs(diff) < 0.00001)
            {
                s = 0;
                h = 0;  
            }
            else
            {
                if (l <= 0.5) s = diff / (max + min);
                else s = diff / (2 - max - min);

                double r_dist = (max - double_r) / diff;
                double g_dist = (max - double_g) / diff;
                double b_dist = (max - double_b) / diff;

                if (double_r == max) h = b_dist - g_dist;
                else if (double_g == max) h = 2 + r_dist - b_dist;
                else h = 4 + g_dist - r_dist;

                h = h * 60;
                if (h < 0) h += 360;
            }
        }
        
        public static void HlsToRgb(double h, double l, double s,
            out double r, out double g, out double b)
        {
            double p2;
            if (l <= 0.5) p2 = l * (1 + s);
            else p2 = l + s - l * s;

            double p1 = 2 * l - p2;
            double double_r, double_g, double_b;
            if (s == 0)
            {
                double_r = l;
                double_g = l;
                double_b = l;
            }
            else
            {
                double_r = QqhToRgb(p1, p2, h + 120);
                double_g = QqhToRgb(p1, p2, h);
                double_b = QqhToRgb(p1, p2, h - 120);
            }
            
            r = double_r * 255.0;
            g = double_g * 255.0;
            b = double_b * 255.0;
        }

        private static double QqhToRgb(double q1, double q2, double hue)
        {
            if (hue > 360) hue -= 360;
            else if (hue < 0) hue += 360;

            if (hue < 60) return q1 + (q2 - q1) * hue / 60;
            if (hue < 180) return q2;
            if (hue < 240) return q1 + (q2 - q1) * (240 - hue) / 60;
            return q1;
        }
    }
}
