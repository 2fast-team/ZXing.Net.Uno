using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXing.Net.Uno.Sample.ViewModels;

public class MainPageViewModel : ObservableObject
{
    private string _qRCodeResult = string.Empty;
    public string QRCodeResult 
    { 
        get => _qRCodeResult;
        set => SetProperty(ref _qRCodeResult, value);
    }

    public void WriteQRCodeResultToString(string result)
    {
        QRCodeResult = result;
    }
}
