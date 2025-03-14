using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Uno.Core;
using CommunityToolkit.Uno.Core.Primitives;
using CommunityToolkit.Uno.Views;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Media.Core;

namespace CommunityToolkit.Uno.Camera.Controls
{
    [TemplatePart(Name = MainGridName, Type = typeof(Grid))]
    public sealed partial class CameraControl : Control, ICameraView
    {
        private const string MainGridName = "MainGrid";
        Grid MainGrid;
        CameraManager CameraManager { get; }
        public CameraControl()
		{
			this.DefaultStyleKey = typeof(CameraControl);
            CameraManager = new CameraManager(this, CameraProvider, ()=>
            {
                //CameraManager.ConnectCamera(CancellationToken.None);
            });

            //CameraManager.UpdateFlashMode(CameraFlashMode.On);
            //CameraManager.

        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            MainGrid = (Grid)GetTemplateChild(MainGridName);
            StartCamara();
        }

        private async Task StartCamara()
        {


            MainGrid.Background = new SolidColorBrush(Colors.Red);

            var previewView = CameraManager.CreatePlatformView();
            MainGrid.Children.Add(previewView);

            await CameraManager.UpdateCaptureResolution(MainGrid.DesiredSize, CancellationToken.None);
            await CameraManager.ConnectCamera(CancellationToken.None);
            //await CameraManager.StartCameraPreview(CancellationToken.None);
        }

        /// <summary>
        /// Gets a value indicating whether the camera feature is available on the current device.
        /// </summary>
        public bool IsAvailable
        {
            get => (bool)GetValue(IsAvailableProperty);
            set => SetValue(IsAvailableProperty, value);
        }

        static readonly DependencyProperty IsAvailableProperty =
            DependencyProperty.Register(
                nameof(IsAvailable), 
                typeof(bool), 
                typeof(CameraControl),
                new PropertyMetadata(CameraViewDefaults.IsAvailable));

        /// <summary>
        /// Backing <see cref="DependencyProperty"/> for the <see cref="CameraFlashMode"/> property.
        /// </summary>
        public static readonly DependencyProperty CameraFlashModeProperty =
            DependencyProperty.Register(
                nameof(CameraFlashMode), 
                typeof(CameraFlashMode), 
                typeof(CameraControl),
                new PropertyMetadata(CameraViewDefaults.CameraFlashMode));

        /// <summary>
        /// Gets or sets the <see cref="CameraFlashMode"/>.
        /// </summary>
        public CameraFlashMode CameraFlashMode
        {
            get => (CameraFlashMode)GetValue(CameraFlashModeProperty);
            set => SetValue(CameraFlashModeProperty, value);
        }

        /// <summary>
        /// Backing <see cref="DependencyProperty"/> for the <see cref="IsTorchOn"/> property.
        /// </summary>
        public static readonly DependencyProperty IsTorchOnProperty =
            DependencyProperty.Register(
                nameof(IsTorchOn), 
                typeof(bool), 
                typeof(CameraControl),
                new PropertyMetadata(CameraViewDefaults.IsTorchOn));

        /// <summary>
        /// Gets or sets a value indicating whether the torch (flash) is on.
        /// </summary>
        public bool IsTorchOn
        {
            get => (bool)GetValue(IsTorchOnProperty);
            set => SetValue(IsTorchOnProperty, value);
        }

        static readonly DependencyProperty IsCameraBusyProperty =
            DependencyProperty.Register(
                nameof(IsCameraBusy), 
                typeof(bool), 
                typeof(CameraControl),
                new PropertyMetadata(CameraViewDefaults.IsCameraBusy));

        /// <summary>
        /// Gets a value indicating whether the camera is currently busy.
        /// </summary>
        public bool IsCameraBusy
        { 
            get => (bool)GetValue(IsCameraBusyProperty); 
            private set => SetValue(IsCameraBusyProperty, value); 
        }

        /// <summary>
        /// Backing <see cref="DependencyProperty"/> for the <see cref="SelectedCamera"/> property.
        /// </summary>
        public static readonly DependencyProperty? SelectedCameraProperty =
            DependencyProperty.Register(
                nameof(SelectedCamera),
                typeof(CameraInfo), 
                typeof(CameraControl), 
                new PropertyMetadata(null));

        /// <inheritdoc cref="ICameraView.SelectedCamera"/>
        public CameraInfo? SelectedCamera
        {
            get => (CameraInfo?)GetValue(SelectedCameraProperty);
            set => SetValue(SelectedCameraProperty, value);
        }

        /// <summary>
        /// Backing <see cref="DependencyProperty"/> for the <see cref="ZoomFactor"/> property.
        /// </summary>
        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register(
                nameof(ZoomFactor), 
                typeof(float), 
                typeof(CameraControl),
                new PropertyMetadata(CameraViewDefaults.ZoomFactor));

        /// <inheritdoc cref="ICameraView.ZoomFactor"/>
        public float ZoomFactor
        {
            get => (float)GetValue(ZoomFactorProperty);
            set => SetValue(ZoomFactorProperty, value);
        }

        /// <summary>
        /// Backing <see cref="DependencyProperty"/> for the <see cref="ImageCaptureResolution"/> property.
        /// </summary>
        public static readonly DependencyProperty ImageCaptureResolutionProperty =
            DependencyProperty.Register(
                nameof(ImageCaptureResolution),
                typeof(Size), 
                typeof(CameraControl),
                new PropertyMetadata(CameraViewDefaults.ImageCaptureResolution));

        /// <inheritdoc cref="ICameraView.ImageCaptureResolution"/>
        public Size ImageCaptureResolution
        {
            get => (Size)GetValue(ImageCaptureResolutionProperty);
            set => SetValue(ImageCaptureResolutionProperty, value);
        }

        //static ICameraProvider CameraProvider => Application.Current?.Services.GetRequiredService<ICameraProvider>() ?? throw new CameraException("Unable to retrieve CameraProvider");
        static ICameraProvider CameraProvider { get; } = new CameraProvider();

        TaskCompletionSource handlerCompletedTCS = new();

        [EditorBrowsable(EditorBrowsableState.Never)]
        TaskCompletionSource IAsynchronousHandler.HandlerCompleteTCS => handlerCompletedTCS;

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool ICameraView.IsBusy
        {
            get => IsCameraBusy;
            set => SetValue(IsCameraBusyProperty, value);
        }

        public TaskCompletionSource HandlerCompleteTCS => throw new NotImplementedException();

        public void OnMediaCaptured(Stream imageData)
        {
            throw new NotImplementedException();
        }

        public void OnMediaCapturedFailed(string failureReason)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="ICameraView.CaptureImage"/>
        public async ValueTask CaptureImage(CancellationToken token)
        {
            handlerCompletedTCS.TrySetCanceled(token);

            handlerCompletedTCS = new();
            //Handler?.Invoke(nameof(ICameraView.CaptureImage));

            await handlerCompletedTCS.Task.WaitAsync(token);
        }

        /// <inheritdoc cref="ICameraView.StartCameraPreview"/>
        public async ValueTask StartCameraPreview(CancellationToken token)
        {
            handlerCompletedTCS.TrySetCanceled(token);

            handlerCompletedTCS = new();

            CameraManager.StartCameraPreview(token);
            //Handler?.Invoke(nameof(ICameraView.StartCameraPreview));

            await handlerCompletedTCS.Task.WaitAsync(token);
        }

        /// <inheritdoc cref="ICameraView.StopCameraPreview"/>
        public void StopCameraPreview()
        {
            CameraManager.StopCameraPreview();
            //Handler?.Invoke(nameof(ICameraView.StopCameraPreview));
        }

        /// <inheritdoc cref="ICameraView.GetAvailableCameras"/>
        public async ValueTask<IReadOnlyList<CameraInfo>> GetAvailableCameras(CancellationToken token)
        {
            if (CameraProvider.AvailableCameras is null)
            {
                await CameraProvider.RefreshAvailableCameras(token);

                if (CameraProvider.AvailableCameras is null)
                {
                    throw new CameraException("Unable to refresh available cameras");
                }
            }

            return CameraProvider.AvailableCameras;
        }

        ///// <summary>
        ///// Event that is raised when the camera capture fails.
        ///// </summary>
        //public event EventHandler<MediaCaptureFailedEventArgs> MediaCaptureFailed
        //{
        //    add => weakEventManager.AddEventHandler(value);
        //    remove => weakEventManager.RemoveEventHandler(value);
        //}

        ///// <summary>
        ///// Event that is raised when the camera captures an image.
        ///// </summary>
        ///// <remarks>
        ///// The <see cref="MediaCapturedEventArgs"/> contains the captured image data.
        ///// </remarks>
        //public event EventHandler<MediaCapturedEventArgs> MediaCaptured
        //{
        //    add => weakEventManager.AddEventHandler(value);
        //    remove => weakEventManager.RemoveEventHandler(value);
        //}
    }
}
