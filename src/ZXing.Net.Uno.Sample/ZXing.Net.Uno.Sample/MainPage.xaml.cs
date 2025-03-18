using ZXing.Net.Uno.Sample.ViewModels;

namespace ZXing.Net.Uno.Sample;

public sealed partial class MainPage : Page
{

    public MainPageViewModel ViewModel { get; } = new MainPageViewModel();
    public MainPage()
    {
        this.InitializeComponent();
    }

    private void CameraBarcodeReaderControl_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {

#if DEBUG
        Console.WriteLine(e.Results.FirstOrDefault().Value);
#endif

        ViewModel.WriteQRCodeResultToString(e.Results.FirstOrDefault().Value);
    }
}
