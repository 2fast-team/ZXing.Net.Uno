using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace ZXing.Net.Uno.Barcode
{
    public class BarcodeWriter : BarcodeWriter<Android.Graphics.Bitmap>, IBarcodeWriter
    {
        BarcodeBitmapRenderer bitmapRenderer;

        public BarcodeWriter()
            => Renderer = (bitmapRenderer = new BarcodeBitmapRenderer());

        public Color ForegroundColor
        {
            get => bitmapRenderer.ForegroundColor;
            set => bitmapRenderer.ForegroundColor = value;
        }

        public Color BackgroundColor
        {
            get => bitmapRenderer.BackgroundColor;
            set => bitmapRenderer.BackgroundColor = value;
        }
    }
}
