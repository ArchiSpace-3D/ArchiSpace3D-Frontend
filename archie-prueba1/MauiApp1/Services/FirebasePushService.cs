#if ANDROID
using Plugin.Firebase.CloudMessaging;
#endif
using System.Diagnostics;

namespace MauiApp1.Services
{
    public static class FirebasePushService
    {
#if ANDROID
        private static bool _listenersRegistrados = false;
#endif

        public static async Task InicializarYRegistrarAsync()
        {
            try
            {
#if ANDROID
                if (OperatingSystem.IsAndroidVersionAtLeast(33))
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                    if (status != PermissionStatus.Granted)
                    {
                        status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                    }
                    if (status != PermissionStatus.Granted)
                    {
                        return;
                    }
                }

                await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
                var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();

                if (!string.IsNullOrEmpty(token) && UserSession.Idusuario > 0)
                {
                    await ApiService.RegistrarFcmTokenAsync(UserSession.Idusuario, token);
                }

                RegistrarListeners();
#else
                await Task.CompletedTask;
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FirebasePushService no disponible en este entorno: {ex.Message}");
            }
        }

#if ANDROID
        private static void RegistrarListeners()
        {
            if (_listenersRegistrados) return;
            _listenersRegistrados = true;

            CrossFirebaseCloudMessaging.Current.TokenChanged += async (_, e) =>
            {
                if (UserSession.Idusuario > 0 && !string.IsNullOrEmpty(e.Token))
                {
                    await ApiService.RegistrarFcmTokenAsync(UserSession.Idusuario, e.Token);
                }
            };

            CrossFirebaseCloudMessaging.Current.NotificationReceived += (_, e) =>
            {
                Debug.WriteLine($"Push recibido: {e.Notification.Title} - {e.Notification.Body}");
            };

            CrossFirebaseCloudMessaging.Current.NotificationTapped += (_, e) =>
            {
                if (e.Notification.Data.TryGetValue("idProyecto", out var idProyectoStr) &&
                    int.TryParse(idProyectoStr, out var idProyecto))
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try
                        {
                            var proyecto = await ApiService.GetProyectoByIdAsync(idProyecto);
                            if (proyecto != null)
                                UserSession.ActiveProject = proyecto;

                            if (Shell.Current != null)
                                await Shell.Current.GoToAsync("//DashboardPage");
                        }
                        catch { }
                    });
                }
            };
        }
#endif
    }
}
