﻿#if WINDOWS
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

[SupportedOSPlatform("windows10.0.17763.0")]
partial class CameraManager
{
	MediaPlayerElement? _mediaElement;
	MediaCapture? _mediaCapture;
	MediaFrameSource? _frameSource;
    MediaFrameReader? _mediaFrameReader;
    SoftwareBitmap _backBuffer;

    public MediaPlayerElement CreatePlatformView()
	{
		_mediaElement = new MediaPlayerElement();
		return _mediaElement;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public void UpdateFlashMode(CameraFlashMode flashMode)
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

	public void UpdateZoom(float zoomLevel)
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

	public async ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token)
	{
		await PlatformUpdateResolution(resolution, token);
	}

	protected virtual void PlatformDisconnect()
	{
		PlatformStopCameraPreview();
		Dispose();
    }

	protected virtual async ValueTask PlatformTakePicture(CancellationToken token)
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

	protected virtual void Dispose(bool disposing)
	{
		PlatformStopCameraPreview();
		if (disposing)
		{
			_mediaCapture?.Dispose();
		}
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

		await StartCameraPreview(token);
	}

	protected virtual async Task PlatformStartCameraPreview(CancellationToken token)
	{
		if (_mediaElement is null)
		{
			return;
		}

		_mediaCapture = new MediaCapture();

		if (cameraView.SelectedCamera is null)
		{
			await cameraProvider.RefreshAvailableCameras(token);
			cameraView.SelectedCamera = cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");
		}

		await _mediaCapture.InitializeCameraForCameraView(cameraView.SelectedCamera.DeviceId, token);

		_frameSource = _mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;

		if (_frameSource is not null)
		{
			_mediaElement.AutoPlay = true;
			_mediaElement.Source = MediaSource.CreateFromMediaFrameSource(_frameSource);
		}

		IsInitialized = true;

		await PlatformUpdateResolution(cameraView.ImageCaptureResolution, token);

		if(AnalyseImages && _frameSource is not null)
		{
            _mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(_frameSource, MediaEncodingSubtypes.Bgra8);
            _mediaFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
            await _mediaFrameReader.StartAsync();
        }

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

    /// <summary>
    /// Stops the camera preview and disposes of the MediaCapture object.
    /// </summary>
    protected virtual async void PlatformStopCameraPreview()
	{
		if (_mediaElement is null)
		{
			return;
		}

		_mediaElement.Source = null;
		_mediaCapture?.Dispose();

		_mediaCapture = null;

        if (_mediaFrameReader != null)
        {
            await _mediaFrameReader.StopAsync();
            _mediaFrameReader.FrameArrived -= ColorFrameReader_FrameArrived;
            _mediaFrameReader.Dispose();
            _mediaFrameReader = null;
        }
        IsInitialized = false;
	}

	protected async Task PlatformUpdateResolution(Size resolution, CancellationToken token)
	{
		if (!IsInitialized || _mediaCapture is null)
		{
			return;
		}

		if (cameraView.SelectedCamera is null)
		{
			await cameraProvider.RefreshAvailableCameras(token);
			cameraView.SelectedCamera = cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");
		}

		var filteredPropertiesList = cameraView.SelectedCamera.ImageEncodingProperties.Where(p => p.Width <= resolution.Width && p.Height <= resolution.Height).ToList();

		filteredPropertiesList = filteredPropertiesList.Count is not 0
			? filteredPropertiesList
			: [.. cameraView.SelectedCamera.ImageEncodingProperties.OrderByDescending(p => p.Width * p.Height)];

		if (filteredPropertiesList.Count is not 0)
		{
			await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, filteredPropertiesList.First()).AsTask(token);
		}
	}
}
#endif