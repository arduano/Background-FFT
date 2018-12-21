using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FancyVisualizer
{
    public class Settings
    {
        public int Bars = 500;
        public int Duplicates = 4;
        public int SmoothWidth = 5;
        public double BarAveragingSample = 2;
        public int SpinSpeed = 1;
        public int StartFrequency = 1;
        public int EndFrequency = 2000;
        public double DecoyBarsDecaySpeed = .001;
        public double BassThreshold = .3;
        public double BassBrightnessMultiplier = 1;
        public double BassHueChangeMultiplier = 1;
        public bool Mirror = false;
        public double TargetParticleCount = 100;
        public double ParticleSize = .01;
        public double ParticleSpeed = .01;
        public double ParticleTrailMinVolume = 1;
    }
}
