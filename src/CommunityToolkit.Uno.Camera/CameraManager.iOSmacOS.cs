#if IOS || MACCATALYST
using System.Diagnostics;
using AVFoundation;
using CommunityToolkit.Uno.Core.Primitives;
using CommunityToolkit.Uno.Extensions;
using CoreFoundation;
using CoreMedia;
using CoreVideo;
using Foundation;
using UIKit;
using Windows.Foundation;
using ZXing.Net.Uno;

namespace CommunityToolkit.Uno.Core;

partial class CameraManager
{
	// TODO: Check if we really need this
	readonly NSDictionary<NSString, NSObject> codecSettings = new([AVVideo.CodecKey], [new NSString("jpeg")]);

	AVCaptureSession? _captureSession;
	AVCapturePhotoOutput? _photoOutput;
	AVCaptureInput? _captureInput;
	AVCaptureDevice? _captureDevice;
    AVCaptureVideoDataOutput? _videoDataOutput;
    CaptureDelegate _captureDelegate;
    DispatchQueue _dispatchQueue;

    AVCaptureFlashMode _flashMode;

	IDisposable? _orientationDidChangeObserver;
	PreviewView? _previewView;
	AVCaptureVideoOrientation _videoOrientation;

	// IN the future change the return type to be an alias
	public UIView CreatePlatformView()
	{
		_captureSession = new AVCaptureSession
		{
			SessionPreset = AVCaptureSession.PresetPhoto
		};

		_previewView = new PreviewView
		{
			Session = _captureSession
		};

		_orientationDidChangeObserver = UIDevice.Notifications.ObserveOrientationDidChange((_, _) => UpdateVideoOrientation());
		UpdateVideoOrientation();

		return _previewView;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public void UpdateFlashMode(CameraFlashMode flashMode)
	{
		this._flashMode = flashMode.ToPlatform();
	}

	public void UpdateZoom(float zoomLevel)
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

	public async ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token)
	{
		if (_captureDevice is null)
		{
			return;
		}

		_captureDevice.LockForConfiguration(out NSError? error);
		if (error is not null)
		{
			Trace.WriteLine(error);
			return;
		}

		if (cameraView.SelectedCamera is null)
		{
			await cameraProvider.RefreshAvailableCameras(token);
			cameraView.SelectedCamera = cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");
		}

		var filteredFormatList = cameraView.SelectedCamera.SupportedFormats.Where(f =>
		{
			var d = ((CMVideoFormatDescription)f.FormatDescription).Dimensions;
			return d.Width <= resolution.Width && d.Height <= resolution.Height;
		}).ToList();

		filteredFormatList = [.. (filteredFormatList.Count is not 0 ? filteredFormatList : cameraView.SelectedCamera.SupportedFormats)
			.OrderByDescending(f =>
			{
				var d = ((CMVideoFormatDescription)f.FormatDescription).Dimensions;
				return d.Width * d.Height;
			})];

		if (filteredFormatList.Count is not 0)
		{
			_captureDevice.ActiveFormat = filteredFormatList.First();
		}

		_captureDevice.UnlockForConfiguration();
	}

	protected virtual async Task PlatformConnectCamera(CancellationToken token)
	{
		if (cameraProvider.AvailableCameras is null)
		{
			await cameraProvider.RefreshAvailableCameras(token);

			if (cameraProvider.AvailableCameras is null)
			{
				throw new CameraException("Unable to refresh cameras");
			}
		}

		await PlatformStartCameraPreview(token);
	}

	protected virtual async Task PlatformStartCameraPreview(CancellationToken token)
	{
		if (_captureSession is null)
		{
			return;
		}

		_captureSession.BeginConfiguration();

		foreach (var input in _captureSession.Inputs)
		{
			_captureSession.RemoveInput(input);
			input.Dispose();
		}

		if (cameraView.SelectedCamera is null)
		{
			await cameraProvider.RefreshAvailableCameras(token);
			cameraView.SelectedCamera = cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");
		}

		_captureDevice = cameraView.SelectedCamera.CaptureDevice ?? throw new CameraException($"No Camera found");
		_captureInput = new AVCaptureDeviceInput(_captureDevice, out _);
		_captureSession.AddInput(_captureInput);

		if (_photoOutput is null)
		{
			_photoOutput = new AVCapturePhotoOutput();
			_captureSession.AddOutput(_photoOutput);
		}

		await UpdateCaptureResolution(cameraView.ImageCaptureResolution, token);

		_captureSession.CommitConfiguration();

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

		_captureSession.StartRunning();
		IsInitialized = true;
		OnLoaded.Invoke();
	}

	protected virtual void PlatformStopCameraPreview()
	{
		if (_captureSession is null)
		{
			return;
		}

		if (_captureSession.Running)
		{
			_captureSession.StopRunning();
		}

		IsInitialized = false;
	}

	protected virtual void PlatformDisconnect()
	{
		PlatformStopCameraPreview();
        Dispose();
	}

	protected virtual async ValueTask PlatformTakePicture(CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(_photoOutput);

		var capturePhotoSettings = AVCapturePhotoSettings.FromFormat(codecSettings);
		capturePhotoSettings.FlashMode = _photoOutput.SupportedFlashModes.Contains(_flashMode) ? _flashMode : _photoOutput.SupportedFlashModes.First();

		if (AVMediaTypes.Video.GetConstant() is NSString avMediaTypeVideo)
		{
			var photoOutputConnection = _photoOutput.ConnectionFromMediaType(avMediaTypeVideo);
			if (photoOutputConnection is not null)
			{
				photoOutputConnection.VideoOrientation = _videoOrientation;
			}
		}

		var wrapper = new AVCapturePhotoCaptureDelegateWrapper();

		_photoOutput.CapturePhoto(capturePhotoSettings, wrapper);

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

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			_captureSession?.StopRunning();
			_captureSession?.Dispose();
			_captureSession = null;

			_captureInput?.Dispose();
			_captureInput = null;

			_orientationDidChangeObserver?.Dispose();
			_orientationDidChangeObserver = null;

			_photoOutput?.Dispose();
			_photoOutput = null;
		}
	}

	static AVCaptureVideoOrientation GetVideoOrientation()
	{
		IEnumerable<UIScene> scenes = UIApplication.SharedApplication.ConnectedScenes;
		var interfaceOrientation = scenes.FirstOrDefault() is UIWindowScene windowScene
			? windowScene.InterfaceOrientation
			: UIApplication.SharedApplication.StatusBarOrientation;

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
		_videoOrientation = GetVideoOrientation();
		_previewView?.UpdatePreviewVideoOrientation(_videoOrientation);
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

	sealed partial class PreviewView : UIView
	{
		public PreviewView()
		{
			PreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
		}

		[Export("layerClass")]
		public static ObjCRuntime.Class GetLayerClass()
		{
			return new ObjCRuntime.Class(typeof(AVCaptureVideoPreviewLayer));
		}

		public AVCaptureSession? Session
		{
			get => PreviewLayer.Session;
			set => PreviewLayer.Session = value;
		}

		AVCaptureVideoPreviewLayer PreviewLayer => (AVCaptureVideoPreviewLayer)Layer;

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();
			UpdatePreviewVideoOrientation(GetVideoOrientation());
		}

		public void UpdatePreviewVideoOrientation(AVCaptureVideoOrientation videoOrientation)
		{
			if (PreviewLayer.Connection is not null)
			{
				PreviewLayer.Connection.VideoOrientation = videoOrientation;
			}
		}
	}
}
#endif