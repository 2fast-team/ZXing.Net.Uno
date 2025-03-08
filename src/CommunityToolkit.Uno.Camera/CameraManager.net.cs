using CommunityToolkit.Maui.Core.Primitives;
using Windows.Foundation;

namespace CommunityToolkit.Maui.Core;

partial class CameraManager
{
	const string notSupportedMessage = "CameraView is only supported on net-ios, net-android, net-windows and net-maccatalyst.";
#if !__IOS__ && !__ANDROID__ && !__MACOS__ && !WINDOWS
    public void Dispose() => throw new NotSupportedException(notSupportedMessage);

	public NativePlatformCameraPreviewView CreatePlatformView() => throw new NotSupportedException(notSupportedMessage);

	public void UpdateFlashMode(CameraFlashMode flashMode) => throw new NotSupportedException(notSupportedMessage);

	public void UpdateZoom(float zoomLevel) => throw new NotSupportedException(notSupportedMessage);

	public ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token) => throw new NotSupportedException(notSupportedMessage);

	protected virtual Task PlatformStartCameraPreview(CancellationToken token) => throw new NotSupportedException(notSupportedMessage);

	protected virtual void PlatformStopCameraPreview() => throw new NotSupportedException(notSupportedMessage);

	protected virtual Task PlatformConnectCamera(CancellationToken token) => throw new NotSupportedException(notSupportedMessage);

	protected virtual void PlatformDisconnect() => throw new NotSupportedException(notSupportedMessage);

	protected virtual ValueTask PlatformTakePicture(CancellationToken token) => throw new NotSupportedException(notSupportedMessage);
#endif
}