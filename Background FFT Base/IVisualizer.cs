using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Background_FFT.Base
{
    public interface IVisualizer
    {
        void TransferFftData(NextFftEventArgs e);
        void ShowSettings();
        Dictionary<string, string> Settings { get; }
        bool Enabled { get; set; }
    }
}
