namespace ZXing.Net.Uno;

public class BarcodeDetectionEventArgs : EventArgs
{
	public BarcodeDetectionEventArgs(BarcodeResult[] results)
		: base()
	{
		Results = results;
	}

	public BarcodeResult[] Results { get; private set; }
}
