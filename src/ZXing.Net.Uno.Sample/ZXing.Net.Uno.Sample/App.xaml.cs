using Uno.Resizetizer;

namespace ZXing.Net.Uno.Sample;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
                .ConfigureServices((context, services) =>
                {
                    // TODO: Register your services
                    //services.AddSingleton<IMyService, MyService>();
                })
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = builder.Build();

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();

            // Place the frame in the current Window
            MainWindow.Content = rootFrame;
        }

        if (rootFrame.Content == null)
        {
            // When the navigation stack isn't restored navigate to the first page,
            // configuring the new page by passing required information as a navigation
            // parameter
            CheckPermissionAndNavigate(rootFrame, args);
        }
        // Ensure the current window is active
        MainWindow.Activate();
    }

#if __ANDROID__
    private async Task CheckPermissionAndNavigate(Frame frame, LaunchActivatedEventArgs args)
#else
    private Task CheckPermissionAndNavigate(Frame frame, LaunchActivatedEventArgs args)
#endif
    {
#if __ANDROID__
        // This will only check if the permission is granted but will not prompt the user.
        bool isGranted = await Windows.Extensions.PermissionsHelper.CheckPermission(new System.Threading.CancellationToken(), Android.Manifest.Permission.Camera);

        if (!isGranted)
        {
            // This will prompt the user with the native permission dialog if needed. If already granted it will simply return true.
            bool isPermissionGranted = await Windows.Extensions.PermissionsHelper.TryGetPermission(new System.Threading.CancellationToken(), Android.Manifest.Permission.Camera);
            if (isPermissionGranted)
            {
                frame.Navigate(typeof(MainPage), args.Arguments);
            }
            else
            {
                App.Current.Exit();
            }
        }
        else
        {
            frame.Navigate(typeof(MainPage), args.Arguments);
        }
#else
        frame.Navigate(typeof(MainPage), args.Arguments);
        return Task.CompletedTask;
#endif
    }
}
