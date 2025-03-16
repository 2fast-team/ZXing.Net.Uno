#if WINDOWS
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;
using ZXing.Common;

namespace ZXing.Net.Uno;

public class BarcodeWriter : BarcodeWriter<WriteableBitmap>, IBarcodeWriter
{
	WriteableBitmapRenderer bitmapRenderer;

	public BarcodeWriter()
		=> Renderer = (bitmapRenderer = new WriteableBitmapRenderer());

	public Color ForegroundColor
	{
		get => bitmapRenderer.Foreground;
		set => bitmapRenderer.Foreground = value;
	}

	public Color BackgroundColor
	{
		get => bitmapRenderer.Background;
		set => bitmapRenderer.Background = value;
	}
}
#endif