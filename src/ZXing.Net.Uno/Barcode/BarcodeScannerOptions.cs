namespace ZXing.Net.Uno;

public record BarcodeReaderOptions
{
	public bool AutoRotate { get; init; }

	public bool TryHarder { get; init; } = true;

	public bool TryInverted { get; init; }

	public BarcodeFormat Formats { get; init; }

	public bool Multiple { get; init; }

	public bool UseCode39ExtendedMode { get; init; }
}
