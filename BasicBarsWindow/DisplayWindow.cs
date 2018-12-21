using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Background_FFT.Base;
using System.Drawing;
using OpenTK.Input;

namespace BasicBarsWindow
{
    public class DisplayWindow : GameWindow
    {
        View view;

        Settings setting;

        public NextFftEventArgs currentState;

        bool fullscreen = false;

        float[] points;

        public bool Fullscreen
        {
            get
            {
                return fullscreen;
            }
            private set
            {
                fullscreen = value;
                if (fullscreen)
                {
                    DisplayDevice disp = null;
                    foreach (var d in DisplayDevice.AvailableDisplays)
                    {
                        if (d.Bounds.Contains(Mouse.X, Mouse.Y))
                        {
                            disp = d;
                            break;
                        }
                    }
                    if (disp == null)
                    {
                        fullscreen = false;
                        return;
                    }
                    WindowBorder = WindowBorder.Hidden;
                    WindowState = WindowState.Fullscreen;
                }
                else
                {
                    WindowBorder = WindowBorder.Resizable;
                    WindowState = WindowState.Normal;
                }
            }
        }

        DateTime last_click_time = DateTime.Now;

        int pointsBufferId;
        int indexBufferId;

        uint[] indexes;

        public DisplayWindow(int width, int height, Settings s)
            : base(width, height)
        {
            view = new View(new Vector2(0, 0), 0, 2);
            setting = s;
            VSync = VSyncMode.On;
            points = new float[setting.Bars * 8];
            indexes = new uint[setting.Bars * 4];

            for (uint i = 0; i < indexes.Length; i++) indexes[i] = i;

            GL.GenBuffers(1, out pointsBufferId);
            GL.BindBuffer(BufferTarget.ArrayBuffer, pointsBufferId);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(points.Length * 4),
                points,
                BufferUsageHint.StaticDraw);

            GL.GenBuffers(1, out indexBufferId);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(indexes.Length * 4),
                indexes,
                BufferUsageHint.StaticDraw);

        }

        double[] barLengths = new double[1];

        public void updateSettings()
        {

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        double Interepolate(double s, double a, double b)
        {
            double c = Math.Cos(s * Math.PI) / 2 + .5;
            return a * c + (1 - c) * b;
        }

        double[] AkimaInterepolate(double[] x, double[] y, double[] xi)
        {
            if (x.Length != y.Length) throw new Exception("Length of x's and y's must be equal");

            int n = x.Length;

            double[] dx = new double[n - 1];
            for (int i = 0; i < dx.Length; i++) dx[i] = x[i + 1] - x[i];

            double[] m = new double[n - 1];
            for (int i = 0; i < m.Length; i++) m[i] = (y[i + 1] - y[i]) / dx[i];

            double mm = 2 * m[0] - m[1];
            double mmm = 2.0 * mm - m[0];
            double mp = 2.0 * m[n - 2] - m[n - 3];
            double mpp = 2.0 * mp - m[n - 2];

            double[] m1 = new double[m.Length + 4];
            Array.Copy(m, 0, m1, 2, m.Length);
            m1[0] = mmm;
            m1[1] = mm;
            m1[m1.Length - 1] = mp;
            m1[m1.Length - 2] = mpp;

            double[] dm = new double[m1.Length - 1];
            for (int i = 0; i < dm.Length; i++) dm[i] = Math.Abs(m1[i + 1] - m1[i]);

            double[] f1 = new double[n];
            Array.Copy(dm, 2, f1, 0, n);
            double[] f2 = new double[n];
            Array.Copy(dm, 0, f1, 0, n);
            double[] f12 = new double[n];
            for (int i = 0; i < n; i++) f12[i] = f1[i] + f2[i];

            List<int> _ids = new List<int>();
            double f12_max = f12.Max();
            for (int i = 0; i < f12.Length; i++)
                if (f12[i] > f12_max * 1e-9)
                    _ids.Add(i);
            int[] ids = _ids.ToArray();

            double[] b = new double[n];
            Array.Copy(m1, 1, b, 0, n);

            foreach (int i in ids)
                b[i] = (f1[i] * m1[i + 1] + f2[i] * m1[i + 2]) / f12[i];

            double[] c = new double[m.Length];
            for (int i = 0; i < c.Length; i++) c[i] = (3.0 * m[i] - 2.0 * b[i] - b[i + 1]) / dx[i];
            double[] d = new double[m.Length];
            for (int i = 0; i < d.Length; i++) d[i] = (b[i] + b[i + 1] - 2.0 * m[i]) / (dx[i] * dx[i]);

            int[] bb = new int[xi.Length];
            int _i = 0;
            for (int i = 0; i < xi.Length; i++)
            {
                while (!(x[_i] >= xi[i] && x[_i + 1] > xi[i]))
                {
                    _i++;
                    if (_i > n - 2) throw new Exception("xi values larger than second largest x values");
                }
                bb[i] = _i;
            }

            double[] wj = new double[xi.Length];
            for (int i = 0; i < xi.Length; i++)
                wj[i] = xi[i] - x[bb[i]];

            double[] result = new double[xi.Length];
            for (int i = 0; i < xi.Length; i++) result[i] = ((wj[i] * d[bb[i]] + c[bb[i]]) * wj[i] + b[bb[i]]) * wj[i] + y[bb[i]];
            return result;
        }

        public void Update()
        {
            var fftstate = (double[])currentState.FftData.Clone();
            int bars = setting.Bars;
            if (bars != barLengths.Length)
                barLengths = new double[bars];
            var vals = new double[bars];
            double starthz = setting.StartFrequency;
            double endhz = setting.EndFrequency;
            double hzstep = (endhz - starthz) / bars;

            double[] X = new double[fftstate.Length];
            double[] Y = new double[fftstate.Length];
            double[] Xi = new double[bars];
            double[] Yi;

            for (int i = 1; i < fftstate.Length - 1; i++)
            {
                fftstate[i] = (fftstate[i] + fftstate[i - 1] + fftstate[i + 1]) / 3;
            }
            for (int i = 0; i < fftstate.Length; i++)
            {
                X[i] = i;
                Y[i] = fftstate[i];
            }

            for (int i = 0; i < bars; i++)
            {
                double hz = starthz + hzstep * i;
                double index = currentState.FrequencyToIndex(hz);
                Xi[i] = index;
                //if (index % 1 == 0)
                //    if (index >= fftstate.Length) vals[i] = 0;
                //    else vals[i] = fftstate[(int)index];
                //else
                //{
                //    if (index < fftstate.Length - 1) vals[i] = Interepolate(index % 1,
                //        fftstate[(int)Math.Floor(index)],
                //        fftstate[(int)Math.Ceiling(index)]);
                //    else if (index > fftstate.Length - 1) vals[i] = Interepolate(index % 1,
                //       fftstate[(int)Math.Floor(index)],
                //       0);
                //    else vals[i] = 0;
                //}
                //if (vals[i] < 0.0001) vals[i] = 0.0001;
            }

            Yi = AkimaInterepolate(X, Y, Xi);
            vals = Yi;
            for (int i = 0; i < bars; i++)
            {
                if (vals[i] < 0.00005) vals[i] = 0.00005;
            }
            for (int i = 1; i < bars - 1; i++)
            {
                vals[i] = (vals[i] + vals[i - 1] + vals[i + 1]) / 3;
            }

            unsafe
            {
                //for (int i = bars - 1; i > 0; i--)
                //{
                //    barLengths[i] = barLengths[i - 1];
                //}
                for (int i = 0; i < bars; i++)
                {
                    barLengths[i] = (barLengths[i] * setting.BarAveragingSample + vals[i]) / (setting.BarAveragingSample + 1);
                    if (vals[i] > barLengths[i]) barLengths[i] = vals[i];
                }
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (currentState == null) return;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.LoadIdentity();
            view.ApplyTransform(ClientRectangle.Width, ClientRectangle.Height);

            GL.EnableClientState(ArrayCap.VertexArray);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Color3(1.0, 0, 1.0);
            float step = 1.0f / barLengths.Length;
            int a = 0;
            float height = 0;
            float x = 0;
            float xps = 0;
            for (int i = 0; i < barLengths.Length; i++)
            {
                height = (float)Math.Pow(barLengths[i], 0.7);
                x = step * i - .5f;
                xps = x + step;
                //GL.Vertex2(x, height);
                //GL.Vertex2(x, -height);
                //GL.Vertex2(x + step, -height);
                //GL.Vertex2(x + step, height);
                a = i * 8;
                points[a + 0] = x;
                points[a + 1] = height;
                points[a + 2] = x;
                points[a + 3] = -height;
                points[a + 4] = x + step;
                points[a + 5] = -height;
                points[a + 6] = x + step;
                points[a + 7] = height;
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, pointsBufferId);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(points.Length * 4),
                points,
                BufferUsageHint.StaticDraw);

            GL.VertexPointer(2, VertexPointerType.Float, 8, 0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);

            GL.DrawElements(PrimitiveType.Quads, indexes.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);

            SwapBuffers();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1.0f, 64.0f);

            GL.MatrixMode(MatrixMode.Projection);

            GL.LoadMatrix(ref projection);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if ((DateTime.Now - last_click_time).TotalMilliseconds < 500)
            {
                Fullscreen = !Fullscreen;
                last_click_time = DateTime.Now.AddSeconds(-10);
            }
            else
            {
                last_click_time = DateTime.Now;
            }
        }
    }
}
