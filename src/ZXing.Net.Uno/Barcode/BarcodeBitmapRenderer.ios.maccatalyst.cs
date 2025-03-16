using System;
using ZXing.Rendering;
using ZXing.Common;
using System.Threading;
using Windows.UI;

#if IOS || MACCATALYST
using Foundation;
using CoreFoundation;
using CoreGraphics;
using UIKit;

namespace ZXing.Net.Uno
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

	internal class BarcodeBitmapRenderer : IBarcodeRenderer<UIImage>
	{
		public CGColor ForegroundColor { get; set; } = new CGColor(1.0f, 1.0f, 1.0f);
		public CGColor BackgroundColor { get; set; } = new CGColor(0f, 0f, 0f);

		public UIImage Render(BitMatrix matrix, ZXing.BarcodeFormat format, string content)
			=> Render(matrix, format, content, new EncodingOptions());

		public UIImage Render(BitMatrix matrix, ZXing.BarcodeFormat format, string content, EncodingOptions options)
		{
			var renderer = new UIGraphicsImageRenderer(new CGSize(matrix.Width, matrix.Height), new UIGraphicsImageRendererFormat {
				Opaque = false,
				Scale = UIScreen.MainScreen.Scale
			});

			var waiter = new ManualResetEvent(false);
			UIImage image = null!;
			
			renderer.CreateImage(context =>
			{
				var black = new CGColor(0f, 0f, 0f);
				var white = new CGColor(1.0f, 1.0f, 1.0f);
				
				for (var x = 0; x < matrix.Width; x++)
				{
					for (var y = 0; y < matrix.Height; y++)
					{
						context.CGContext.SetFillColor(matrix[x, y] ? black : white);
						context.CGContext.FillRect(new CGRect(x, y, 1, 1));
					}
				}
				
				SetImage(context.CurrentImage);
			});

			waiter.WaitOne();
			return image;
			
			void SetImage(UIImage img)
			{
				image = img;
				waiter.Set();
			}
		}
	}
}
#endif
