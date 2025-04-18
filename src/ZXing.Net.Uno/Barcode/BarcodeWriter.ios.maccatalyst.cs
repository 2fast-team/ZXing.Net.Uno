#if IOS || MACCATALYST
using UIKit;
using Windows.UI;

namespace ZXing.Net.Uno.Barcode
{
    public class BarcodeWriter : BarcodeWriter<UIImage>, IBarcodeWriter
    {
        BarcodeBitmapRenderer bitmapRenderer;

        public BarcodeWriter()
            => Renderer = (bitmapRenderer = new BarcodeBitmapRenderer());

        public Color ForegroundColor
        {
            get => new UIColor(bitmapRenderer.ForegroundColor);
            set => bitmapRenderer.ForegroundColor = value;
        }

        public Color BackgroundColor
        {
            get => new UIColor(bitmapRenderer.BackgroundColor);
            set => bitmapRenderer.BackgroundColor = value;
        }
    }
}
#endif