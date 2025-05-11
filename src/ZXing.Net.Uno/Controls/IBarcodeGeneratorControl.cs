using Windows.UI;

namespace ZXing.Net.Uno.Controls
{
    public interface IBarcodeGeneratorControl
    {
        BarcodeFormat Format { get; }

        string Value { get; }

        //Color ForegroundColor { get; }

        //Color BackgroundColor { get; }

        int BarcodeMargin { get; }
    }
}
