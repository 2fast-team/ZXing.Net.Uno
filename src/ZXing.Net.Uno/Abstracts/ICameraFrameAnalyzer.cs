using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXing.Net.Uno
{
    public interface ICameraFrameAnalyzer
    {
        void FrameReady(CameraFrameBufferEventArgs args);
    }
}
