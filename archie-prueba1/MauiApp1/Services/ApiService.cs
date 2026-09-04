using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MauiApp1.Models;

namespace MauiApp1.Services
{
    public class ApiService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static string NormalizeBaseUrl(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "http://192.168.101.75:5000";
            }

            string trimmed = input.Trim().TrimEnd('/');

            if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = "http://" + trimmed;
            }

            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                if (uri.IsDefaultPort || uri.Port == 80)
                {
                    if (!trimmed.Contains(":80") && !trimmed.Contains(":443"))
                    {
                        var builder = new UriBuilder(uri)
                        {
                            Port = 5000
                        };
                        return builder.Uri.ToString().TrimEnd('/');
                    }
                }
            }

            return trimmed;
        }

        private static void SetAuthHeader(HttpRequestMessage request)
        {
            if (!string.IsNullOrEmpty(UserSession.Token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserSession.Token);
            }
        }

        // ==================== AUTH Y USUARIOS ====================

        public static async Task<(bool Success, string Message, LoginResponse? Data)> LoginAsync(string hostOrUrl, string email, string contrasena)
        {
            try
            {
                string baseUrl = NormalizeBaseUrl(hostOrUrl);
                string url = $"{baseUrl}/api/auth/login";

                var loginData = new LoginRequest
                {
                    Email = email,
                    Contrasena = contrasena
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(loginData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResult = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(jsonResult, _jsonOptions);

                    if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.Token))
                    {
                        UserSession.SetSession(loginResponse, baseUrl);
                        return (true, "Inicio de sesión exitoso.", loginResponse);
                    }

                    return (false, "Respuesta inválida del servidor.", null);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    string err = await response.Content.ReadAsStringAsync();
                    return (false, string.IsNullOrWhiteSpace(err) ? "Credenciales incorrectas." : err, null);
                }

                return (false, $"Error del backend ({response.StatusCode}): {await response.Content.ReadAsStringAsync()}", null);
            }
            catch (TaskCanceledException)
            {
                return (false, "Tiempo de espera agotado al contactar la API. Verifica que esté corriendo y que la IP sea accesible.", null);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Error de red: no se pudo conectar con el host. Detalle: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Fallo de conexión: {ex.Message}", null);
            }
        }

        public static async Task<(bool Success, string Message)> RegistrarUsuarioAsync(string hostOrUrl, UsuarioRegistroRequest usuario)
        {
            try
            {
                string baseUrl = NormalizeBaseUrl(hostOrUrl);
                string url = $"{baseUrl}/api/usuario";

                var content = new StringContent(
                    JsonSerializer.Serialize(usuario),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Usuario registrado exitosamente. Ya puedes iniciar sesión.");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    string conflictMsg = await response.Content.ReadAsStringAsync();
                    return (false, string.IsNullOrWhiteSpace(conflictMsg) ? "El usuario o email ya existe." : conflictMsg);
                }

                string err = await response.Content.ReadAsStringAsync();
                return (false, $"Error al registrar ({response.StatusCode}): {err}");
            }
            catch (Exception ex)
            {
                return (false, $"Error de red al registrar: {ex.Message}");
            }
        }

        
        public static async Task<List<ProyectoDto>> GetProyectosAsync()
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return new List<ProyectoDto>();

            try
            {
                string url = $"{UserSession.BaseUrl}/api/proyecto";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                SetAuthHeader(request);

                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<ProyectoDto>>(json, _jsonOptions) ?? new List<ProyectoDto>();
                }
            }
            catch { }

            return new List<ProyectoDto>();
        }

        public static async Task<(bool Success, string Message, ProyectoDto? Data)> CrearProyectoAsync(CrearProyectoRequest proyecto)
        {
            if (string.IsNullOrEmpty(UserSession.Token))
                return (false, "Debes iniciar sesión para crear proyectos.", null);

            try
            {
                string url = $"{UserSession.BaseUrl}/api/proyecto";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                SetAuthHeader(request);

                request.Content = new StringContent(
                    JsonSerializer.Serialize(proyecto),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var creado = JsonSerializer.Deserialize<ProyectoDto>(json, _jsonOptions);
                    return (true, "Proyecto creado exitosamente.", creado);
                }

                string err = await response.Content.ReadAsStringAsync();
                return (false, $"Error al crear proyecto ({response.StatusCode}): {err}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error de red: {ex.Message}", null);
            }
        }

        public static async Task<(bool Success, string Message)> EliminarProyectoAsync(int idProyecto)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return (false, "No autenticado.");

            try
            {
                string url = $"{UserSession.BaseUrl}/api/proyecto/{idProyecto}";
                using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                SetAuthHeader(request);

                using var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode
                    ? (true, "Proyecto eliminado correctamente.")
                    : (false, $"Error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        // ==================== INVITACIONES ====================

        public static async Task<(bool Success, string Message, InvitacionDto? Data)> CrearInvitacionAsync(int idProyecto, string codigo)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return (false, "No autenticado.", null);

            try
            {
                string url = $"{UserSession.BaseUrl}/api/invitacion";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                SetAuthHeader(request);

                var invitacion = new CrearInvitacionRequest
                {
                    Idproyecto = idProyecto,
                    Idarquitecto = UserSession.Idusuario,
                    Codigo = codigo,
                    Fechaexpiracion = DateTime.UtcNow.AddDays(7)
                };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(invitacion),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var creada = JsonSerializer.Deserialize<InvitacionDto>(json, _jsonOptions);
                    return (true, "Invitación generada con éxito.", creada);
                }

                string err = await response.Content.ReadAsStringAsync();
                return (false, $"Error: {err}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error de red: {ex.Message}", null);
            }
        }

        public static async Task<(bool Success, string Message)> UsarInvitacionAsync(string codigo)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return (false, "No autenticado.");

            try
            {
                string url = $"{UserSession.BaseUrl}/api/invitacion/codigo/{Uri.EscapeDataString(codigo)}/usar";
                using var request = new HttpRequestMessage(HttpMethod.Put, url);
                SetAuthHeader(request);

                var body = new UsarInvitacionRequest { IdClienteUsado = UserSession.Idusuario };
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "¡Te has unido al proyecto exitosamente!");
                }

                string err = await response.Content.ReadAsStringAsync();
                return (false, $"No se pudo unir: {err}");
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        // ==================== MEDICIONES DE SENSORES ====================

        public static async Task<(bool Success, string Message)> GuardarMedicionAsync(CrearMedicionRequest medicion)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return (false, "Debes iniciar sesión para guardar mediciones.");

            try
            {
                string url = $"{UserSession.BaseUrl}/api/Medicion";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                SetAuthHeader(request);

                medicion.Fechamedicion = DateTime.UtcNow;

                request.Content = new StringContent(
                    JsonSerializer.Serialize(medicion),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Medición registrada en el proyecto.");
                }

                string err = await response.Content.ReadAsStringAsync();
                return (false, $"Error al guardar medición: {err}");
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        // ==================== NOTIFICACIONES ====================

        public static async Task<List<NotificacionDto>> GetNotificacionesAsync()
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return new List<NotificacionDto>();

            try
            {
                string url = $"{UserSession.BaseUrl}/api/notificacion";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                SetAuthHeader(request);

                using var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<NotificacionDto>>(json, _jsonOptions) ?? new List<NotificacionDto>();
                }
            }
            catch { }

            return new List<NotificacionDto>();
        }

        public static async Task<bool> MarcarNotificacionLeidaAsync(int idNotificacion)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return false;

            try
            {
                string url = $"{UserSession.BaseUrl}/api/notificacion/{idNotificacion}/leer";
                using var request = new HttpRequestMessage(HttpMethod.Put, url);
                SetAuthHeader(request);

                using var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ==================== ESPACIO FISICO ====================

        public static async Task<EspacioFisicoDto?> GetEspacioFisicoByProyectoAsync(int idProyecto)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return null;

            try
            {
                string url = $"{UserSession.BaseUrl}/api/espacioFisico/proyecto/{idProyecto}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                SetAuthHeader(request);

                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<EspacioFisicoDto>(json, _jsonOptions);
                }
            }
            catch { }

            return null;
        }

        public static async Task<(bool Success, string Message, EspacioFisicoDto? Data)> GuardarEspacioFisicoAsync(CrearEspacioFisicoRequest espacio)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return (false, "No autenticado.", null);

            try
            {
                string url = $"{UserSession.BaseUrl}/api/espacioFisico";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                SetAuthHeader(request);

                request.Content = new StringContent(
                    JsonSerializer.Serialize(espacio),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var creado = JsonSerializer.Deserialize<EspacioFisicoDto>(json, _jsonOptions);
                    return (true, "Espacio físico registrado con éxito.", creado);
                }

                string err = await response.Content.ReadAsStringAsync();
                return (false, $"Error ({response.StatusCode}): {err}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", null);
            }
        }

        // ==================== VERSIONES DE DISENO ====================

        public static async Task<List<VersionDisenoDto>> GetVersionesByProyectoAsync(int idProyecto)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return new List<VersionDisenoDto>();

            try
            {
                string url = $"{UserSession.BaseUrl}/api/versionDiseño/proyecto/{idProyecto}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                SetAuthHeader(request);

                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<VersionDisenoDto>>(json, _jsonOptions) ?? new List<VersionDisenoDto>();
                }
            }
            catch { }

            return new List<VersionDisenoDto>();
        }

        public static async Task<VersionDisenoDto?> GetVersionActualAsync(int idProyecto)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return null;

            try
            {
                string url = $"{UserSession.BaseUrl}/api/versionDiseño/proyecto/{idProyecto}/actual";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                SetAuthHeader(request);

                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<VersionDisenoDto>(json, _jsonOptions);
                }
            }
            catch { }

            return null;
        }

        public static async Task<(bool Success, string Message, VersionDisenoDto? Data)> CrearVersionDisenoAsync(CrearVersionDisenoRequest version)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return (false, "No autenticado.", null);

            try
            {
                string url = $"{UserSession.BaseUrl}/api/versionDiseño";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                SetAuthHeader(request);

                request.Content = new StringContent(
                    JsonSerializer.Serialize(version),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var creada = JsonSerializer.Deserialize<VersionDisenoDto>(json, _jsonOptions);
                    return (true, "Nueva versión creada exitosamente.", creada);
                }

                string err = await response.Content.ReadAsStringAsync();
                return (false, $"Error al crear versión: {err}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", null);
            }
        }

        public static async Task<(bool Success, string Message)> MarcarVersionComoActualAsync(int idVersion, int idProyecto)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return (false, "No autenticado.");

            try
            {
                string url = $"{UserSession.BaseUrl}/api/versionDiseño/{idVersion}/actual";
                using var request = new HttpRequestMessage(HttpMethod.Put, url);
                SetAuthHeader(request);

                var body = new MarcarActualRequest { IdProyecto = idProyecto };
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode
                    ? (true, "Versión marcada como actual.")
                    : (false, $"Error ({response.StatusCode})");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        // ==================== ELEMENTOS ESTRUCTURALES ====================

        public static async Task<List<ElementoEstructuralDto>> GetElementosByVersionAsync(int idVersion)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return new List<ElementoEstructuralDto>();

            try
            {
                string url = $"{UserSession.BaseUrl}/api/elementoeEstructural/version/{idVersion}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                SetAuthHeader(request);

                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<ElementoEstructuralDto>>(json, _jsonOptions) ?? new List<ElementoEstructuralDto>();
                }
            }
            catch { }

            return new List<ElementoEstructuralDto>();
        }

        public static async Task<(bool Success, string Message, ElementoEstructuralDto? Data)> CrearElementoEstructuralAsync(CrearElementoEstructuralRequest elemento)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return (false, "No autenticado.", null);

            try
            {
                string url = $"{UserSession.BaseUrl}/api/elementoeEstructural";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                SetAuthHeader(request);

                request.Content = new StringContent(
                    JsonSerializer.Serialize(elemento),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var creado = JsonSerializer.Deserialize<ElementoEstructuralDto>(json, _jsonOptions);
                    return (true, "Elemento estructural agregado.", creado);
                }

                string err = await response.Content.ReadAsStringAsync();
                return (false, $"Error: {err}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", null);
            }
        }

        public static async Task<(bool Success, string Message)> EliminarElementoEstructuralAsync(int idElemento)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return (false, "No autenticado.");

            try
            {
                string url = $"{UserSession.BaseUrl}/api/elementoeEstructural/{idElemento}";
                using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                SetAuthHeader(request);

                using var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode
                    ? (true, "Elemento eliminado.")
                    : (false, $"Error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        // ==================== MODELOS IMPORTADOS ====================

        public static async Task<List<ModeloImportadoDto>> GetModelosByVersionAsync(int idVersion)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return new List<ModeloImportadoDto>();

            try
            {
                string url = $"{UserSession.BaseUrl}/api/modeloImportado/version/{idVersion}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                SetAuthHeader(request);

                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<ModeloImportadoDto>>(json, _jsonOptions) ?? new List<ModeloImportadoDto>();
                }
            }
            catch { }

            return new List<ModeloImportadoDto>();
        }

        // ==================== HISTORIAL DE MEDICIONES ====================

        public static async Task<List<MedicionDto>> GetMedicionesByProyectoAsync(int idProyecto)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return new List<MedicionDto>();

            try
            {
                string url = $"{UserSession.BaseUrl}/api/Medicion/proyecto/{idProyecto}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                SetAuthHeader(request);

                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<MedicionDto>>(json, _jsonOptions) ?? new List<MedicionDto>();
                }
            }
            catch { }

            return new List<MedicionDto>();
        }

        public static async Task<(bool Success, string Message)> EliminarMedicionAsync(int idMedicion)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return (false, "No autenticado.");

            try
            {
                string url = $"{UserSession.BaseUrl}/api/Medicion/{idMedicion}";
                using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                SetAuthHeader(request);

                using var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode
                    ? (true, "Medición eliminada.")
                    : (false, $"Error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        // ==================== ACTUALIZACION DE PROYECTO & PERFIL ====================

        public static async Task<(bool Success, string Message)> ActualizarProyectoAsync(int idProyecto, ActualizarProyectoRequest proyecto)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return (false, "No autenticado.");

            try
            {
                string url = $"{UserSession.BaseUrl}/api/proyecto/{idProyecto}";
                using var request = new HttpRequestMessage(HttpMethod.Put, url);
                SetAuthHeader(request);

                request.Content = new StringContent(
                    JsonSerializer.Serialize(proyecto),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode
                    ? (true, "Proyecto actualizado correctamente.")
                    : (false, $"Error al actualizar ({response.StatusCode})");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public static async Task<(bool Success, string Message)> ActualizarUsuarioAsync(int idUsuario, ActualizarUsuarioRequest usuario)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return (false, "No autenticado.");

            try
            {
                string url = $"{UserSession.BaseUrl}/api/usuario/{idUsuario}";
                using var request = new HttpRequestMessage(HttpMethod.Put, url);
                SetAuthHeader(request);

                request.Content = new StringContent(
                    JsonSerializer.Serialize(usuario),
                    Encoding.UTF8,
                    "application/json");

                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    UserSession.Nombre = usuario.Nombre;
                    return (true, "Perfil actualizado con éxito.");
                }

                return (false, $"Error al actualizar perfil ({response.StatusCode})");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public static async Task<UsuarioDto?> GetUsuarioByIdAsync(int idUsuario)
        {
            if (string.IsNullOrEmpty(UserSession.Token)) return null;

            try
            {
                string url = $"{UserSession.BaseUrl}/api/usuario/{idUsuario}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                SetAuthHeader(request);

                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UsuarioDto>(json, _jsonOptions);
                }
            }
            catch { }

            return null;
        }
    }
}
