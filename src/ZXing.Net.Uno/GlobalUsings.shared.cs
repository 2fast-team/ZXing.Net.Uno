﻿#if IOS || MACCATALYST
global using NativePlatformCameraPreviewView = global::UIKit.UIView;
global using NativePlatformView = global::UIKit.UIView;
global using NativePlatformImageView = global::UIKit.UIImageView;
global using NativePlatformImage = global::UIKit.UIImage;
#elif ANDROID
global using NativePlatformCameraPreviewView = global::AndroidX.Camera.View.PreviewView;
global using NativePlatformView = global::Android.Views.View;
global using NativePlatformImageView = global::Android.Widget.ImageView;
global using NativePlatformImage = global::Android.Graphics.Bitmap;
#elif WINDOWS
global using NativePlatformCameraPreviewView = global::Microsoft.UI.Xaml.FrameworkElement;
global using NativePlatformView = global::Microsoft.UI.Xaml.FrameworkElement;
global using NativePlatformImageView = global::Microsoft.UI.Xaml.Controls.Image;
global using NativePlatformImage = global::Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap;
#else
global using NativePlatformCameraPreviewView = global::Microsoft.UI.Xaml.Controls.Control;
global using NativePlatformView = global::Microsoft.UI.Xaml.Controls.Control;
global using NativePlatformImageView = ZXing.Net.Uno.NativePlatformImageView;
global using NativePlatformImage = ZXing.Net.Uno.NativePlatformImage;
#endif