#if IOS || MACCATALYST
using AVFoundation;
using CommunityToolkit.Uno.Core.Primitives;
using CommunityToolkit.Uno.Extensions;
using CoreFoundation;
using CoreMedia;
using CoreVideo;
using Foundation;
using ObjCRuntime;
using System.Diagnostics;
using UIKit;
using Windows.Foundation;
using ZXing.Net.Uno;

namespace CommunityToolkit.Uno.Core;


partial class CameraManager
{
    // TODO: Check if we really need this
    readonly NSDictionary<NSString, NSObject> codecSettings = new([AVVideo.CodecKey], [new NSString("jpeg")]);
    AVCaptureDeviceInput? _audioInput;
    AVCaptureSession? _captureSession;
    AVCapturePhotoOutput? _photoOutput;
    AVCaptureInput? _captureInput;
    AVCaptureDevice? _captureDevice;
    AVCaptureVideoDataOutput? _videoDataOutput;
    CaptureDelegate _captureDelegate;
    DispatchQueue _dispatchQueue;

    AVCaptureSession? captureSession;

    AVCaptureFlashMode flashMode;

    IDisposable? orientationDidChangeObserver;
    AVCapturePhotoOutput? photoOutput;
    PreviewView? previewView;

    AVCaptureDeviceInput? videoInput;
    AVCaptureVideoOrientation videoOrientation;
    AVCaptureMovieFileOutput? videoOutput;
    string? videoRecordingFileName;
    TaskCompletionSource? videoRecordingFinalizeTcs;
    Stream? videoRecordingStream;

    /// <inheritdoc />
    public void Dispose()
    {
        CleanupVideoRecordingResources();

        captureSession?.StopRunning();
        captureSession?.Dispose();
        captureSession = null;

        _captureInput?.Dispose();
        _captureInput = null;

        _captureDevice = null;

        orientationDidChangeObserver?.Dispose();
        orientationDidChangeObserver = null;

        photoOutput?.Dispose();
        photoOutput = null;

        previewView?.Dispose();
        previewView = null;

        videoRecordingStream?.Dispose();
        videoRecordingStream = null;
    }

    public NativePlatformCameraPreviewView CreatePlatformView()
    {
        captureSession = new AVCaptureSession
        {
            SessionPreset = AVCaptureSession.PresetPhoto
        };

        previewView = new PreviewView
        {
            Session = captureSession
        };

        orientationDidChangeObserver = UIDevice.Notifications.ObserveOrientationDidChange((_, _) => UpdateVideoOrientation());
        UpdateVideoOrientation();

        return previewView;
    }

    public partial void UpdateFlashMode(CameraFlashMode flashMode)
    {
        this.flashMode = flashMode.ToPlatform();
    }

    public partial void UpdateZoom(float zoomLevel)
    {
        if (!IsInitialized || _captureDevice is null)
        {
            return;
        }

        if (zoomLevel < (float)_captureDevice.MinAvailableVideoZoomFactor || zoomLevel > (float)_captureDevice.MaxAvailableVideoZoomFactor)
        {
            return;
        }

        _captureDevice.LockForConfiguration(out NSError? error);
        if (error is not null)
        {
            Trace.WriteLine(error);
            return;
        }

        _captureDevice.VideoZoomFactor = zoomLevel;
        _captureDevice.UnlockForConfiguration();
    }

    public partial ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token)
    {
        if (cameraView.SelectedCamera is null)
        {
            throw new CameraException($"Unable to update Capture Resolution because {nameof(ICameraControl)}.{nameof(ICameraControl.SelectedCamera)} is null.");
        }

        if (_captureDevice is null)
        {
            return ValueTask.CompletedTask;
        }

        _captureDevice.LockForConfiguration(out NSError? error);
        if (error is not null)
        {
            Trace.WriteLine(error);
            return ValueTask.CompletedTask;
        }

        var formatsMatchingResolution = cameraView.SelectedCamera.SupportedFormats
            .Where(format => MatchesResolution(format, resolution))
            .ToList();

        var availableFormats = formatsMatchingResolution.Count is not 0
            ? formatsMatchingResolution
            : GetPhotoCompatibleFormats(cameraView.SelectedCamera.SupportedFormats);

        var selectedFormat = availableFormats
            .OrderByDescending(f => f.ResolutionArea)
            .FirstOrDefault();

        if (selectedFormat is not null)
        {
            _captureDevice.ActiveFormat = selectedFormat;
        }

        _captureDevice.UnlockForConfiguration();
        return ValueTask.CompletedTask;
    }

    private async partial Task PlatformConnectCamera(CancellationToken token)
    {
        await PlatformStartCameraPreview(token);
    }

    private async partial Task PlatformStartCameraPreview(CancellationToken token)
    {
        if (captureSession is null)
        {
            return;
        }

        captureSession.BeginConfiguration();

        foreach (var input in captureSession.Inputs)
        {
            captureSession.RemoveInput(input);
            input.Dispose();
        }

        cameraView.SelectedCamera ??= cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");

        _captureDevice = cameraView.SelectedCamera.CaptureDevice ?? throw new CameraException($"No Camera found");
        _captureInput = new AVCaptureDeviceInput(_captureDevice, out _);
        captureSession.AddInput(_captureInput);

        if (photoOutput is null)
        {
            photoOutput = new AVCapturePhotoOutput();
            captureSession.AddOutput(photoOutput);
        }

        if (AnalyseImages)
        {
            if (_videoDataOutput == null)
            {
                _videoDataOutput = new AVCaptureVideoDataOutput();

                var videoSettings = NSDictionary.FromObjectAndKey(
                    new NSNumber((int)CVPixelFormatType.CV32BGRA),
                    CVPixelBuffer.PixelFormatTypeKey);

                _videoDataOutput.WeakVideoSettings = videoSettings;

                if (_captureDelegate == null)
                {
                    _captureDelegate = new CaptureDelegate
                    {
                        SampleProcessor = cvPixelBuffer =>
                        {
                            // analyse only every _vidioFrameDivider value
                            if (_videoFrameCounter % VidioFrameDivider == 0)
                            {
                                FrameReady?.Invoke(this, new CameraFrameBufferEventArgs(new ZXing.Net.Uno.Readers.PixelBufferHolder
                                {
                                    Data = cvPixelBuffer,
                                    Size = new Size(cvPixelBuffer.Width, cvPixelBuffer.Height)
                                }));
                            }
                            _videoFrameCounter++;
                        }
                    };
                }

                if (_dispatchQueue == null)
                    _dispatchQueue = new DispatchQueue("CameraBufferQueue");

                _videoDataOutput.AlwaysDiscardsLateVideoFrames = true;
                _videoDataOutput.SetSampleBufferDelegate(_captureDelegate, _dispatchQueue);
            }

            _captureSession.AddOutput(_videoDataOutput);
        }


        await UpdateCaptureResolution(cameraView.ImageCaptureResolution, token);

        captureSession.CommitConfiguration();
        captureSession.StartRunning();
        IsInitialized = true;
        OnLoaded.Invoke();
    }

    private partial void PlatformStopCameraPreview()
    {
        if (captureSession is null)
        {
            return;
        }

        if (captureSession.Running)
        {
            captureSession.StopRunning();
        }

        IsInitialized = false;
    }

    private partial void PlatformDisconnect()
    {
    }

    private async partial Task PlatformStartVideoRecording(Stream stream, CancellationToken token)
    {
        var isPermissionGranted = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video).WaitAsync(token);
        if (!isPermissionGranted)
        {
            throw new CameraException("Camera permission is not granted. Please enable it in the app settings.");
        }

        if (captureSession is null)
        {
            throw new CameraException("Capture session is not initialized. Call ConnectCamera first.");
        }

        CleanupVideoRecordingResources();

        var videoDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video) ?? throw new CameraException("Unable to get video device");

        videoInput = new AVCaptureDeviceInput(videoDevice, out NSError? error);
        if (error is not null)
        {
            throw new CameraException($"Error creating video input: {error.LocalizedDescription}");
        }

        if (!captureSession.CanAddInput(videoInput))
        {
            videoInput?.Dispose();
            throw new CameraException("Unable to add video input to capture session.");
        }

        captureSession.BeginConfiguration();
        captureSession.AddInput(videoInput);

        try
        {
            var audioDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Audio);
            if (audioDevice is not null)
            {
                _audioInput = new AVCaptureDeviceInput(audioDevice, out NSError? audioError);
                if (audioError is null && captureSession.CanAddInput(_audioInput))
                {
                    captureSession.AddInput(_audioInput);
                }
                else
                {
                    _audioInput?.Dispose();
                    _audioInput = null;
                }
            }
        }
        catch
        {
            // Ignore audio configuration issues; proceed with video-only recording
        }

        videoOutput = new AVCaptureMovieFileOutput();

        if (!captureSession.CanAddOutput(videoOutput))
        {
            captureSession.RemoveInput(videoInput);
            if (_audioInput is not null)
            {
                captureSession.RemoveInput(_audioInput);
                _audioInput.Dispose();
                _audioInput = null;
            }

            videoInput?.Dispose();
            videoOutput?.Dispose();
            captureSession.CommitConfiguration();
            throw new CameraException("Unable to add video output to capture session.");
        }

        captureSession.AddOutput(videoOutput);
        captureSession.CommitConfiguration();

        videoRecordingStream = stream;
        videoRecordingFinalizeTcs = new TaskCompletionSource();
        videoRecordingFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mov");

        var outputUrl = NSUrl.FromFilename(videoRecordingFileName);
        videoOutput.StartRecordingToOutputFile(outputUrl, new AVCaptureMovieFileOutputRecordingDelegate(videoRecordingFinalizeTcs));
    }

    private async partial Task<Stream> PlatformStopVideoRecording(CancellationToken token)
    {
        if (captureSession is null
            || videoRecordingFileName is null
            || videoInput is null
            || videoOutput is null
            || videoRecordingStream is null
            || videoRecordingFinalizeTcs is null)
        {
            return Stream.Null;
        }

        videoOutput.StopRecording();
        await videoRecordingFinalizeTcs.Task.WaitAsync(token);

        if (File.Exists(videoRecordingFileName))
        {
            await using var inputStream = new FileStream(videoRecordingFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            await inputStream.CopyToAsync(videoRecordingStream, token);
            await videoRecordingStream.FlushAsync(token);
            if (videoRecordingStream.CanSeek)
            {
                videoRecordingStream.Position = 0;
            }
        }

        CleanupVideoRecordingResources();

        return videoRecordingStream;
    }

    void CleanupVideoRecordingResources()
    {
        if (captureSession is not null)
        {
            captureSession.BeginConfiguration();

            foreach (var input in captureSession.Inputs)
            {
                captureSession.RemoveInput(input);
                input.Dispose();
            }

            foreach (var output in captureSession.Outputs)
            {
                captureSession.RemoveOutput(output);
                output.Dispose();
            }

            // Restore to photo preset for preview after video recording
            captureSession.SessionPreset = AVCaptureSession.PresetPhoto;
            captureSession.CommitConfiguration();
        }

        videoOutput = null;
        videoInput = null;
        _audioInput = null;

        // Clean up temporary file
        if (videoRecordingFileName is not null)
        {
            if (File.Exists(videoRecordingFileName))
            {
                File.Delete(videoRecordingFileName);
            }

            videoRecordingFileName = null;
        }

        videoRecordingFinalizeTcs = null;
    }

    private async partial ValueTask PlatformTakePicture(CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(photoOutput);

        var capturePhotoSettings = AVCapturePhotoSettings.FromFormat(codecSettings);
        capturePhotoSettings.FlashMode = photoOutput.SupportedFlashModes.Contains(flashMode) ? flashMode : photoOutput.SupportedFlashModes.First();

        if (AVMediaTypes.Video.GetConstant() is NSString avMediaTypeVideo)
        {
            var photoOutputConnection = photoOutput.ConnectionFromMediaType(avMediaTypeVideo);
            if (photoOutputConnection is not null)
            {
                photoOutputConnection.VideoOrientation = videoOrientation;
            }
        }

        var wrapper = new AVCapturePhotoCaptureDelegateWrapper();

        photoOutput.CapturePhoto(capturePhotoSettings, wrapper);

        var result = await wrapper.Task.WaitAsync(token);
        if (result.Error is not null)
        {
            var failureReason = result.Error.LocalizedDescription;
            if (!string.IsNullOrEmpty(result.Error.LocalizedFailureReason))
            {
                failureReason = $"{failureReason} - {result.Error.LocalizedFailureReason}";
            }

            cameraView.OnMediaCapturedFailed(failureReason);
            return;
        }

        Stream? imageData;
        try
        {
            imageData = result.Photo.FileDataRepresentation?.AsStream();
        }
        catch (Exception e)
        {
            // possible exception: ObjCException NSInvalidArgumentException NSAllocateMemoryPages(...) failed in AVCapturePhoto.get_FileDataRepresentation()
            cameraView.OnMediaCapturedFailed($"Unable to retrieve the file data representation from the captured result: {e.Message}");
            return;
        }

        if (imageData is null)
        {
            cameraView.OnMediaCapturedFailed("Unable to retrieve the file data representation from the captured result.");
        }
        else
        {
            cameraView.OnMediaCaptured(imageData);
        }
    }

    static AVCaptureVideoOrientation GetVideoOrientation()
    {
        IEnumerable<UIScene> scenes = UIApplication.SharedApplication.ConnectedScenes;

        UIInterfaceOrientation interfaceOrientation;
        if (!(OperatingSystem.IsMacCatalystVersionAtLeast(26) || OperatingSystem.IsIOSVersionAtLeast(26)))
        {
            interfaceOrientation = scenes.FirstOrDefault() is UIWindowScene windowScene
                ? windowScene.InterfaceOrientation
                : UIApplication.SharedApplication.StatusBarOrientation;
        }
        else
        {
            interfaceOrientation = scenes.FirstOrDefault() is UIWindowScene windowScene
                ? windowScene.EffectiveGeometry.InterfaceOrientation
                : UIApplication.SharedApplication.StatusBarOrientation;
        }

        return interfaceOrientation switch
        {
            UIInterfaceOrientation.Portrait => AVCaptureVideoOrientation.Portrait,
            UIInterfaceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
            UIInterfaceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeRight,
            UIInterfaceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeLeft,
            _ => AVCaptureVideoOrientation.Portrait
        };
    }

    void UpdateVideoOrientation()
    {
        videoOrientation = GetVideoOrientation();
        previewView?.UpdatePreviewVideoOrientation(videoOrientation);
    }

    IEnumerable<AVCaptureDeviceFormat> GetPhotoCompatibleFormats(IEnumerable<AVCaptureDeviceFormat> formats)
    {
        if (photoOutput is not null)
        {
            var photoPixelFormats = photoOutput.GetSupportedPhotoPixelFormatTypesForFileType(nameof(AVFileTypes.Jpeg));
            return formats.Where(format => photoPixelFormats.Contains((NSNumber)format.FormatDescription.MediaSubType));
        }

        return formats;
    }

    static bool MatchesResolution(AVCaptureDeviceFormat format, Size resolution)
    {
        var dimensions = ((CMVideoFormatDescription)format.FormatDescription).Dimensions;
        return dimensions.Width <= resolution.Width
               && dimensions.Height <= resolution.Height;
    }

    sealed class AVCapturePhotoCaptureDelegateWrapper : AVCapturePhotoCaptureDelegate
    {
        readonly TaskCompletionSource<CapturePhotoResult> taskCompletionSource = new();

        public Task<CapturePhotoResult> Task =>
            taskCompletionSource.Task;

        public override void DidFinishProcessingPhoto(AVCapturePhotoOutput output, AVCapturePhoto photo, NSError? error)
        {
            taskCompletionSource.TrySetResult(new()
            {
                Output = output,
                Photo = photo,
                Error = error
            });
        }
    }

    sealed record CapturePhotoResult
    {
        public required AVCapturePhotoOutput Output { get; init; }

        public required AVCapturePhoto Photo { get; init; }

        public NSError? Error { get; init; }
    }

    sealed partial class PreviewView : NativePlatformCameraPreviewView
    {
        public PreviewView()
        {
            PreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
        }

        public AVCaptureSession? Session
        {
            get => PreviewLayer.Session;
            set => PreviewLayer.Session = value;
        }

        AVCaptureVideoPreviewLayer PreviewLayer => (AVCaptureVideoPreviewLayer)Layer;

        [Export("layerClass")]
        public static Class GetLayerClass()
        {
            return new Class(typeof(AVCaptureVideoPreviewLayer));
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            UpdatePreviewVideoOrientation(GetVideoOrientation());
        }

        public void UpdatePreviewVideoOrientation(AVCaptureVideoOrientation videoOrientation)
        {
            if (PreviewLayer.Connection is not null)
            {
#if IOS || MACCATALYST
#if (IOS && !MACCATALYST)
                // iOS 17+ uses VideoRotationAngle, older uses VideoOrientation
                if (OperatingSystem.IsIOSVersionAtLeast(17, 0))
                {
                    PreviewLayer.Connection.VideoRotationAngle = videoOrientation switch
                    {
                        AVCaptureVideoOrientation.Portrait => 0f,
                        AVCaptureVideoOrientation.LandscapeRight => 90f,
                        AVCaptureVideoOrientation.PortraitUpsideDown => 180f,
                        AVCaptureVideoOrientation.LandscapeLeft => 270f,
                        _ => 0f
                    };
                }
                else
                {
#pragma warning disable CA1422 // Suppress obsolete warning for older iOS
                    PreviewLayer.Connection.VideoOrientation = videoOrientation;
#pragma warning restore CA1422
                }
#else
				// MacCatalyst 17+ uses VideoRotationAngle, older uses VideoOrientation
				if (OperatingSystem.IsMacCatalystVersionAtLeast(17, 0))
				{
					PreviewLayer.Connection.VideoRotationAngle = videoOrientation switch
					{
						AVCaptureVideoOrientation.Portrait => 0f,
						AVCaptureVideoOrientation.LandscapeRight => 90f,
						AVCaptureVideoOrientation.PortraitUpsideDown => 180f,
						AVCaptureVideoOrientation.LandscapeLeft => 270f,
						_ => 0f
					};
				}
				else
				{
#pragma warning disable CA1422 // Suppress obsolete warning for older MacCatalyst
					PreviewLayer.Connection.VideoOrientation = videoOrientation;
#pragma warning restore CA1422
				}
#endif
#endif
            }
        }
    }
}

class AVCaptureMovieFileOutputRecordingDelegate(TaskCompletionSource taskCompletionSource) : AVCaptureFileOutputRecordingDelegate
{
    public override void FinishedRecording(AVCaptureFileOutput captureOutput, NSUrl outputFileUrl, NSObject[] connections, NSError? error)
    {
        taskCompletionSource.SetResult();
    }
}
#endif