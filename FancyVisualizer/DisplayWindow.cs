using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using Background_FFT.Base;
using System.Drawing;
using OpenTK.Input;
using System.Threading;
using System.Diagnostics;
using SharpFont;

namespace FancyVisualizer
{
    public class DisplayWindow : GameWindow
    {
        View view;

        Settings setting;

        public NextFftEventArgs currentState;

        bool fullscreen = false;

        Particle[] particles;
        List<Particle[]> particlesHistory = new List<Particle[]>();

        Random rand = new Random();

        float baseHeight = .25f;

        bool menuSelected = false;
        double menuSelectedAnimation = 0;
        double menuHoverAnimation = 0;

        bool leftDown = false;
        bool leftClicked = false;

        Slider bassSlider = new Slider(new Vector2(-0.35f, 0), new Vector2(-0.80f, 0), 0.1, 3, 1);
        Slider volumeSlider = new Slider(new Vector2(0.35f, 0), new Vector2(0.80f, 0), 1, 20, 1);

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
        int colorBufferId;
        int indexBufferId;

        double[] points;
        double[] points2;
        double[] points3;
        uint[] indexes;
        float[] colors;
        float[] colors2;
        float[] colors3;

        double Bass = 1;

        double Hue = 0;

        int vals_offset = 0;

        bool Updated = false;

        DateTime lastUpdateTime;

        public DisplayWindow(int width, int height, Settings s)
            : base(width, height, new GraphicsMode(new ColorFormat(8, 8, 8, 8), 8, 8, 4))
        {
            setting = s;
            view = new View(new Vector2(0, 0), 0, 2);
            view.hMirror = setting.Mirror;
            VSync = VSyncMode.On;
            particles = new Particle[(int)setting.TargetParticleCount];
            points = new double[(setting.Bars + 1) * 4 * setting.Duplicates];
            points2 = new double[(setting.Bars + 1) * 4 * setting.Duplicates];
            points3 = new double[(setting.Bars + 1) * 2 * setting.Duplicates];
            indexes = new uint[(setting.Bars + 1) * 2 * setting.Duplicates];
            colors = new float[(setting.Bars + 1) * 8 * setting.Duplicates];
            colors2 = new float[(setting.Bars + 1) * 8 * setting.Duplicates];
            colors3 = new float[(setting.Bars + 1) * 4 * setting.Duplicates];

            for (uint i = 0; i < indexes.Length; i++) indexes[i] = i;

            GL.GenBuffers(1, out pointsBufferId);
            GL.BindBuffer(BufferTarget.ArrayBuffer, pointsBufferId);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(points.Length * 8),
                points,
                BufferUsageHint.StaticDraw);

            GL.GenBuffers(1, out colorBufferId);
            GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferId);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(colors.Length * 4),
                colors,
                BufferUsageHint.StaticDraw);

            GL.GenBuffers(1, out indexBufferId);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(indexes.Length * 4),
                indexes,
                BufferUsageHint.StaticDraw);


            double step = 1.0f / setting.Bars / setting.Duplicates;
            int a = 0;
            double x = 0;
            double xps = 0;

            double _hue = Hue - 30;
            if (_hue < 0) _hue += 360;

            Parallel.For(0, setting.Bars * setting.Duplicates, new Action<int>(i =>
            {
                x = step * i;
                xps = x + step;

                double angle1 = x * 2 * Math.PI;
                double angle2 = xps * 2 * Math.PI;

                double cos1 = Math.Cos(angle1);
                double sin1 = Math.Sin(angle1);
                double cos2 = Math.Cos(angle2);
                double sin2 = Math.Sin(angle2);

            }));

            lastUpdateTime = DateTime.Now;
        }

        double[] barLengths = new double[1];
        double[] decoyBarLengths = new double[1];

        double off_counter = 0;
        bool isOff = true;
        double centreFade = 1;

        double barVolume = 0;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        double SineInterepolate(double s, double a, double b)
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

        public void Update()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            double updateTimeScalar = ((double)(DateTime.Now - lastUpdateTime).Ticks / 10000.0) / (1000.0 / 60.0);
            lastUpdateTime = DateTime.Now;

            bool lmbd = Mouse.GetState().LeftButton == ButtonState.Pressed;
            leftClicked = false;
            if (!lmbd && leftDown) leftDown = false;
            if (!leftDown && lmbd)
            {
                leftDown = true;
                leftClicked = true;
            }

            #region Bar lengths and FFT
            var fftstate = (double[])currentState.FftData.Clone();
            for (int i = 0; i < fftstate.Length; i++) fftstate[i] *= volumeSlider.CurrVal;
            int bars = setting.Bars;
            if (bars != barLengths.Length)
            {
                barLengths = new double[bars];
                decoyBarLengths = new double[bars];
            }

            var vals = new double[bars];
            double starthz = setting.StartFrequency;
            double endhz = setting.EndFrequency;
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

            var trebble = fftstate.Skip(500).Sum();
            var smoothness = 1 / (trebble * trebble * 3 + Bass) * 5;
            if (smoothness > 25) smoothness = 25;
            vals = PartialInverseInterpolate(vals, 30, smoothness);
            for (int i = bars - 1; i >= 0; i--)
            {
                //double sum = vals[i];
                //for (int j = 0; j < setting.SmoothWidth; j++)
                //{
                //    int _j = (i - j - 1) % bars;
                //    if (_j < 0) _j += bars;
                //    sum += vals[_j];
                //}
                //for (int j = 0; j < setting.SmoothWidth; j++) sum += vals[(i + j + 1) % bars];
                //vals[i] = sum / (setting.SmoothWidth * 2 + 1);
                double sum = decoyBarLengths[i];
                for (int j = 0; j < setting.SmoothWidth; j++)
                {
                    int _j = (i - j - 1) % bars;
                    if (_j < 0) _j += bars;
                    sum += decoyBarLengths[_j];
                }
                for (int j = 0; j < setting.SmoothWidth; j++) sum += decoyBarLengths[(i + j + 1) % bars];
                decoyBarLengths[i] = sum / (setting.SmoothWidth * 2 + 1);
            }

            vals_offset += (int)((setting.SpinSpeed + Bass * 2.5 * setting.SpinSpeed) * updateTimeScalar);
            vals_offset = vals_offset % vals.Length;

            Parallel.For(0, bars, new Action<int>(i =>
            {
                int _i = (i + vals_offset) % vals.Length;
                barLengths[i] = (barLengths[i] * setting.BarAveragingSample + vals[_i]) / (setting.BarAveragingSample + 1);
                if (vals[_i] > barLengths[i]) barLengths[i] = vals[_i];

                decoyBarLengths[i] -= setting.DecoyBarsDecaySpeed * updateTimeScalar;
                if (decoyBarLengths[i] < barLengths[i]) decoyBarLengths[i] = barLengths[i];
            }));
            #endregion

            barVolume = (barLengths.Sum() / bars * 15 + (currentState.Volume * volumeSlider.CurrVal) / 2) * bassSlider.CurrVal;
            double bass = Math.Max(0, barVolume - setting.BassThreshold);
            Bass -= .05;
            if (Bass < 0) Bass = 0;
            if (bass > Bass) Bass = bass;

            Hue += Bass * setting.BassHueChangeMultiplier;
            Hue = Hue % 360;

            #region Particles
            if (currentState.Volume != 0)
            {
                for (int a = 0; a < 2; a++)
                {
                    int i = 0;
                    for (; i < particles.Length; i++) if (!particles[i].exists) break;
                    if (i != particles.Length)
                        if (rand.NextDouble() < 1)
                        {
                            double ang = rand.NextDouble() * Math.PI * 2;
                            double cos = Math.Cos(ang);
                            double sin = Math.Sin(ang);
                            particles[i] = (new Particle(
                                new Vector2((float)cos, (float)sin) * baseHeight,
                                new Vector2((float)cos, (float)sin) * (float)setting.ParticleSpeed,
                                (float)setting.ParticleSize));
                        }
                }

                for (int i = 0; i < particles.Length; i++)
                {
                    if (!particles[i].exists) continue;
                    particles[i].Step((float)((barVolume + Bass) * updateTimeScalar));
                    if (!view.bounds.Contains(particles[i].location.X / 1.1f, particles[i].location.Y / 1.1f))
                    {
                        particles[i].exists = false;
                    }
                }
            }

            particlesHistory.Add((Particle[])particles.Clone());
            int maxParticles = (int)Math.Max(Math.Floor((barVolume - setting.ParticleTrailMinVolume) / (setting.ParticleTrailMinVolume / 3)), 0) + 1;
            if (particlesHistory.Count > maxParticles) particlesHistory.RemoveAt(0);
            if (particlesHistory.Count > maxParticles) particlesHistory.RemoveAt(0);
            #endregion

            #region Centre Fade
            if (isOff)
            {
                centreFade = 0;
                if (currentState.Volume != 0) isOff = false;
            }
            else
            {
                if (currentState.Volume == 0)
                {
                    off_counter = Math.Min(off_counter + 0.005 * updateTimeScalar, 1);
                    if (off_counter >= 1)
                    {
                        centreFade -= 0.005 * updateTimeScalar;
                        if (centreFade < 0)
                        {
                            centreFade = 0;
                            isOff = true;
                            off_counter = 0;
                        }
                    }
                }
                else
                {
                    centreFade = Math.Min(centreFade + 0.05 * updateTimeScalar, 1);
                }
            }
            #endregion

            double mx;
            double my;
            if (this.Width > this.Height)
            {
                mx = (Mouse.X - (double)this.Width / 2) / this.Height;
                my = (Mouse.Y - (double)this.Height / 2) / this.Height;
            }
            else
            {
                mx = (Mouse.X - (double)this.Width / 2) / this.Width;
                my = (Mouse.Y - (double)this.Height / 2) / this.Width;
            }

            if (bassSlider.dragging)
            {
                if (!leftDown) bassSlider.dragging = false;
                else
                {
                    bassSlider.setDrag(mx, my);
                }
            }
            else if (volumeSlider.dragging)
            {
                if (!leftDown) volumeSlider.dragging = false;
                else
                {
                    volumeSlider.setDrag(mx, my);
                }
            }
            else
            {
                #region Centre Click
                var mxc = mx / (1.9 + Bass / 10) * 2;
                var myc = my / (1.9 + Bass / 10) * 2;
                if (Math.Sqrt(mxc * mxc + myc * myc) < baseHeight)
                {
                    menuHoverAnimation += 0.2;
                    if (menuHoverAnimation > 1) menuHoverAnimation = 1;
                    if (leftClicked) menuSelected = !menuSelected;
                }
                else
                {
                    menuHoverAnimation -= 0.2;
                    if (menuHoverAnimation < 0) menuHoverAnimation = 0;
                }

                if (menuSelected)
                {
                    menuSelectedAnimation += 0.05;
                    if (menuSelectedAnimation > 1) menuSelectedAnimation = 1;
                }
                else
                {
                    menuSelectedAnimation -= 0.05;
                    if (menuSelectedAnimation < 0) menuSelectedAnimation = 0;
                }
                #endregion

                bassSlider.hovered = false;
                if (bassSlider.isInHead(mx, my) && menuSelectedAnimation != 0)
                {
                    bassSlider.hovered = true;
                    if (leftClicked) bassSlider.startDrag(mx, my);
                }
                volumeSlider.hovered = false;
                if (volumeSlider.isInHead(mx, my) && menuSelectedAnimation != 0)
                {
                    volumeSlider.hovered = true;
                    if (leftClicked) volumeSlider.startDrag(mx, my);
                }
            }
            Updated = true;
        }

        public void Render()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Stopwatch s = new Stopwatch();
            List<long> times = new List<long>();

            var _barLengths = (double[])barLengths.Clone();
            var _decoyBarLengths = (double[])decoyBarLengths.Clone();

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.LoadIdentity();

            {
                #region Particles
                view.zoom = 2;
                view.ApplyTransform(ClientRectangle.Width, ClientRectangle.Height);

                if (centreFade != 0)
                {
                    GL.Begin(PrimitiveType.Quads);
                    double particleLength = Math.Max(Bass / 30, setting.ParticleSize);
                    var ptcles = (Particle[])particles.Clone();

                    foreach (var p in ptcles)
                    {
                        if (!p.exists) continue;
                        GL.Color4(.5, .5, .5, p.opacity * centreFade);
                        Vector2 dir = p.velocity.Normalized();
                        Vector2 start = p.location + Vector2.Multiply(dir, (float)particleLength / 2);
                        Vector2 end = start - Vector2.Multiply(dir, (float)particleLength);
                        GL.Vertex2(start + dir.PerpendicularLeft * p.size / 2);
                        GL.Vertex2(start + dir.PerpendicularRight * p.size / 2);
                        GL.Vertex2(end + dir.PerpendicularRight * p.size / 2);
                        GL.Vertex2(end + dir.PerpendicularLeft * p.size / 2);
                    }
                    GL.End();
                }
                #endregion
            }

            times.Add(s.ElapsedMilliseconds);
            s.Restart();

            {
                #region Sliders
                var opacity = menuSelectedAnimation;
                var slider = bassSlider;
                GL.Begin(PrimitiveType.Quads);
                GL.Color4(50.0 / 255, 50.0 / 255, 50.0 / 255, opacity);
                var dir = (slider.End - slider.Start).Normalized() * slider.lineSize;
                GL.Vertex2(slider.Start - dir + dir.PerpendicularLeft);
                GL.Vertex2(slider.Start - dir + dir.PerpendicularRight);
                GL.Vertex2(slider.End + dir + dir.PerpendicularRight);
                GL.Vertex2(slider.End + dir + dir.PerpendicularLeft);
                GL.End();
                dir = (slider.End - slider.Start).Normalized() * slider.headSize;
                GL.Begin(PrimitiveType.Quads);
                var pos = slider.Start + (slider.End - slider.Start) * (float)slider.HeadPos;
                GL.Color4(80.0 / 255, 80.0 / 255, 80.0 / 255, opacity);
                GL.Vertex2(pos - dir + dir.PerpendicularLeft);
                GL.Vertex2(pos - dir + dir.PerpendicularRight);
                GL.Vertex2(pos + dir + dir.PerpendicularRight);
                GL.Vertex2(pos + dir + dir.PerpendicularLeft);
                dir *= 0.8f;
                if (!slider.hovered)
                {
                    GL.Color4(50.0 / 255, 50.0 / 255, 50.0 / 255, opacity);
                    GL.Vertex2(pos - dir + dir.PerpendicularLeft);
                    GL.Vertex2(pos - dir + dir.PerpendicularRight);
                    GL.Vertex2(pos + dir + dir.PerpendicularRight);
                    GL.Vertex2(pos + dir + dir.PerpendicularLeft);
                }
                GL.End();

                slider = volumeSlider;
                GL.Begin(PrimitiveType.Quads);
                GL.Color4(50.0 / 255, 50.0 / 255, 50.0 / 255, opacity);
                dir = (slider.End - slider.Start).Normalized() * slider.lineSize;
                GL.Vertex2(slider.Start - dir + dir.PerpendicularLeft);
                GL.Vertex2(slider.Start - dir + dir.PerpendicularRight);
                GL.Vertex2(slider.End + dir + dir.PerpendicularRight);
                GL.Vertex2(slider.End + dir + dir.PerpendicularLeft);
                GL.End();
                dir = (slider.End - slider.Start).Normalized() * slider.headSize;
                GL.Begin(PrimitiveType.Quads);
                pos = slider.Start + (slider.End - slider.Start) * (float)slider.HeadPos;
                GL.Color4(80.0 / 255, 80.0 / 255, 80.0 / 255, opacity);
                GL.Vertex2(pos - dir + dir.PerpendicularLeft);
                GL.Vertex2(pos - dir + dir.PerpendicularRight);
                GL.Vertex2(pos + dir + dir.PerpendicularRight);
                GL.Vertex2(pos + dir + dir.PerpendicularLeft);
                dir *= 0.8f;
                if (!slider.hovered)
                {
                    GL.Color4(50.0 / 255, 50.0 / 255, 50.0 / 255, opacity);
                    GL.Vertex2(pos - dir + dir.PerpendicularLeft);
                    GL.Vertex2(pos - dir + dir.PerpendicularRight);
                    GL.Vertex2(pos + dir + dir.PerpendicularRight);
                    GL.Vertex2(pos + dir + dir.PerpendicularLeft);
                }
                GL.End();

                #endregion
            }

            times.Add(s.ElapsedMilliseconds);
            s.Restart();

            {
                #region Circles
                view.zoom = 1.9 + Bass / 10;
                view.ApplyTransform(ClientRectangle.Width, ClientRectangle.Height);

                double step = 1.0f / _barLengths.Length / setting.Duplicates;

                double innerHeight = baseHeight - .1;
                double middleHeight = baseHeight - .003;

                Color outer_color = Color.LightGray;
                Color middle_color = Color.DimGray;
                Color inner_color = Color.Black;

                if (menuSelectedAnimation > 0)
                {
                    outer_color = ColorInterpolate(outer_color, Color.LightGray, menuSelectedAnimation);
                    middle_color = ColorInterpolate(middle_color, Color.DimGray, menuSelectedAnimation);
                    inner_color = ColorInterpolate(inner_color, Color.FromArgb(20, 20, 20), menuSelectedAnimation);
                }

                if (menuHoverAnimation > 0)
                {
                    outer_color = ColorInterpolate(outer_color, Color.LightGray, menuHoverAnimation);
                    middle_color = ColorInterpolate(middle_color, Color.DimGray, menuHoverAnimation);
                    inner_color = ColorInterpolate(inner_color, Color.FromArgb(10, 10, 10), menuHoverAnimation);
                }

                float outer_r = outer_color.R / 255.0f;
                float outer_g = outer_color.G / 255.0f;
                float outer_b = outer_color.B / 255.0f;
                float middle_r = middle_color.R / 255.0f;
                float middle_g = middle_color.G / 255.0f;
                float middle_b = middle_color.B / 255.0f;
                float inner_r = inner_color.R / 255.0f;
                float inner_g = inner_color.G / 255.0f;
                float inner_b = inner_color.B / 255.0f;

                int max = _decoyBarLengths.Length * setting.Duplicates;
                Parallel.For(0, max + 1, new Action<int>(i =>
                {
                //if (i == max) i = 0;
                int _i = i % _decoyBarLengths.Length;
                    double height = _decoyBarLengths[_i] + baseHeight;
                    double x = step * i;
                    double xps = x + step;

                    double angle1 = x * 2 * Math.PI;
                    double angle2 = xps * 2 * Math.PI;

                    double cos1 = Math.Cos(angle1);
                    double sin1 = Math.Sin(angle1);
                    double cos2 = Math.Cos(angle2);
                    double sin2 = Math.Sin(angle2);

                    int a = i * 4;

                    points[a + 0] = cos1 * baseHeight;
                    points[a + 1] = sin1 * baseHeight;
                    points[a + 2] = cos1 * height;
                    points[a + 3] = sin1 * height;

                    a = i * 8;

                    double _hue = (angle1 / (float)Math.PI * 180 - (float)Hue + 60) % 360;
                    if (_hue < 0) _hue += 360;
                    Color c = ColorFromHsb((float)_hue, 1, .5f);

                    for (int j = 0; j < 2; j++)
                    {
                        colors[a + j * 4 + 0] = c.R / 255.0f;
                        colors[a + j * 4 + 1] = c.G / 255.0f;
                        colors[a + j * 4 + 2] = c.B / 255.0f;
                        colors[a + j * 4 + 3] = .5f;
                    }

                    a = i * 4;

                    points2[a + 0] = cos1 * baseHeight;
                    points2[a + 1] = sin1 * baseHeight;
                    points2[a + 2] = cos1 * middleHeight;
                    points2[a + 3] = sin1 * middleHeight;

                    a = i * 8;

                    colors2[a + 0] = outer_r;
                    colors2[a + 1] = outer_g;
                    colors2[a + 2] = outer_b;
                    colors2[a + 3] = (float)centreFade;
                    colors2[a + 4] = middle_r;
                    colors2[a + 5] = middle_r;
                    colors2[a + 6] = middle_r;
                    colors2[a + 7] = (float)centreFade;
                }));

                times.Add(s.ElapsedMilliseconds);
                s.Restart();

                GL.BindBuffer(BufferTarget.ArrayBuffer, pointsBufferId);
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    (IntPtr)(points.Length * 8),
                    points,
                    BufferUsageHint.StaticDraw);
                GL.VertexPointer(2, VertexPointerType.Double, 16, 0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferId);
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    (IntPtr)(colors.Length * 4),
                    colors,
                    BufferUsageHint.StaticDraw);
                GL.ColorPointer(4, ColorPointerType.Float, 16, 0);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
                GL.DrawElements(PrimitiveType.QuadStrip, indexes.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);

                if (centreFade != 0)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, pointsBufferId);
                    GL.BufferData(
                        BufferTarget.ArrayBuffer,
                        (IntPtr)(points2.Length * 8),
                        points2,
                        BufferUsageHint.StaticDraw);
                    GL.VertexPointer(2, VertexPointerType.Double, 16, 0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferId);
                    GL.BufferData(
                        BufferTarget.ArrayBuffer,
                        (IntPtr)(colors2.Length * 4),
                        colors2,
                        BufferUsageHint.StaticDraw);
                    GL.ColorPointer(4, ColorPointerType.Float, 16, 0);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
                    GL.DrawElements(PrimitiveType.QuadStrip, indexes.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);
                }

                times.Add(s.ElapsedMilliseconds);
                s.Restart();

                double maxLength = _barLengths.Max();

                double _base_height = baseHeight - Bass / 20;
                _base_height = baseHeight;

                max = barLengths.Length * setting.Duplicates;
                Parallel.For(0, max + 1, new Action<int>(i =>
                {
                //if (i == max) i = 0;
                int _i = i % barLengths.Length;
                    double height = barLengths[_i] + baseHeight;
                    double x = step * i;
                    double xps = x + step;

                    double angle1 = x * 2 * Math.PI;
                    double angle2 = xps * 2 * Math.PI;

                    double cos1 = Math.Cos(angle1);
                    double sin1 = Math.Sin(angle1);
                    double cos2 = Math.Cos(angle2);
                    double sin2 = Math.Sin(angle2);

                    int a = i * 4;

                    points[a + 0] = cos1 * _base_height;
                    points[a + 1] = sin1 * _base_height;
                    points[a + 2] = cos1 * height;
                    points[a + 3] = sin1 * height;

                    a = i * 8;

                    double _hue = (angle1 / (float)Math.PI * 180 - (float)Hue) % 360;
                    if (_hue < 0) _hue += 360;
                    Color c = ColorFromHsb((float)_hue, 1, .5f);

                    for (int j = 0; j < 2; j++)
                    {
                        colors[a + j * 4 + 0] = c.R / 255.0f;
                        colors[a + j * 4 + 1] = c.G / 255.0f;
                        colors[a + j * 4 + 2] = c.B / 255.0f;
                        colors[a + j * 4 + 3] = 1;
                    }

                    a = i * 4;

                    points2[a + 0] = cos1 * middleHeight;
                    points2[a + 1] = sin1 * middleHeight;
                    points2[a + 2] = cos1 * innerHeight;
                    points2[a + 3] = sin1 * innerHeight;

                    a = i * 2;

                    points3[a + 0] = cos1 * innerHeight;
                    points3[a + 1] = sin1 * innerHeight;

                    a = i * 8;

                    colors2[a + 0] = middle_r;
                    colors2[a + 1] = middle_g;
                    colors2[a + 2] = middle_b;
                    colors2[a + 3] = (float)centreFade;
                    colors2[a + 4] = inner_r;
                    colors2[a + 5] = inner_r;
                    colors2[a + 6] = inner_r;
                    colors2[a + 7] = (float)centreFade;

                    a = i * 4;

                    colors3[a + 0] = inner_r;
                    colors3[a + 1] = inner_r;
                    colors3[a + 2] = inner_r;
                    colors3[a + 3] = (float)centreFade;
                }));

                times.Add(s.ElapsedMilliseconds);
                s.Restart();

                GL.BindBuffer(BufferTarget.ArrayBuffer, pointsBufferId);
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    (IntPtr)(points.Length * 8),
                    points,
                    BufferUsageHint.StaticDraw);
                GL.VertexPointer(2, VertexPointerType.Double, 16, 0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferId);
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    (IntPtr)(colors.Length * 4),
                    colors,
                    BufferUsageHint.StaticDraw);
                GL.ColorPointer(4, ColorPointerType.Float, 16, 0);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
                GL.DrawElements(PrimitiveType.QuadStrip, indexes.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);

                if (centreFade != 0)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, pointsBufferId);
                    GL.BufferData(
                        BufferTarget.ArrayBuffer,
                        (IntPtr)(points2.Length * 8),
                        points2,
                        BufferUsageHint.StaticDraw);
                    GL.VertexPointer(2, VertexPointerType.Double, 16, 0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferId);
                    GL.BufferData(
                        BufferTarget.ArrayBuffer,
                        (IntPtr)(colors2.Length * 4),
                        colors2,
                        BufferUsageHint.StaticDraw);
                    GL.ColorPointer(4, ColorPointerType.Float, 16, 0);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
                    GL.DrawElements(PrimitiveType.QuadStrip, indexes.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);

                    GL.BindBuffer(BufferTarget.ArrayBuffer, pointsBufferId);
                    GL.BufferData(
                        BufferTarget.ArrayBuffer,
                        (IntPtr)(points3.Length * 8),
                        points3,
                        BufferUsageHint.StaticDraw);
                    GL.VertexPointer(2, VertexPointerType.Double, 16, 0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferId);
                    GL.BufferData(
                        BufferTarget.ArrayBuffer,
                        (IntPtr)(colors3.Length * 4),
                        colors3,
                        BufferUsageHint.StaticDraw);
                    GL.ColorPointer(4, ColorPointerType.Float, 16, 0);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
                    GL.DrawElements(PrimitiveType.Polygon, indexes.Length / 2, DrawElementsType.UnsignedInt, IntPtr.Zero);
                }
                #endregion
            }

            times.Add(s.ElapsedMilliseconds);
            s.Restart();

            SwapBuffers();

            times.Add(s.ElapsedMilliseconds);
            s.Restart();

            view.zoom = 2;
            view.ApplyTransform(ClientRectangle.Width, ClientRectangle.Height);
            string _s = "";
            foreach (long l in times) _s += l + "   ";
            Console.WriteLine(_s);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (currentState == null) return;

            //Update();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            Update();
            //SpinWait.SpinUntil(() => Updated);
            //Updated = false;
            Render();
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

        public static Color ColorInterpolate(Color c1, Color c2, double x)
        {
            int r = (int)(c1.R + (c2.R - c1.R) * x);
            int g = (int)(c1.G + (c2.G - c1.G) * x);
            int b = (int)(c1.B + (c2.B - c1.B) * x);
            int a = (int)(c1.A + (c2.A - c1.A) * x);
            return Color.FromArgb(a, r, g, b);
        }
    }
}
