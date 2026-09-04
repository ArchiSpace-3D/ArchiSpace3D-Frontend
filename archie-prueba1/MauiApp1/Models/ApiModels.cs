using System;
using System.Text.Json.Serialization;

namespace MauiApp1.Models
{
    public class LoginRequest
    {
        [JsonPropertyName("Email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("Contrasena")]
        public string Contrasena { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("idusuario")]
        public int Idusuario { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("apellido")]
        public string Apellido { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("rol")]
        public string Rol { get; set; } = string.Empty;
    }

    public class UsuarioRegistroRequest
    {
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("apellido")]
        public string Apellido { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("contrasena")]
        public string Contrasena { get; set; } = string.Empty;

        [JsonPropertyName("rol")]
        public string Rol { get; set; } = "Arquitecto"; // "Arquitecto" o "Cliente"

        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }

        [JsonPropertyName("direccion")]
        public string? Direccion { get; set; }

        [JsonPropertyName("tipodocumento")]
        public string? Tipodocumento { get; set; }

        [JsonPropertyName("numerodocumento")]
        public string? Numerodocumento { get; set; }
    }

    public class ProyectoDto
    {
        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("idarquitecto")]
        public int Idarquitecto { get; set; }

        [JsonPropertyName("idcliente")]
        public int Idcliente { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("ubicacion")]
        public string? Ubicacion { get; set; }

        [JsonPropertyName("estado")]
        public string? Estado { get; set; }

        [JsonPropertyName("presupuesto")]
        public decimal? Presupuesto { get; set; }

        [JsonPropertyName("codigosalaactiva")]
        public string? Codigosalaactiva { get; set; }

        [JsonPropertyName("fechaactualizacion")]
        public DateTime? Fechaactualizacion { get; set; }

        [JsonPropertyName("fechacreacion")]
        public DateTime? Fechacreacion { get; set; }

        // Helpers de presentacion en UI
        public string FechaFormateada => Fechaactualizacion?.ToString("dd/MM/yyyy") ?? Fechacreacion?.ToString("dd/MM/yyyy") ?? "Reciente";
        public string EstadoNormalizado => string.IsNullOrWhiteSpace(Estado) ? "En progreso" : Estado;
        public string PresupuestoFormateado => Presupuesto.HasValue ? $"${Presupuesto.Value:N2}" : "$0.00";
    }

    public class CrearProyectoRequest
    {
        [JsonPropertyName("idarquitecto")]
        public int Idarquitecto { get; set; }

        [JsonPropertyName("idcliente")]
        public int Idcliente { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("ubicacion")]
        public string? Ubicacion { get; set; }

        [JsonPropertyName("estado")]
        public string? Estado { get; set; } = "En progreso";

        [JsonPropertyName("presupuesto")]
        public decimal? Presupuesto { get; set; }

        [JsonPropertyName("codigosalaactiva")]
        public string? Codigosalaactiva { get; set; }
    }

    public class ActualizarProyectoRequest
    {
        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("idarquitecto")]
        public int Idarquitecto { get; set; }

        [JsonPropertyName("idcliente")]
        public int Idcliente { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("ubicacion")]
        public string? Ubicacion { get; set; }

        [JsonPropertyName("estado")]
        public string? Estado { get; set; }

        [JsonPropertyName("presupuesto")]
        public decimal? Presupuesto { get; set; }

        [JsonPropertyName("codigosalaactiva")]
        public string? Codigosalaactiva { get; set; }
    }

    // ==================== ESPACIO FISICO ====================
    public class EspacioFisicoDto
    {
        [JsonPropertyName("idespaciofisico")]
        public int Idespaciofisico { get; set; }

        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("anchoaproximado")]
        public decimal? Anchoaproximado { get; set; }

        [JsonPropertyName("largoaproximado")]
        public decimal? Largoaproximado { get; set; }

        [JsonPropertyName("altoaproximado")]
        public decimal? Altoaproximado { get; set; }

        [JsonPropertyName("puntosreferencia")]
        public string Puntosreferencia { get; set; } = "{}";

        [JsonPropertyName("orientacionazimuth")]
        public decimal? Orientacionazimuth { get; set; }

        [JsonPropertyName("fechacaptura")]
        public DateTime? Fechacaptura { get; set; }

        // Calculos arquitectonicos en UI
        public decimal AreaCalculada => (Anchoaproximado ?? 0) * (Largoaproximado ?? 0);
        public decimal VolumenCalculado => AreaCalculada * (Altoaproximado ?? 0);
    }

    public class CrearEspacioFisicoRequest
    {
        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }

        [JsonPropertyName("anchoaproximado")]
        public decimal? Anchoaproximado { get; set; }

        [JsonPropertyName("largoaproximado")]
        public decimal? Largoaproximado { get; set; }

        [JsonPropertyName("altoaproximado")]
        public decimal? Altoaproximado { get; set; }

        [JsonPropertyName("puntosreferencia")]
        public string Puntosreferencia { get; set; } = "{}";

        [JsonPropertyName("orientacionazimuth")]
        public decimal? Orientacionazimuth { get; set; }

        [JsonPropertyName("fechacaptura")]
        public DateTime? Fechacaptura { get; set; } = DateTime.UtcNow;
    }

    // ==================== VERSIONES DE DISENO ====================
    public class VersionDisenoDto
    {
        [JsonPropertyName("idversiondiseno")]
        public int Idversiondiseno { get; set; }

        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("numeroversion")]
        public int Numeroversion { get; set; }

        [JsonPropertyName("esactual")]
        public bool? Esactual { get; set; }

        [JsonPropertyName("fechacreacion")]
        public DateTime? Fechacreacion { get; set; }

        public string TituloVersion => $"Version {Numeroversion}" + (Esactual == true ? " (Activa)" : "");
        public string FechaFormateada => Fechacreacion?.ToString("dd/MM/yyyy HH:mm") ?? "Reciente";
    }

    public class CrearVersionDisenoRequest
    {
        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("numeroversion")]
        public int Numeroversion { get; set; }

        [JsonPropertyName("esactual")]
        public bool? Esactual { get; set; } = true;

        [JsonPropertyName("fechacreacion")]
        public DateTime? Fechacreacion { get; set; } = DateTime.UtcNow;
    }

    public class MarcarActualRequest
    {
        [JsonPropertyName("idProyecto")]
        public int IdProyecto { get; set; }
    }

    // ==================== ELEMENTOS ESTRUCTURALES ====================
    public class ElementoEstructuralDto
    {
        [JsonPropertyName("idelementoestructural")]
        public int Idelementoestructural { get; set; }

        [JsonPropertyName("idversiondiseno")]
        public int Idversiondiseno { get; set; }

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [JsonPropertyName("material")]
        public string? Material { get; set; }

        [JsonPropertyName("posicionx")]
        public decimal? Posicionx { get; set; }

        [JsonPropertyName("posiciony")]
        public decimal? Posiciony { get; set; }

        [JsonPropertyName("posicionz")]
        public decimal? Posicionz { get; set; }

        [JsonPropertyName("dimensionancho")]
        public decimal? Dimensionancho { get; set; }

        [JsonPropertyName("dimensionalto")]
        public decimal? Dimensionalto { get; set; }

        [JsonPropertyName("dimensionprofundidad")]
        public decimal? Dimensionprofundidad { get; set; }

        public string DimensionesTexto => $"{Dimensionancho:0.##} x {Dimensionalto:0.##} x {Dimensionprofundidad:0.##} m";
    }

    public class CrearElementoEstructuralRequest
    {
        [JsonPropertyName("idversiondiseno")]
        public int Idversiondiseno { get; set; }

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = "Muro";

        [JsonPropertyName("material")]
        public string? Material { get; set; }

        [JsonPropertyName("posicionx")]
        public decimal? Posicionx { get; set; } = 0;

        [JsonPropertyName("posiciony")]
        public decimal? Posiciony { get; set; } = 0;

        [JsonPropertyName("posicionz")]
        public decimal? Posicionz { get; set; } = 0;

        [JsonPropertyName("dimensionancho")]
        public decimal? Dimensionancho { get; set; }

        [JsonPropertyName("dimensionalto")]
        public decimal? Dimensionalto { get; set; }

        [JsonPropertyName("dimensionprofundidad")]
        public decimal? Dimensionprofundidad { get; set; }
    }

    // ==================== MODELOS IMPORTADOS ====================
    public class ModeloImportadoDto
    {
        [JsonPropertyName("idmodeloimportado")]
        public int Idmodeloimportado { get; set; }

        [JsonPropertyName("idversiondiseno")]
        public int Idversiondiseno { get; set; }

        [JsonPropertyName("nombrearchivo")]
        public string Nombrearchivo { get; set; } = string.Empty;

        [JsonPropertyName("formato")]
        public string Formato { get; set; } = string.Empty;

        [JsonPropertyName("rutastorage")]
        public string Rutastorage { get; set; } = string.Empty;

        [JsonPropertyName("posicionx")]
        public decimal? Posicionx { get; set; }

        [JsonPropertyName("posiciony")]
        public decimal? Posiciony { get; set; }

        [JsonPropertyName("posicionz")]
        public decimal? Posicionz { get; set; }

        [JsonPropertyName("rotacionx")]
        public decimal? Rotacionx { get; set; }

        [JsonPropertyName("rotaciony")]
        public decimal? Rotaciony { get; set; }

        [JsonPropertyName("rotacionz")]
        public decimal? Rotacionz { get; set; }

        [JsonPropertyName("escalax")]
        public decimal? Escalax { get; set; }

        [JsonPropertyName("escalay")]
        public decimal? Escalay { get; set; }

        [JsonPropertyName("escalaz")]
        public decimal? Escalaz { get; set; }

        [JsonPropertyName("fechaimportacion")]
        public DateTime? Fechaimportacion { get; set; }
    }

    // ==================== INVITACIONES ====================
    public class InvitacionDto
    {
        [JsonPropertyName("idinvitacion")]
        public int Idinvitacion { get; set; }

        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("idarquitecto")]
        public int Idarquitecto { get; set; }

        [JsonPropertyName("idclienteusado")]
        public int? Idclienteusado { get; set; }

        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("usada")]
        public bool? Usada { get; set; }

        [JsonPropertyName("fechacreacion")]
        public DateTime? Fechacreacion { get; set; }

        [JsonPropertyName("fechaexpiracion")]
        public DateTime? Fechaexpiracion { get; set; }
    }

    public class CrearInvitacionRequest
    {
        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("idarquitecto")]
        public int Idarquitecto { get; set; }

        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("fechaexpiracion")]
        public DateTime? Fechaexpiracion { get; set; }
    }

    public class UsarInvitacionRequest
    {
        [JsonPropertyName("idClienteUsado")]
        public int IdClienteUsado { get; set; }
    }

    // ==================== MEDICIONES ====================
    public class MedicionDto
    {
        [JsonPropertyName("idmedicion")]
        public int Idmedicion { get; set; }

        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("puntoinicial")]
        public string Puntoinicial { get; set; } = "{}";

        [JsonPropertyName("puntofinal")]
        public string Puntofinal { get; set; } = "{}";

        [JsonPropertyName("distancia")]
        public decimal Distancia { get; set; }

        [JsonPropertyName("fechamedicion")]
        public DateTime? Fechamedicion { get; set; }

        public string FechaFormateada => Fechamedicion?.ToString("dd/MM/yyyy HH:mm") ?? "Reciente";
        public string DistanciaFormateada => $"{Distancia:N2} m";
    }

    public class CrearMedicionRequest
    {
        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("puntoinicial")]
        public string Puntoinicial { get; set; } = "{}";

        [JsonPropertyName("puntofinal")]
        public string Puntofinal { get; set; } = "{}";

        [JsonPropertyName("distancia")]
        public decimal Distancia { get; set; }

        [JsonPropertyName("fechamedicion")]
        public DateTime? Fechamedicion { get; set; }
    }

    // ==================== NOTIFICACIONES ====================
    public class NotificacionDto
    {
        [JsonPropertyName("idnotificacion")]
        public int Idnotificacion { get; set; }

        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("idversiondiseno")]
        public int? Idversiondiseno { get; set; }

        [JsonPropertyName("tipo")]
        public string? Tipo { get; set; }

        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [JsonPropertyName("leida")]
        public bool? Leida { get; set; }

        [JsonPropertyName("fechaenvio")]
        public DateTime? Fechaenvio { get; set; }

        public string FechaFormateada => Fechaenvio?.ToString("dd/MM HH:mm") ?? "Reciente";
        public string TipoNormalizado => string.IsNullOrWhiteSpace(Tipo) ? "General" : Tipo;
    }

    // ==================== USUARIOS ====================
    public class UsuarioDto
    {
        [JsonPropertyName("idusuario")]
        public int Idusuario { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("apellido")]
        public string Apellido { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("rol")]
        public string Rol { get; set; } = string.Empty;

        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }

        [JsonPropertyName("direccion")]
        public string? Direccion { get; set; }

        [JsonPropertyName("tipodocumento")]
        public string? Tipodocumento { get; set; }

        [JsonPropertyName("numerodocumento")]
        public string? Numerodocumento { get; set; }

        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
    }

    public class ActualizarUsuarioRequest
    {
        [JsonPropertyName("idusuario")]
        public int Idusuario { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("apellido")]
        public string Apellido { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("contrasena")]
        public string? Contrasena { get; set; }

        [JsonPropertyName("rol")]
        public string Rol { get; set; } = string.Empty;

        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }

        [JsonPropertyName("direccion")]
        public string? Direccion { get; set; }

        [JsonPropertyName("tipodocumento")]
        public string? Tipodocumento { get; set; }

        [JsonPropertyName("numerodocumento")]
        public string? Numerodocumento { get; set; }
    }
}
