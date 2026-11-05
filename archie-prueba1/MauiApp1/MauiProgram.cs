using MauiApp1;
using Microsoft.Maui.LifecycleEvents;
#if ANDROID
using Plugin.Firebase.Core.Platforms.Android;
#endif

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (h, _) =>
        {
#if ANDROID
            h.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif WINDOWS
            h.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
#endif
        });

        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("NoUnderline", (h, _) =>
        {
#if ANDROID
            h.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif WINDOWS
            h.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
#endif
        });

        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline", (h, _) =>
        {
#if ANDROID
            h.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif WINDOWS
            h.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
#endif
        });

        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("ArchieWebViewPermissions", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.Settings.JavaScriptEnabled = true;
            handler.PlatformView.Settings.DomStorageEnabled = true;
            handler.PlatformView.Settings.AllowFileAccess = true;
            handler.PlatformView.Settings.MediaPlaybackRequiresUserGesture = false;
            if (view is Element el && el.StyleId == "MeasureARWebView")
            {
                handler.PlatformView.SetWebChromeClient(new ArchiMeasureWebChromeClient());
            }
#endif
        });

        builder
            .UseMauiApp<App>()
            .RegisterFirebaseServices()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        return builder.Build();
    }

    private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android.OnCreate((activity, _) =>
                CrossFirebase.Initialize(activity, () => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? activity)));
#endif
        });

        return builder;
    }
}

#if ANDROID
internal sealed class ArchiMeasureWebChromeClient : Android.Webkit.WebChromeClient
{
    public override void OnPermissionRequest(Android.Webkit.PermissionRequest? request)
    {
        request?.Grant(request.GetResources());
    }
}
#endif
