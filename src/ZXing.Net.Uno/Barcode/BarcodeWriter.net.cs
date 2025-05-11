#if !__IOS__ && !__ANDROID__ && !__MACOS__ && !WINDOWS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace ZXing.Net.Uno.Barcode
{
    public class BarcodeWriter : BarcodeWriter<NativePlatformImage>
    {
        public Color ForegroundColor { get; set; }

        public Color BackgroundColor { get; set; }
    }
}
#endif