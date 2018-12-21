using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Utils;
using NAudio.Dsp;
using NAudio;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using Background_FFT.Base;

namespace Background_FFT.Base
{
    public delegate void NextFftEventHandler(object sender, NextFftEventArgs e);

    public class Worker : IDisposable
    {
        IWaveIn capture = new WasapiLoopbackCapture();

        Thread bgProcess;
        bool Running = false;

        public bool Paused;

        int fftLength = 2048 * 2;
        byte[] fftData;

        double fftBufferMaxLen = 1;
        List<float> fftBuffer = new List<float>();

        int updatesPerSecond = 90;

        double Volume = 0;

        public event NextFftEventHandler NextFftEvent;

        double incompleteBuffersRatio = 0;
        int incompleteBuffersSample = 100;
        double incompleteBuffersTargetRatio = 0.3;

        public Worker(int ups = 90)
        {
            updatesPerSecond = ups;
            bgProcess = new Thread(new ThreadStart(this.ProcessLoop));
        }

        public void Start()
        {
            fftBufferMaxLen = fftLength;
            capture.DataAvailable += DataAvailable;
            capture.StartRecording();
            Running = true;
            bgProcess.Start();
        }

        private void ProcessLoop()
        {
            while (Running)
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();

                double[] fftResult = new double[fftLength / 2];
                float[] fftBufferPart = new float[fftLength];
                double volume = 0;

                bool running = false;
                bool incomplete_buffer = false;

                if (fftBuffer.Count > 0 && !Paused)
                {
                    if (fftBuffer.Count < fftLength)
                    {
                        incompleteBuffersRatio = (incompleteBuffersRatio * incompleteBuffersSample + 1) / (incompleteBuffersSample + 1);
                        incomplete_buffer = true;
                    }
                    else
                    {
                        incompleteBuffersRatio = incompleteBuffersRatio * incompleteBuffersSample / (incompleteBuffersSample + 1);
                    }
                    while (fftBuffer.Count < fftLength)
                    {
                        fftBuffer.Add(0);
                    }
                    try
                    {
                        fftBufferPart = fftBuffer.GetRange(0, fftLength).ToArray();
                        fftBuffer.RemoveRange(0, capture.WaveFormat.SampleRate / updatesPerSecond);
                        double sum = 0;
                        for (int i = 0; i < fftLength; i++)
                        {
                            sum  += fftBufferPart[i];
                        }
                        if(sum == 0) goto Skip;
                        if (sum != 0)
                        {
                            if (incompleteBuffersRatio > incompleteBuffersTargetRatio * 2)
                                fftBufferMaxLen += 1;
                            if (incompleteBuffersRatio < incompleteBuffersTargetRatio / 2)
                                fftBufferMaxLen -= 1;
                        }
                        Complex[] FftInput = new Complex[fftLength];
                        for (int i = 0; i < fftLength; i++)
                        {
                            FftInput[i].X = (float)(fftBufferPart[i] * FastFourierTransform.HammingWindow(i, fftLength));
                            FftInput[i].Y = 0;
                        }
                        FastFourierTransform.FFT(true, (int)Math.Log(fftLength, 2.0), FftInput);
                        Complex[] fftResultBuffer = new Complex[fftLength];
                        for (int i = 0; i < fftLength / 2; i++) fftResult[i] = Math.Abs(FftInput[i].X);
                        volume = fftResult.Sum();
                        running = true;
                    }
                    catch
                    {
                    }
                    Skip:;
                }


                int avg_sample = updatesPerSecond / 20;
                Volume = (Volume * avg_sample + volume) / (avg_sample + 1);
                if (!running) Volume = 0;


                int bufferlen = fftBuffer.Count + capture.WaveFormat.SampleRate / updatesPerSecond - fftLength;

                int ElapsedTime = (int)timer.ElapsedMilliseconds;
                var args = new NextFftEventArgs(ElapsedTime, capture.WaveFormat.SampleRate, fftResult, fftBufferPart, fftLength, bufferlen, (int)(1000.0 / capture.WaveFormat.SampleRate * bufferlen), Volume);

                NextFftEvent(this, args);

                if (10000000.0 / updatesPerSecond - (int)timer.ElapsedTicks > 0) Thread.Sleep(TimeSpan.FromTicks((long)Math.Max(0, 10000000.0 / updatesPerSecond - timer.ElapsedTicks)));
                timer.Stop();
            }
        }

        private void DataAvailable(object sender, WaveInEventArgs e)
        {

            for (int i = 0; i < e.BytesRecorded; i += capture.WaveFormat.BlockAlign)
            {
                float left = BitConverter.ToSingle(e.Buffer, i);
                float right = BitConverter.ToSingle(e.Buffer, i + 4);
                fftBuffer.Add((left + right) / 2);
            }
            if (fftBuffer.Count > fftBufferMaxLen)
            {
                fftBuffer.RemoveRange(0, fftBuffer.Count - (int)fftBufferMaxLen);
            }
        }

        public void Dispose()
        {
            capture.Dispose();
            Running = false;
        }
    }
}
