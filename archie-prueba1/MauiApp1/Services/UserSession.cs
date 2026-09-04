using MauiApp1.Models;

namespace MauiApp1.Services
{
    public static class UserSession
    {
        public static string? Token { get; set; }
        public static int Idusuario { get; set; }
        public static string Nombre { get; set; } = "Invitado";
        public static string Apellido { get; set; } = string.Empty;
        public static string Email { get; set; } = string.Empty;
        public static string Rol { get; set; } = "Arquitecto";
        public static string BaseUrl { get; set; } = "http://10.0.2.2:5000";

        public static ProyectoDto? ActiveProject { get; set; }

        public static bool IsAuthenticated => !string.IsNullOrEmpty(Token);

        public static string NombreCompleto => string.IsNullOrWhiteSpace(Apellido) 
            ? Nombre 
            : $"{Nombre} {Apellido}";

        public static void SetSession(LoginResponse loginResponse, string baseUrl)
        {
            Token = loginResponse.Token;
            Idusuario = loginResponse.Idusuario;
            Nombre = loginResponse.Nombre;
            Apellido = loginResponse.Apellido;
            Email = loginResponse.Email;
            Rol = loginResponse.Rol;
            BaseUrl = baseUrl;
        }

        public static void ClearSession()
        {
            Token = null;
            Idusuario = 0;
            Nombre = "Invitado";
            Apellido = string.Empty;
            Email = string.Empty;
            Rol = "Arquitecto";
        }
    }
}
