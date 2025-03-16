using System.ComponentModel;
using CommunityToolkit.Uno.Core;
using CommunityToolkit.Uno.Core.Primitives;
using Windows.Foundation;
using ZXing;
using ZXing.Net.Uno;
using ZXing.Net.Uno.Readers;

namespace CommunityToolkit.Uno.Camera.Controls;

[TemplatePart(Name = MainGridName, Type = typeof(Grid))]
public sealed partial class CameraBarcodeReaderControl : Control, ICameraBarcodeReaderView
{
    private const string MainGridName = "MainGrid";
    Grid MainGrid;
    CameraManager CameraManager { get; }
    ZXing.Net.Uno.Readers.IBarcodeReader barcodeReader;

    public event EventHandler<BarcodeDetectionEventArgs> BarcodesDetected;
    public event EventHandler<CameraFrameBufferEventArgs> FrameReady;
    public CameraBarcodeReaderControl()
	{
		this.DefaultStyleKey = typeof(CameraBarcodeReaderControl);
        CameraManager = new CameraManager(this, CameraProvider, () =>
        {
            //CameraManager.ConnectCamera(CancellationToken.None);
        }, true);

        
    }

    private void CameraManager_FrameReady(object? sender, CameraFrameBufferEventArgs e)
    {
        FrameReady?.Invoke(this, e);

        if (IsDetecting)
        {
            var barcodes = BarcodeReader.Decode(e.Data);

            if (barcodes?.Any() ?? false)
                BarcodesDetected?.Invoke(this,new BarcodeDetectionEventArgs(barcodes));
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        MainGrid = (Grid)GetTemplateChild(MainGridName);
        StartCamara();
    }

    private async Task StartCamara()
    {
        var previewView = CameraManager.CreatePlatformView();
        MainGrid.Children.Add(previewView);

        await CameraManager.UpdateCaptureResolution(MainGrid.DesiredSize, CancellationToken.None);
        await CameraManager.ConnectCamera(CancellationToken.None);

        CameraManager.FrameReady += CameraManager_FrameReady;
    }

    /// <summary>
    /// Gets a value indicating whether the camera feature is available on the current device.
    /// </summary>
    public bool IsAvailable
    {
        get => (bool)GetValue(IsAvailableProperty);
        set => SetValue(IsAvailableProperty, value);
    }

    static readonly DependencyProperty IsAvailableProperty =
        DependencyProperty.Register(
            nameof(IsAvailable),
            typeof(bool),
            typeof(CameraBarcodeReaderControl),
            new PropertyMetadata(CameraViewDefaults.IsAvailable));

    /// <summary>
    /// Backing <see cref="DependencyProperty"/> for the <see cref="CameraFlashMode"/> property.
    /// </summary>
    public static readonly DependencyProperty CameraFlashModeProperty =
        DependencyProperty.Register(
            nameof(CameraFlashMode),
            typeof(CameraFlashMode),
            typeof(CameraBarcodeReaderControl),
            new PropertyMetadata(CameraViewDefaults.CameraFlashMode));

    /// <summary>
    /// Gets or sets the <see cref="CameraFlashMode"/>.
    /// </summary>
    public CameraFlashMode CameraFlashMode
    {
        get => (CameraFlashMode)GetValue(CameraFlashModeProperty);
        set => SetValue(CameraFlashModeProperty, value);
    }

    /// <summary>
    /// Backing <see cref="DependencyProperty"/> for the <see cref="IsTorchOn"/> property.
    /// </summary>
    public static readonly DependencyProperty IsTorchOnProperty =
        DependencyProperty.Register(
            nameof(IsTorchOn),
            typeof(bool),
            typeof(CameraBarcodeReaderControl),
            new PropertyMetadata(CameraViewDefaults.IsTorchOn));

    /// <summary>
    /// Gets or sets a value indicating whether the torch (flash) is on.
    /// </summary>
    public bool IsTorchOn
    {
        get => (bool)GetValue(IsTorchOnProperty);
        set => SetValue(IsTorchOnProperty, value);
    }

    static readonly DependencyProperty IsCameraBusyProperty =
        DependencyProperty.Register(
            nameof(IsCameraBusy),
            typeof(bool),
            typeof(CameraBarcodeReaderControl),
            new PropertyMetadata(CameraViewDefaults.IsCameraBusy));

    /// <summary>
    /// Gets a value indicating whether the camera is currently busy.
    /// </summary>
    public bool IsCameraBusy
    {
        get => (bool)GetValue(IsCameraBusyProperty);
        private set => SetValue(IsCameraBusyProperty, value);
    }

    /// <summary>
    /// Backing <see cref="DependencyProperty"/> for the <see cref="SelectedCamera"/> property.
    /// </summary>
    public static readonly DependencyProperty? SelectedCameraProperty =
        DependencyProperty.Register(
            nameof(SelectedCamera),
            typeof(CameraInfo),
            typeof(CameraBarcodeReaderControl),
            new PropertyMetadata(null));

    /// <inheritdoc cref="ICameraView.SelectedCamera"/>
    public CameraInfo? SelectedCamera
    {
        get => (CameraInfo?)GetValue(SelectedCameraProperty);
        set => SetValue(SelectedCameraProperty, value);
    }

    /// <summary>
    /// Backing <see cref="DependencyProperty"/> for the <see cref="ZoomFactor"/> property.
    /// </summary>
    public static readonly DependencyProperty ZoomFactorProperty =
        DependencyProperty.Register(
            nameof(ZoomFactor),
            typeof(float),
            typeof(CameraBarcodeReaderControl),
            new PropertyMetadata(CameraViewDefaults.ZoomFactor));

    /// <inheritdoc cref="ICameraView.ZoomFactor"/>
    public float ZoomFactor
    {
        get => (float)GetValue(ZoomFactorProperty);
        set => SetValue(ZoomFactorProperty, value);
    }

    /// <summary>
    /// Backing <see cref="DependencyProperty"/> for the <see cref="ImageCaptureResolution"/> property.
    /// </summary>
    public static readonly DependencyProperty ImageCaptureResolutionProperty =
        DependencyProperty.Register(
            nameof(ImageCaptureResolution),
            typeof(Size),
            typeof(CameraBarcodeReaderControl),
            new PropertyMetadata(CameraViewDefaults.ImageCaptureResolution));

    /// <inheritdoc cref="ICameraView.ImageCaptureResolution"/>
    public Size ImageCaptureResolution
    {
        get => (Size)GetValue(ImageCaptureResolutionProperty);
        set => SetValue(ImageCaptureResolutionProperty, value);
    }

    public static readonly DependencyProperty OptionsProperty =
        DependencyProperty.Register(
            nameof(Options), 
            typeof(BarcodeReaderOptions), 
            typeof(CameraBarcodeReaderControl),
            new PropertyMetadata(new BarcodeReaderOptions()));

    public BarcodeReaderOptions Options
    {
        get => (BarcodeReaderOptions)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public static readonly DependencyProperty IsDetectingProperty =
        DependencyProperty.Register(
            nameof(IsDetecting), 
            typeof(bool), 
            typeof(CameraBarcodeReaderControl),
            new PropertyMetadata(true));

    public bool IsDetecting
    {
        get => (bool)GetValue(IsDetectingProperty);
        set => SetValue(IsDetectingProperty, value);
    }

    //static ICameraProvider CameraProvider => Application.Current?.Services.GetRequiredService<ICameraProvider>() ?? throw new CameraException("Unable to retrieve CameraProvider");
    static ICameraProvider CameraProvider { get; } = new CameraProvider();

    protected ZXing.Net.Uno.Readers.IBarcodeReader BarcodeReader
    => barcodeReader ??= new BarcodeReader();

    TaskCompletionSource handlerCompletedTCS = new();

    [EditorBrowsable(EditorBrowsableState.Never)]
    TaskCompletionSource IAsynchronousHandler.HandlerCompleteTCS => handlerCompletedTCS;

    [EditorBrowsable(EditorBrowsableState.Never)]
    bool ICameraView.IsBusy
    {
        get => IsCameraBusy;
        set => SetValue(IsCameraBusyProperty, value);
    }

    void ICameraBarcodeReaderView.BarcodesDetected(BarcodeDetectionEventArgs e) => BarcodesDetected?.Invoke(this, e);
    void ICameraFrameAnalyzer.FrameReady(CameraFrameBufferEventArgs e) => FrameReady?.Invoke(this, e);

    CameraFlashMode ICameraView.CameraFlashMode => throw new NotImplementedException();

    Size ICameraView.ImageCaptureResolution => throw new NotImplementedException();

    public void OnMediaCaptured(Stream imageData)
    {
        throw new NotImplementedException();
    }

    public void OnMediaCapturedFailed(string failureReason)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="ICameraView.CaptureImage"/>
    public async ValueTask CaptureImage(CancellationToken token)
    {
        handlerCompletedTCS.TrySetCanceled(token);

        handlerCompletedTCS = new();
        //Handler?.Invoke(nameof(ICameraView.CaptureImage));

        await handlerCompletedTCS.Task.WaitAsync(token);
    }

    /// <inheritdoc cref="ICameraView.StartCameraPreview"/>
    public async ValueTask StartCameraPreview(CancellationToken token)
    {
        handlerCompletedTCS.TrySetCanceled(token);

        handlerCompletedTCS = new();

        await CameraManager.StartCameraPreview(token);
        //Handler?.Invoke(nameof(ICameraView.StartCameraPreview));

        await handlerCompletedTCS.Task.WaitAsync(token);
    }

    /// <inheritdoc cref="ICameraView.StopCameraPreview"/>
    public void StopCameraPreview()
    {
        CameraManager.StopCameraPreview();
        //Handler?.Invoke(nameof(ICameraView.StopCameraPreview));
    }

    /// <inheritdoc cref="ICameraView.GetAvailableCameras"/>
    public async ValueTask<IReadOnlyList<CameraInfo>> GetAvailableCameras(CancellationToken token)
    {
        if (CameraProvider.AvailableCameras is null)
        {
            await CameraProvider.RefreshAvailableCameras(token);

            if (CameraProvider.AvailableCameras is null)
            {
                throw new CameraException("Unable to refresh available cameras");
            }
        }

        return CameraProvider.AvailableCameras;
    }

    ///// <summary>
    ///// Event that is raised when the camera capture fails.
    ///// </summary>
    //public event EventHandler<MediaCaptureFailedEventArgs> MediaCaptureFailed
    //{
    //    add => weakEventManager.AddEventHandler(value);
    //    remove => weakEventManager.RemoveEventHandler(value);
    //}

    ///// <summary>
    ///// Event that is raised when the camera captures an image.
    ///// </summary>
    ///// <remarks>
    ///// The <see cref="MediaCapturedEventArgs"/> contains the captured image data.
    ///// </remarks>
    //public event EventHandler<MediaCapturedEventArgs> MediaCaptured
    //{
    //    add => weakEventManager.AddEventHandler(value);
    //    remove => weakEventManager.RemoveEventHandler(value);
    //}
}
