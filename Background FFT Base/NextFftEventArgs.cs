using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Background_FFT.Base
{
    public class NextFftEventArgs : EventArgs
    {
        private int m_delay;

        public int Delay
        {
            get { return m_delay; }
            private set { m_delay = value; }
        }

        private int m_samplerate;

        public int SampleRate
        {
            get { return m_samplerate; }
            private set { m_samplerate = value; }
        }

        private double[] m_fftData;

        public double[] FftData
        {
            get { return m_fftData; }
            private set { m_fftData = value; }
        }

        private float[] m_fftBuffer;

        public float[] FftBuffer
        {
            get { return m_fftBuffer; }
            private set { m_fftBuffer = value; }
        }

        private int m_fftLen;

        public int FftLength
        {
            get { return m_fftLen; }
            private set { m_fftLen = value; }
        }

        public int ResultLength
        {
            get { return m_fftLen / 2; }
        }

        private int m_bufferLength;

        public int BufferLength
        {
            get { return m_bufferLength; }
            private set { m_bufferLength = value; }
        }

        private int m_bufferms;

        public int BufferMilliseconds
        {
            get { return m_bufferms; }
            private set { m_bufferms = value; }
        }

        private double m_volume;

        public double Volume
        {
            get { return m_volume; }
            private set { m_volume = value; }
        }

        public NextFftEventArgs(int delay, int samplerate, double[] fftData, float[] fftBuffer, int fftLen, int bufferLength, int bufferms, double volume)
        {
            m_delay = delay;
            m_samplerate = samplerate;
            m_fftData = fftData;
            m_fftBuffer = fftBuffer;
            m_fftLen = fftLen;
            m_bufferLength = bufferLength;
            m_bufferms = bufferms;
            m_volume = volume;
        }

        public double FrequencyToIndex(double hz)
        {
            return hz * m_fftLen / m_samplerate;
        }

        public double IndexToFrequency(int i)
        {
            return (double)i * m_samplerate / m_fftLen;
        }
    }
}
