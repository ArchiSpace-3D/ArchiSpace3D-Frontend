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
                Debug.WriteLine("🔥 FirebasePushService: iniciando...");

                await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
                Debug.WriteLine("🔥 CheckIfValidAsync completado.");

                var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
                Debug.WriteLine($"🔥 Token obtenido: {token}");

                if (!string.IsNullOrEmpty(token) && UserSession.Idusuario > 0)
                {
                    Debug.WriteLine($"🔥 Enviando token al backend para idusuario={UserSession.Idusuario}...");
                    var (success, message) = await ApiService.RegistrarFcmTokenAsync(UserSession.Idusuario, token);
                    Debug.WriteLine($"🔥 Resultado registro: success={success}, message={message}");
                }
                else
                {
                    Debug.WriteLine($"🔥 No se envía: token vacío={string.IsNullOrEmpty(token)}, idusuario={UserSession.Idusuario}");
                }

                RegistrarListeners();
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"🔥 ERROR en InicializarYRegistrarAsync: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
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
                Debug.WriteLine($"🔥 Push recibido: {e.Notification.Title} - {e.Notification.Body}");
            };

            CrossFirebaseCloudMessaging.Current.NotificationTapped += (_, e) =>
            {
                if (e.Notification.Data.TryGetValue("idProyecto", out var idProyectoStr) &&
                    int.TryParse(idProyectoStr, out var idProyecto))
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        // await Shell.Current.GoToAsync($"//ProyectoDetallePage?idProyecto={idProyecto}");
                    });
                }
            };
        }
#endif
    }
}