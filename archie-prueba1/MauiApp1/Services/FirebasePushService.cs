#if ANDROID
using Plugin.Firebase.CloudMessaging;
#endif

namespace MauiApp1.Services
{
    public static class FirebasePushService
    {
#if ANDROID
        private static bool _listenersRegistrados = false;
#endif

        /// <summary>
        /// Llamar justo después de un login exitoso (normal o Google).
        /// Pide permiso, obtiene el token FCM y lo registra en el backend.
        /// </summary>
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
                        System.Diagnostics.Debug.WriteLine("Permiso de notificaciones denegado.");
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
#endif
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Application.Current?.MainPage?.DisplayAlert("Error Notificaciones", ex.Message, "OK");
                });
            }
        }

#if ANDROID
        private static void RegistrarListeners()
        {
            if (_listenersRegistrados) return;
            _listenersRegistrados = true;

            // El token puede cambiar (reinstalación, limpieza de datos, etc.)
            Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.TokenChanged += async (_, e) =>
            {
                if (UserSession.Idusuario > 0 && !string.IsNullOrEmpty(e.Token))
                {
                    await ApiService.RegistrarFcmTokenAsync(UserSession.Idusuario, e.Token);
                }
            };

            // Push recibido con la app en foreground
            Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.NotificationReceived += (_, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Push recibido: {e.Notification.Title} - {e.Notification.Body}");
            };

            // Usuario tocó la notificación (app en background o cerrada)
            Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.NotificationTapped += (_, e) =>
            {
                if (e.Notification.Data.TryGetValue("idProyecto", out var idProyectoStr) &&
                    int.TryParse(idProyectoStr, out var idProyecto))
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        // Ajusta esto a como navegues realmente a la pantalla del proyecto
                        // await Shell.Current.GoToAsync($"//ProyectoDetallePage?idProyecto={idProyecto}");
                    });
                }
            };
        }
#endif
    }
}