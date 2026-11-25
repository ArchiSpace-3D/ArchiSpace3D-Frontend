using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MauiApp1.Models;

namespace MauiApp1.Services
{
    public static class ApiService
    {
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(40)
        };

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static string ApiRoot => $"{UserSession.BaseUrl.TrimEnd('/')}/api";

        private static void AttachAuth(HttpRequestMessage request)
        {
            if (!string.IsNullOrEmpty(UserSession.Token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserSession.Token);
            }
        }

        public static void ClearAuthHeader()
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }

        public static async Task WarmUpServerAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await _http.GetAsync(UserSession.BaseUrl.TrimEnd('/'), cts.Token);
            }
            catch { }
        }

        private static string ExtractErrorMessage(string rawBody, int statusCode)
        {
            if (string.IsNullOrWhiteSpace(rawBody))
                return $"Error del servidor (HTTP {statusCode}).";

            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                        return msg.GetString() ?? rawBody;
                    if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                        return title.GetString() ?? rawBody;
                }
            }
            catch { }

            return rawBody.Trim('"');
        }

        private static async Task<T?> GetAsync<T>(string endpoint, bool requiresAuth = true)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiRoot}/{endpoint}");
                if (requiresAuth) AttachAuth(request);

                using var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode) return default;

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch
            {
                return default;
            }
        }

        private static async Task<List<T>> GetListAsync<T>(string endpoint) =>
            await GetAsync<List<T>>(endpoint) ?? new List<T>();

        private static async Task<(bool Success, string Message)> SendAsync(
            HttpMethod method,
            string endpoint,
            object? body = null,
            bool requiresAuth = true)
        {
            try
            {
                using var request = new HttpRequestMessage(method, $"{ApiRoot}/{endpoint}");
                if (requiresAuth) AttachAuth(request);

                if (body != null)
                {
                    var jsonBody = JsonSerializer.Serialize(body);
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                }

                using var response = await _http.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, ExtractErrorMessage(responseText, (int)response.StatusCode));

                return (true, "OK");
            }
            catch (TaskCanceledException)
            {
                return (false, "El servidor tardó demasiado en responder. Intenta de nuevo.");
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
        }

        private static async Task<(bool Success, string Message, TResponse? Data)> SendAsync<TResponse>(
            HttpMethod method,
            string endpoint,
            object? body = null,
            bool requiresAuth = true)
        {
            try
            {
                using var request = new HttpRequestMessage(method, $"{ApiRoot}/{endpoint}");
                if (requiresAuth) AttachAuth(request);

                if (body != null)
                {
                    var jsonBody = JsonSerializer.Serialize(body);
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                }

                using var response = await _http.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, ExtractErrorMessage(responseText, (int)response.StatusCode), default);

                if (string.IsNullOrWhiteSpace(responseText))
                    return (true, "OK", default);

                var data = JsonSerializer.Deserialize<TResponse>(responseText, _jsonOptions);
                return (true, "OK", data);
            }
            catch (TaskCanceledException)
            {
                return (false, "El servidor tardó demasiado en responder. Intenta de nuevo.", default);
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}", default);
            }
        }

        private static async Task<(bool Success, string Message, string? Url)> UploadImageAsync(string endpoint, FileResult fileResult)
        {
            try
            {
                await using var stream = await fileResult.OpenReadAsync();
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiRoot}/{endpoint}");
                AttachAuth(request);

                using var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(stream);
                string ext = Path.GetExtension(fileResult.FileName).ToLowerInvariant();
                string mimeType = ext switch
                {
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    _ => "image/jpeg"
                };
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                content.Add(fileContent, "file", fileResult.FileName);
                request.Content = content;

                using var response = await _http.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, ExtractErrorMessage(body, (int)response.StatusCode), null);

                using var doc = JsonDocument.Parse(body);
                var url = doc.RootElement.TryGetProperty("url", out var prop) ? prop.GetString() : null;
                return (true, "Imagen actualizada.", url);
            }
            catch (Exception ex)
            {
                return (false, $"Error subiendo imagen: {ex.Message}", null);
            }
        }

        // ─── Autenticación y Usuarios ──────────────────────────────────────

        public static async Task<(bool Success, string Message, LoginResponse? Data)> LoginAsync(string email, string contrasena)
        {
            var result = await SendAsync<LoginResponse>(
                HttpMethod.Post,
                "usuario/login",
                new LoginRequest { Email = email, Contrasena = contrasena },
                requiresAuth: false);

            if (result.Success && result.Data != null)
                await UserSession.SetSessionAsync(result.Data, UserSession.BaseUrl);

            return result;
        }

        public static Task<(bool Success, string Message, LoginResponse? Data)> LoginAsync(string _, string email, string contrasena) =>
            LoginAsync(email, contrasena);

        public static async Task<(bool Success, string Message, LoginResponse? Data)> GoogleLoginAsync(string accessToken)
        {
            var result = await SendAsync<LoginResponse>(
                HttpMethod.Post,
                "usuario/google-login",
                new { accessToken },
                requiresAuth: false);

            if (result.Success && result.Data != null)
                await UserSession.SetSessionAsync(result.Data, UserSession.BaseUrl);

            return result;
        }

        public static Task<(bool Success, string Message)> RegistrarUsuarioAsync(UsuarioRegistroRequest request) =>
            SendAsync(HttpMethod.Post, "usuario", request, requiresAuth: false);

        public static Task<(bool Success, string Message)> RegistrarUsuarioAsync(string _, UsuarioRegistroRequest request) =>
            RegistrarUsuarioAsync(request);

        public static Task<UsuarioDto?> GetUsuarioByIdAsync(int idUsuario) =>
            GetAsync<UsuarioDto>($"usuario/{idUsuario}");

        public static Task<List<UsuarioDto>> GetUsuariosAsync() =>
            GetListAsync<UsuarioDto>("usuario");

        public static Task<(bool Success, string Message)> ActualizarUsuarioAsync(int idUsuario, ActualizarUsuarioRequest request) =>
            SendAsync(HttpMethod.Put, $"usuario/{idUsuario}", request);

        public static Task<(bool Success, string Message)> EliminarUsuarioAsync(int idUsuario) =>
            SendAsync(HttpMethod.Delete, $"usuario/{idUsuario}");

        public static async Task<(bool Success, string Message)> SubirAvatarUsuarioAsync(int idUsuario, FileResult fileResult)
        {
            var (ok, msg, url) = await UploadImageAsync($"usuario/{idUsuario}/avatar", fileResult);
            if (ok && !string.IsNullOrEmpty(url) && idUsuario == UserSession.Idusuario)
            {
                UserSession.Avatarurl = url;
                Preferences.Default.Set("user_avatar", url);
            }
            return (ok, msg);
        }

        public static async Task<bool> RegistrarFcmTokenAsync(int idUsuario, string fcmToken)
        {
            var (ok, _) = await SendAsync(
                HttpMethod.Post,
                $"usuario/{idUsuario}/fcm-token",
                new { idusuario = idUsuario, token = fcmToken });
            return ok;
        }

        // ─── Proyectos ─────────────────────────────────────────────────────

        public static Task<List<ProyectoDto>> GetProyectosAsync() =>
            GetListAsync<ProyectoDto>("proyecto");

        public static Task<ProyectoDto?> GetProyectoByIdAsync(int idProyecto) =>
            GetAsync<ProyectoDto>($"proyecto/{idProyecto}");

        public static Task<List<ProyectoDto>> GetProyectosByArquitectoAsync(int idArquitecto) =>
            GetListAsync<ProyectoDto>($"proyecto/arquitecto/{idArquitecto}");

        public static Task<List<ProyectoDto>> GetProyectosByClienteAsync(int idCliente) =>
            GetListAsync<ProyectoDto>($"proyecto/cliente/{idCliente}");

        public static Task<(bool Success, string Message, ProyectoDto? Data)> CrearProyectoAsync(CrearProyectoRequest request) =>
            SendAsync<ProyectoDto>(HttpMethod.Post, "proyecto", request);

        public static Task<(bool Success, string Message)> ActualizarProyectoAsync(int id, ActualizarProyectoRequest request) =>
            SendAsync(HttpMethod.Put, $"proyecto/{id}", request);

        public static Task<(bool Success, string Message)> EliminarProyectoAsync(int idProyecto) =>
            SendAsync(HttpMethod.Delete, $"proyecto/{idProyecto}");

        public static async Task<(bool Success, string? Codigo)> ActivarSalaAsync(int idProyecto)
        {
            var (ok, _, doc) = await SendAsync<JsonElement>(HttpMethod.Post, $"proyecto/{idProyecto}/activar-sala");
            if (!ok || doc.ValueKind != JsonValueKind.Object) return (false, null);
            var codigo = doc.TryGetProperty("codigo", out var prop) ? prop.GetString() : null;
            return (!string.IsNullOrWhiteSpace(codigo), codigo);
        }

        public static Task<(bool Success, string Message, string? Url)> SubirImagenProyectoAsync(int idProyecto, FileResult fileResult) =>
            UploadImageAsync($"proyecto/{idProyecto}/imagen", fileResult);

        // ─── Espacios Físicos ──────────────────────────────────────────────

        public static Task<EspacioFisicoDto?> GetEspacioFisicoByProyectoAsync(int idProyecto) =>
            GetAsync<EspacioFisicoDto>($"espacioFisico/proyecto/{idProyecto}");

        public static async Task<List<EspacioFisicoDto>> GetEspaciosPorProyectoListaAsync(int idProyecto)
        {
            var lista = await GetAsync<List<EspacioFisicoDto>>($"espacioFisico/proyecto/{idProyecto}/lista");
            if (lista != null) return lista;

            var unico = await GetEspacioFisicoByProyectoAsync(idProyecto);
            return unico != null ? new List<EspacioFisicoDto> { unico } : new List<EspacioFisicoDto>();
        }

        public static Task<(bool Success, string Message, EspacioFisicoDto? Data)> GuardarEspacioFisicoAsync(CrearEspacioFisicoRequest request) =>
            SendAsync<EspacioFisicoDto>(HttpMethod.Post, "espacioFisico", request);

        public static Task<(bool Success, string Message)> ActualizarEspacioFisicoAsync(int id, EspacioFisicoDto espacio) =>
            SendAsync(HttpMethod.Put, $"espacioFisico/{id}", espacio);

        public static Task<(bool Success, string Message)> EliminarEspacioFisicoAsync(int id) =>
            SendAsync(HttpMethod.Delete, $"espacioFisico/{id}");

        // ─── Versiones de Diseño y Plano CAD ───────────────────────────────

        public static Task<List<VersionDisenoDto>> GetVersionesByProyectoAsync(int idProyecto) =>
            GetListAsync<VersionDisenoDto>($"versionDiseno/proyecto/{idProyecto}");

        public static Task<VersionDisenoDto?> GetVersionActualAsync(int idProyecto) =>
            GetAsync<VersionDisenoDto>($"versionDiseno/proyecto/{idProyecto}/actual");

        public static Task<(bool Success, string Message, VersionDisenoDto? Data)> GuardarVersionAsync(CrearVersionDisenoRequest request) =>
            SendAsync<VersionDisenoDto>(HttpMethod.Post, "versionDiseno", request);

        public static Task<(bool Success, string Message)> MarcarVersionComoActualAsync(int idVersion, int idProyecto) =>
            SendAsync(HttpMethod.Put, $"versionDiseno/{idVersion}/actual", new MarcarActualRequest { IdProyecto = idProyecto });

        public static Task<(bool Success, string Message)> EliminarVersionAsync(int id) =>
            SendAsync(HttpMethod.Delete, $"versionDiseno/{id}");

        public static bool EsJsonPlanoValido(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var trimmed = raw.Trim();
            return trimmed.StartsWith("{") &&
                   trimmed != "{}" &&
                   (trimmed.Contains("points") || trimmed.Contains("modules") || trimmed.Contains("walls"));
        }

        public static async Task<string?> ObtenerPlanoProyectoAsync(int idProyecto)
        {
            var versiones = await GetVersionesByProyectoAsync(idProyecto);
            if (versiones.Count > 0)
            {
                var candidata = versiones
                    .OrderByDescending(v => v.Esactual == true)
                    .ThenByDescending(v => v.Numeroversion)
                    .FirstOrDefault(v => EsJsonPlanoValido(v.Puntosreferencia));

                if (candidata != null)
                    return candidata.Puntosreferencia;
            }

            var espacio = await GetEspacioFisicoByProyectoAsync(idProyecto);
            return EsJsonPlanoValido(espacio?.Puntosreferencia) ? espacio!.Puntosreferencia : null;
        }

        public static async Task<(bool Success, string Message, int NumeroVersion)> GuardarPlanoProyectoAsync(
            int idProyecto,
            string planJson)
        {
            var versiones = await GetVersionesByProyectoAsync(idProyecto);
            int nextVersion = versiones.Count > 0 ? versiones.Max(v => v.Numeroversion) + 1 : 1;

            var nuevaVersion = new CrearVersionDisenoRequest
            {
                Idproyecto = idProyecto,
                Numeroversion = nextVersion,
                Esactual = true,
                Fechacreacion = DateTime.UtcNow,
                Puntosreferencia = planJson
            };
            var (verOk, verMsg, nuevaVer) = await GuardarVersionAsync(nuevaVersion);

            if (verOk && nuevaVer != null && nuevaVer.Idversiondiseno > 0)
            {
                await MarcarVersionComoActualAsync(nuevaVer.Idversiondiseno, idProyecto);
            }

            var existente = await GetEspacioFisicoByProyectoAsync(idProyecto);
            bool espOk;
            string espMsg;

            if (existente != null && (existente.Descripcion == "__PLANO_CAD__" || string.IsNullOrWhiteSpace(existente.Descripcion)))
            {
                existente.Descripcion = "__PLANO_CAD__";
                existente.Puntosreferencia = planJson;
                (espOk, espMsg) = await ActualizarEspacioFisicoAsync(existente.Idespaciofisico, existente);
            }
            else
            {
                var req = new CrearEspacioFisicoRequest
                {
                    Idproyecto = idProyecto,
                    Descripcion = "__PLANO_CAD__",
                    Puntosreferencia = planJson
                };
                var (okCrear, msgCrear, _) = await GuardarEspacioFisicoAsync(req);
                if (!okCrear && existente != null)
                {
                    existente.Puntosreferencia = planJson;
                    (espOk, espMsg) = await ActualizarEspacioFisicoAsync(existente.Idespaciofisico, existente);
                }
                else
                {
                    espOk = okCrear;
                    espMsg = msgCrear;
                }
            }

            bool ok = verOk || espOk;
            return (ok, ok ? "OK" : $"{verMsg} / {espMsg}", nextVersion);
        }

        // ─── Invitaciones ──────────────────────────────────────────────────

        public static Task<(bool Success, string Message, InvitacionDto? Data)> CrearInvitacionAsync(
            int idProyecto,
            string codigo,
            DateTime? fechaExpiracion = null) =>
            SendAsync<InvitacionDto>(
                HttpMethod.Post,
                "invitacion",
                new CrearInvitacionRequest
                {
                    Idproyecto = idProyecto,
                    Idarquitecto = UserSession.Idusuario,
                    Codigo = codigo.Trim().ToUpperInvariant(),
                    Fechaexpiracion = fechaExpiracion
                });

        public static Task<(bool Success, string Message)> UsarInvitacionAsync(string codigo) =>
            SendAsync(
                HttpMethod.Put,
                $"invitacion/codigo/{Uri.EscapeDataString(codigo.Trim().ToUpperInvariant())}/usar",
                new UsarInvitacionRequest { IdClienteUsado = UserSession.Idusuario });

        // ─── Mediciones ────────────────────────────────────────────────────

        public static Task<List<MedicionDto>> GetMedicionesByProyectoAsync(int idProyecto) =>
            GetListAsync<MedicionDto>($"Medicion/proyecto/{idProyecto}");

        public static Task<(bool Success, string Message)> GuardarMedicionAsync(CrearMedicionRequest request) =>
            SendAsync(HttpMethod.Post, "Medicion", request);

        public static Task<(bool Success, string Message)> EliminarMedicionAsync(int id) =>
            SendAsync(HttpMethod.Delete, $"Medicion/{id}");

        // ─── Notificaciones ────────────────────────────────────────────────

        public static Task<List<NotificacionDto>> GetNotificacionesAsync() =>
            GetListAsync<NotificacionDto>("notificacion/mis-notificaciones");

        public static Task<(bool Success, string Message)> MarcarNotificacionLeidaAsync(int idNotificacion) =>
            SendAsync(HttpMethod.Put, $"notificacion/{idNotificacion}/leer");

        public static Task<(bool Success, string Message)> EliminarNotificacionAsync(int id) =>
            SendAsync(HttpMethod.Delete, $"notificacion/{id}");

        // ─── Sugerencias ───────────────────────────────────────────────────

        public static Task<List<SugerenciaDto>> GetSugerenciasByProyectoAsync(int idProyecto) =>
            GetListAsync<SugerenciaDto>($"Sugerencia/proyecto/{idProyecto}");

        public static Task<(bool Success, string Message, SugerenciaDto? Data)> CrearSugerenciaAsync(CrearSugerenciaRequest request) =>
            SendAsync<SugerenciaDto>(HttpMethod.Post, "Sugerencia", request);

        public static Task<(bool Success, string Message)> ActualizarEstadoSugerenciaAsync(int idSugerencia, string nuevoEstado) =>
            SendAsync(HttpMethod.Put, $"Sugerencia/{idSugerencia}/estado", nuevoEstado);

        public static Task<(bool Success, string Message)> EliminarSugerenciaAsync(int idSugerencia) =>
            SendAsync(HttpMethod.Delete, $"Sugerencia/{idSugerencia}");
    }
}