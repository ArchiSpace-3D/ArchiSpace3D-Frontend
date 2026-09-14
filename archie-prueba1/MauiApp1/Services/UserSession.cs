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
        public static string BaseUrl { get; set; } = "https://archispace3d-backend-production.up.railway.app";
        public static string? Telefono { get; set; }
        public static string? Direccion { get; set; }
        public static string? Tipodocumento { get; set; }
        public static string? Numerodocumento { get; set; }
        public static string? Avatarurl { get; set; }

        public static ProyectoDto? ActiveProject { get; set; }

        public static bool IsAuthenticated => !string.IsNullOrEmpty(Token);

        public static string NombreCompleto => string.IsNullOrWhiteSpace(Apellido) 
            ? Nombre 
            : $"{Nombre} {Apellido}";

        public static async Task SetSessionAsync(LoginResponse loginResponse, string baseUrl)
        {
            Token = loginResponse.Token;
            Idusuario = loginResponse.Idusuario;
            Nombre = loginResponse.Nombre;
            Apellido = loginResponse.Apellido;
            Email = loginResponse.Email;
            Rol = loginResponse.Rol;
            Telefono = loginResponse.Telefono;
            Direccion = loginResponse.Direccion;
            Tipodocumento = loginResponse.Tipodocumento;
            Numerodocumento = loginResponse.Numerodocumento;
            Avatarurl = loginResponse.Avatarurl;
            BaseUrl = baseUrl;

            await SecureStorage.SetAsync("auth_token", Token ?? "");
            await SecureStorage.SetAsync("user_id", Idusuario.ToString());
            await SecureStorage.SetAsync("user_nombre", Nombre ?? "");
            await SecureStorage.SetAsync("user_apellido", Apellido ?? "");
            await SecureStorage.SetAsync("user_email", Email ?? "");
            await SecureStorage.SetAsync("user_rol", Rol ?? "");
            await SecureStorage.SetAsync("user_telefono", Telefono ?? "");
            await SecureStorage.SetAsync("user_direccion", Direccion ?? "");
            await SecureStorage.SetAsync("user_tipodocumento", Tipodocumento ?? "");
            await SecureStorage.SetAsync("user_numerodocumento", Numerodocumento ?? "");
            await SecureStorage.SetAsync("user_avatarurl", Avatarurl ?? "");
        }

        public static async Task<bool> LoadSessionAsync()
        {
            var token = await SecureStorage.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token))
                return false;

            Token = token;
            
            var idStr = await SecureStorage.GetAsync("user_id");
            if (int.TryParse(idStr, out int id))
                Idusuario = id;

            Nombre = await SecureStorage.GetAsync("user_nombre") ?? "Invitado";
            Apellido = await SecureStorage.GetAsync("user_apellido") ?? "";
            Email = await SecureStorage.GetAsync("user_email") ?? "";
            Rol = await SecureStorage.GetAsync("user_rol") ?? "Arquitecto";
            Telefono = await SecureStorage.GetAsync("user_telefono");
            Direccion = await SecureStorage.GetAsync("user_direccion");
            Tipodocumento = await SecureStorage.GetAsync("user_tipodocumento");
            Numerodocumento = await SecureStorage.GetAsync("user_numerodocumento");
            Avatarurl = await SecureStorage.GetAsync("user_avatarurl");

            return true;
        }

        public static void ClearSession()
        {
            Token = null;
            Idusuario = 0;
            Nombre = "Invitado";
            Apellido = string.Empty;
            Email = string.Empty;
            Rol = "Arquitecto";
            Telefono = null;
            Direccion = null;
            Tipodocumento = null;
            Numerodocumento = null;
            Avatarurl = null;
            
            SecureStorage.RemoveAll();
        }
    }
}
