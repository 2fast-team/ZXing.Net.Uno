using System.ComponentModel;
using System.Runtime.Versioning;
using CommunityToolkit.Uno.Core.Primitives;
using Windows.Foundation;
namespace CommunityToolkit.Uno.Core;

/// <summary>Default Values for <see cref="ICameraControl"/>"/></summary>
[SupportedOSPlatform("windows10.0.17763.0")]
[SupportedOSPlatform("android21.0")]
[SupportedOSPlatform("ios")]
[SupportedOSPlatform("maccatalyst")]
//[SupportedOSPlatform("tizen")]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CameraViewDefaults
{
	/// <summary>
	/// Default value for <see cref="ICameraControl.IsAvailable"/>
	/// </summary>
	public const bool IsAvailable = false;

	/// <summary>
	/// Default value for <see cref="ICameraControl.IsTorchOn"/>
	/// </summary>
	public const bool IsTorchOn = false;

	/// <summary>
	/// Default value for <see cref="ICameraControl.IsBusy"/>
	/// </summary>
	public const bool IsCameraBusy = false;

	/// <summary>
	/// Default value for <see cref="ICameraControl.ZoomFactor"/>
	/// </summary>
	public const float ZoomFactor = 1.0f;

	/// <summary>
	/// Default value for <see cref="ICameraControl.ImageCaptureResolution"/>
	/// </summary>
	public static Size ImageCaptureResolution { get; } = Size.Empty;

	/// <summary>
	/// Default value for <see cref="ICameraControl.CameraFlashMode"/>
	/// </summary>
	public static CameraFlashMode CameraFlashMode { get; } = CameraFlashMode.Off;

	//internal static Command<CancellationToken> CreateCaptureImageCommand(DependencyProperty bindable)
	//{
	//	var cameraView = (CameraView)bindable;
	//	return new(async token => await cameraView.CaptureImage(token).ConfigureAwait(false));
	//}

	//internal static Command<CancellationToken> CreateStartCameraPreviewCommand(BindableObject bindable)
	//{
	//	var cameraView = (CameraView)bindable;
	//	return new(async token => await cameraView.StartCameraPreview(token).ConfigureAwait(false));
	//}

	//internal static ICommand CreateStopCameraPreviewCommand(BindableObject bindable)
	//{
	//	var cameraView = (CameraView)bindable;
	//	return new Command(token => cameraView.StopCameraPreview());
	//}
}