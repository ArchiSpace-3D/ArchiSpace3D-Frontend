using MauiApp1.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace MauiApp1.Services
{
    public static class SignalRService
    {
        private static HubConnection? _connection;
        private static readonly HashSet<int> _salasUnidas = new();
        private static List<int> _proyectosUsuario = new();

        public static event Action<NotificacionDto>? NotificacionRecibida;

        public static bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public static async Task ConnectAsync()
        {
            if (_connection != null && _connection.State != HubConnectionState.Disconnected)
                return;

            _connection = new HubConnectionBuilder()
                .WithUrl($"{UserSession.BaseUrl}/hubs/sala", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(UserSession.Token);
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On<NotificacionDto>("NuevaNotificacion", n =>
            {
                MainThread.BeginInvokeOnMainThread(() => NotificacionRecibida?.Invoke(n));
            });

            _connection.Reconnected += async _ =>
            {
                foreach (var id in _salasUnidas.ToList())
                    await _connection.InvokeAsync("UnirseASala", id);
            };

            await _connection.StartAsync();
        }

        public static void RegistrarProyectos(IEnumerable<int> ids) =>
            _proyectosUsuario = ids.Distinct().ToList();

        public static async Task UnirseASalaAsync(int idProyecto)
        {
            if (_connection?.State != HubConnectionState.Connected) return;
            await _connection.InvokeAsync("UnirseASala", idProyecto);
            _salasUnidas.Add(idProyecto);
        }

        public static async Task UnirseASalasDelUsuarioAsync()
        {
            foreach (var id in _proyectosUsuario)
                await UnirseASalaAsync(id);
        }

        public static async Task DisconnectAsync()
        {
            if (_connection is null) return;
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
            _salasUnidas.Clear();
        }
    }
}