using System;
using ZXing.Net.Uno.Readers;

namespace ZXing.Net.Uno;

public class CameraFrameBufferEventArgs : EventArgs
{
	public CameraFrameBufferEventArgs(PixelBufferHolder pixelBufferHolder) : base()
		=> Data = pixelBufferHolder;

	public readonly PixelBufferHolder Data;
}
