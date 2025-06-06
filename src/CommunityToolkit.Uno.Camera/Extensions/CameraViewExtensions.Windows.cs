﻿#if WINDOWS
using CommunityToolkit.Uno.Core;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;

namespace CommunityToolkit.Uno.Extensions;

static class CameraViewExtensions
{
	public static async Task UpdateAvailability(this ICameraControl cameraView, CancellationToken token)
	{
		var videoCaptureDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).AsTask(token);

		cameraView.IsAvailable = videoCaptureDevices.Count > 0;
	}

	public static Task InitializeCameraForCameraView(this MediaCapture mediaCapture, string deviceId, CancellationToken token)
	{
		try
		{
			return mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
			{
				VideoDeviceId = deviceId,
				PhotoCaptureSource = PhotoCaptureSource.Auto
			}).AsTask(token);
		}
		catch (System.Runtime.InteropServices.COMException)
		{
			// Camera already initialized
			return Task.CompletedTask;
		}
	}
}
#endif