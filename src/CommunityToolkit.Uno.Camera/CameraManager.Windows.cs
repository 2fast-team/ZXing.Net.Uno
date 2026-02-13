#if WINDOWS
using System.Runtime.Versioning;
using CommunityToolkit.Uno.Core.Primitives;
using CommunityToolkit.Uno.Extensions;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using ZXing.Net.Uno;

namespace CommunityToolkit.Uno.Core;

[SupportedOSPlatform("windows10.0.18362.0")]
partial class CameraManager
{
    MediaPlayerElement? _mediaElement;
    MediaCapture? _mediaCapture;
    MediaFrameSource? _frameSource;
    LowLagMediaRecording? _mediaRecording;
    MediaFrameReader? _mediaFrameReader;
    Stream? _videoCaptureStream;

    public MediaPlayerElement CreatePlatformView()
    {
        _mediaElement = new MediaPlayerElement();
        return _mediaElement;
    }

    public void Dispose()
    {
        PlatformStopCameraPreview();
        _mediaCapture?.Dispose();
    }

    public partial void UpdateFlashMode(CameraFlashMode flashMode)
    {
        if (!IsInitialized || (_mediaCapture?.VideoDeviceController.FlashControl.Supported is false))
        {
            return;
        }

        if (_mediaCapture is null)
        {
            return;
        }

        var (updatedFlashControlEnabled, updatedFlashControlAuto) = flashMode switch
        {
            CameraFlashMode.Off => (false, (bool?)null),
            CameraFlashMode.On => (true, false),
            CameraFlashMode.Auto => (true, true),
            _ => throw new NotSupportedException($"{flashMode} is not yet supported")
        };

        _mediaCapture.VideoDeviceController.FlashControl.Enabled = updatedFlashControlEnabled;

        if (updatedFlashControlAuto.HasValue)
        {
            _mediaCapture.VideoDeviceController.FlashControl.Auto = updatedFlashControlAuto.Value;
        }

    }

    public partial void UpdateZoom(float zoomLevel)
    {
        if (!IsInitialized || _mediaCapture is null || !_mediaCapture.VideoDeviceController.ZoomControl.Supported)
        {
            return;
        }

        var step = _mediaCapture.VideoDeviceController.ZoomControl.Step;

        if (zoomLevel % step != 0)
        {
            zoomLevel = (float)Math.Ceiling(zoomLevel / step) * step;
        }

        _mediaCapture.VideoDeviceController.ZoomControl.Value = zoomLevel;
    }

    public async partial ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token)
    {
        await PlatformUpdateResolution(resolution, token);
    }

    private partial void PlatformDisconnect()
    {
    }

    private async partial ValueTask PlatformTakePicture(CancellationToken token)
    {
        if (_mediaCapture is null)
        {
            return;
        }

        token.ThrowIfCancellationRequested();

        MemoryStream memoryStream = new();

        try
        {
            await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), memoryStream.AsRandomAccessStream());

            memoryStream.Position = 0;

            cameraView.OnMediaCaptured(memoryStream);
        }
        catch (Exception ex)
        {
            cameraView.OnMediaCapturedFailed(ex.Message);
            throw;
        }
    }

    private async partial Task PlatformConnectCamera(CancellationToken token)
    {
        await StartCameraPreview(token);
    }

    private async partial Task PlatformStartCameraPreview(CancellationToken token)
    {
        if (_mediaElement is null)
        {
            return;
        }

        cameraView.SelectedCamera ??= cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");

        _mediaCapture = new MediaCapture();

        await _mediaCapture.InitializeCameraForCameraView(cameraView.SelectedCamera.DeviceId, token);

        _frameSource = _mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;


        if (AnalyseImages && _frameSource is not null)
        {
            _mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(_frameSource, MediaEncodingSubtypes.Bgra8);
            _mediaFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
            await _mediaFrameReader.StartAsync();
        }

        IsInitialized = true;

        await PlatformUpdateResolution(cameraView.ImageCaptureResolution, token);

        OnLoaded.Invoke();
    }

    /// <summary>
	/// Event handler for the ColorFrameReader.FrameArrived event. This is where the image processing occurs.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="args"></param>
    private async void ColorFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        // analyse only every _vidioFrameDivider value
        if (_videoFrameCounter % VidioFrameDivider == 0)
        {
            var mediaFrameReference = sender.TryAcquireLatestFrame();
            var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
            var direct3DSurface = videoMediaFrame?.Direct3DSurface;
            SoftwareBitmap softwareBitmap;

            if (direct3DSurface != null)
            {
                softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(direct3DSurface);

                if (softwareBitmap != null)
                {
                    // Convert to Bgra8 Premultiplied softwareBitmap.
                    if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                        softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                    {
                        softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                    }

                    // Send bitmap to BarCodeReaderView/CameraView
                    FrameReady?.Invoke(this, new CameraFrameBufferEventArgs(
                        new ZXing.Net.Uno.Readers.PixelBufferHolder
                        {
                            Data = softwareBitmap,
                            Size = new Size(softwareBitmap.PixelWidth, softwareBitmap.PixelHeight)
                        }));

                    // Swap the processed frame to _backBuffer and dispose of the unused image.
                    // softwareBitmap = Interlocked.Exchange(ref _backBuffer, softwareBitmap);
                    softwareBitmap?.Dispose();
                    direct3DSurface?.Dispose();
                }
            }
            mediaFrameReference?.Dispose();
        }
        _videoFrameCounter++;
    }

    private partial void PlatformStopCameraPreview()
    {
        if (_mediaElement is null)
        {
            return;
        }

        _mediaElement.Source = null;
        _mediaCapture?.Dispose();

        _mediaCapture = null;
        IsInitialized = false;
    }

    async Task PlatformUpdateResolution(Size resolution, CancellationToken token)
    {
        if (!IsInitialized || _mediaCapture is null)
        {
            return;
        }

        if (cameraView.SelectedCamera is null)
        {
            throw new CameraException($"Unable to update Capture Resolution because {nameof(ICameraControl)}.{nameof(ICameraControl.SelectedCamera)} is null.");
        }

        var filteredPropertiesList = cameraView.SelectedCamera.ImageEncodingProperties.Where(p => p.Width <= resolution.Width && p.Height <= resolution.Height).ToList();

        if (filteredPropertiesList.Count is 0)
        {
            filteredPropertiesList = [.. cameraView.SelectedCamera.ImageEncodingProperties.OrderByDescending(p => p.Width * p.Height)];
        }

        if (filteredPropertiesList.Count is not 0)
        {
            await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, filteredPropertiesList.First()).AsTask(token);
        }
    }

    private async partial Task PlatformStartVideoRecording(Stream stream, CancellationToken token)
    {
        if (!IsInitialized || _mediaCapture is null || _mediaElement is null)
        {
            return;
        }

        _videoCaptureStream = stream;

        var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);
        _mediaRecording = await _mediaCapture.PrepareLowLagRecordToStreamAsync(profile, stream.AsRandomAccessStream());

        _frameSource = _mediaCapture.FrameSources
            .FirstOrDefault(static source => source.Value.Info.MediaStreamType is MediaStreamType.VideoRecord && source.Value.Info.SourceKind is MediaFrameSourceKind.Color)
            .Value;

        if (_frameSource is not null)
        {
            var frameFormat = _frameSource.SupportedFormats
                    .OrderByDescending(f => f.VideoFormat.Width * f.VideoFormat.Height)
                    .FirstOrDefault();

            if (frameFormat is not null)
            {
                await _frameSource.SetFormatAsync(frameFormat);
                _mediaElement.AutoPlay = true;
                _mediaElement.Source = MediaSource.CreateFromMediaFrameSource(_frameSource);
                await _mediaRecording.StartAsync();
            }
        }
    }

    private async partial Task<Stream> PlatformStopVideoRecording(CancellationToken token)
    {
        if (!IsInitialized || _mediaElement is null || _mediaRecording is null || _videoCaptureStream is null)
        {
            return Stream.Null;
        }

        await _mediaRecording.StopAsync();
        return _videoCaptureStream;
    }
}
#endif