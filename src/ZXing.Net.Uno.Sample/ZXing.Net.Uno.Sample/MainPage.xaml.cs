namespace ZXing.Net.Uno.Sample;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }

    private void CameraBarcodeReaderControl_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {

    }
}
