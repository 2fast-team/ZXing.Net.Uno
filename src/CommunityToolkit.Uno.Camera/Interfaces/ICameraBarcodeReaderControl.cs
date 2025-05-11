using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing.Net.Uno;

namespace CommunityToolkit.Uno.Core;

public interface ICameraBarcodeReaderControl : ICameraControl, ICameraFrameAnalyzer
{
    BarcodeReaderOptions Options { get; }

    void BarcodesDetected(BarcodeDetectionEventArgs args);

    bool IsDetecting { get; set; }
}

