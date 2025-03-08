namespace CommunityToolkit.Maui.Core;

partial class CameraProvider
{
#if !__IOS__ && !__ANDROID__ && !__MACOS__ && !WINDOWS
	public partial ValueTask RefreshAvailableCameras(CancellationToken token) => throw new NotSupportedException();
#endif
}