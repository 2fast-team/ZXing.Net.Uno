namespace ZXing.Net.Uno.Sample.ViewModels;

public class MainPageViewModel : ObservableObject
{
    private bool _isDetecting = true;

    public bool IsDetecting 
    { 
        get => _isDetecting; 
        set => SetProperty(ref _isDetecting, value);
    }

    private string _qRCodeResult = string.Empty;
    public string QRCodeResult 
    { 
        get => _qRCodeResult;
        set => SetProperty(ref _qRCodeResult, value);
    }

}
