#if WINDOWS
using System.Runtime.Versioning;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Extensions;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.MediaProperties;

namespace CommunityToolkit.Maui.Core;

[SupportedOSPlatform("windows10.0.17763.0")]
partial class CameraManager
{
	MediaPlayerElement? mediaElement;
	MediaCapture? mediaCapture;
	MediaFrameSource? frameSource;

	public MediaPlayerElement CreatePlatformView()
	{
		mediaElement = new MediaPlayerElement();
		return mediaElement;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public void UpdateFlashMode(CameraFlashMode flashMode)
	{
		if (!IsInitialized || (mediaCapture?.VideoDeviceController.FlashControl.Supported is false))
		{
			return;
		}

		if (mediaCapture is null)
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

		mediaCapture.VideoDeviceController.FlashControl.Enabled = updatedFlashControlEnabled;

		if (updatedFlashControlAuto.HasValue)
		{
			mediaCapture.VideoDeviceController.FlashControl.Auto = updatedFlashControlAuto.Value;
		}

	}

	public void UpdateZoom(float zoomLevel)
	{
		if (!IsInitialized || mediaCapture is null || !mediaCapture.VideoDeviceController.ZoomControl.Supported)
		{
			return;
		}

		var step = mediaCapture.VideoDeviceController.ZoomControl.Step;

		if (zoomLevel % step != 0)
		{
			zoomLevel = (float)Math.Ceiling(zoomLevel / step) * step;
		}

		mediaCapture.VideoDeviceController.ZoomControl.Value = zoomLevel;
	}

	public async ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token)
	{
		await PlatformUpdateResolution(resolution, token);
	}

	protected virtual void PlatformDisconnect()
	{
	}

	protected virtual async ValueTask PlatformTakePicture(CancellationToken token)
	{
		if (mediaCapture is null)
		{
			return;
		}

		token.ThrowIfCancellationRequested();

		MemoryStream memoryStream = new();

		try
		{
			await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), memoryStream.AsRandomAccessStream());

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
			mediaCapture?.Dispose();
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
		if (mediaElement is null)
		{
			return;
		}

		mediaCapture = new MediaCapture();

		if (cameraView.SelectedCamera is null)
		{
			await cameraProvider.RefreshAvailableCameras(token);
			cameraView.SelectedCamera = cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");
		}

		await mediaCapture.InitializeCameraForCameraView(cameraView.SelectedCamera.DeviceId, token);

		frameSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;

		if (frameSource is not null)
		{
			mediaElement.AutoPlay = true;
			mediaElement.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
		}

		IsInitialized = true;

		await PlatformUpdateResolution(cameraView.ImageCaptureResolution, token);

		OnLoaded.Invoke();
	}

	protected virtual void PlatformStopCameraPreview()
	{
		if (mediaElement is null)
		{
			return;
		}

		mediaElement.Source = null;
		mediaCapture?.Dispose();

		mediaCapture = null;
		IsInitialized = false;
	}

	protected async Task PlatformUpdateResolution(Size resolution, CancellationToken token)
	{
		if (!IsInitialized || mediaCapture is null)
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
			await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, filteredPropertiesList.First()).AsTask(token);
		}
	}
}
#endif