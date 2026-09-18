using Android.App;
using Android.Runtime;
using Android.Content;
using Plugin.Firebase.CloudMessaging;

namespace MauiApp1
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override void OnCreate()
        {
            base.OnCreate();
            CrearCanalDeNotificaciones();
        }

        private void CrearCanalDeNotificaciones()
        {
            var channelId = $"{PackageName}.general";
            var notificationManager = (NotificationManager)GetSystemService(Context.NotificationService)!;
            var channel = new NotificationChannel(channelId, "General", NotificationImportance.Default);
            notificationManager.CreateNotificationChannel(channel);
            FirebaseCloudMessagingImplementation.ChannelId = channelId;
        }
    }
}