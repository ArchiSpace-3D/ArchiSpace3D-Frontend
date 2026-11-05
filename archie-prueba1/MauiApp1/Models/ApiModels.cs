using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MauiApp1.Models
{
    public sealed class NullableDecimalAsZeroConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return 0m;
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var d)) return d;
            if (reader.TokenType == JsonTokenType.String &&
                decimal.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
            return 0m;
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value);
    }

    public sealed class NullableBoolAsTrueConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return true;
            if (reader.TokenType == JsonTokenType.True) return true;
            if (reader.TokenType == JsonTokenType.False) return false;
            return true;
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
            writer.WriteBooleanValue(value);
    }

    public class LoginRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("contrasena")]
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

        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }

        [JsonPropertyName("direccion")]
        public string? Direccion { get; set; }

        [JsonPropertyName("tipodocumento")]
        public string? Tipodocumento { get; set; }

        [JsonPropertyName("numerodocumento")]
        public string? Numerodocumento { get; set; }

        [JsonPropertyName("avatarurl")]
        public string? Avatarurl { get; set; }
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
        public string Rol { get; set; } = "Arquitecto";

        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }

        [JsonPropertyName("direccion")]
        public string? Direccion { get; set; }

        [JsonPropertyName("tipodocumento")]
        public string? Tipodocumento { get; set; }

        [JsonPropertyName("numerodocumento")]
        public string? Numerodocumento { get; set; }

        [JsonPropertyName("avatarurl")]
        public string? Avatarurl { get; set; }
    }

    public class ProyectoDto : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propName));

        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("idarquitecto")]
        public int? Idarquitecto { get; set; }

        [JsonPropertyName("idcliente")]
        public int? Idcliente { get; set; }

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

        [JsonPropertyName("imagenurl")]
        public string? Imagenurl { get; set; }

        [JsonPropertyName("codigosalaactiva")]
        public string? Codigosalaactiva { get; set; }

        [JsonPropertyName("fechaaperturasala")]
        public DateTime? Fechaaperturasala { get; set; }

        [JsonPropertyName("fechacreacion")]
        public DateTime? Fechacreacion { get; set; }

        private string _resumenActividad = "Cargando actividad técnica...";
        [JsonIgnore]
        public string ResumenActividad
        {
            get => _resumenActividad;
            set
            {
                if (_resumenActividad != value)
                {
                    _resumenActividad = value;
                    OnPropertyChanged(nameof(ResumenActividad));
                }
            }
        }

        private bool _esDestacado;
        [JsonIgnore]
        public bool EsDestacado
        {
            get => _esDestacado;
            set
            {
                if (_esDestacado != value)
                {
                    _esDestacado = value;
                    OnPropertyChanged(nameof(EsDestacado));
                }
            }
        }

        public string FechaFormateada => Fechacreacion?.ToString("dd/MM/yyyy") ?? "Reciente";

        public string PrimeraLetra => string.IsNullOrWhiteSpace(Nombre)
            ? "?"
            : Nombre.Substring(0, 1).ToUpper();
    }

    public class CrearProyectoRequest
    {
        [JsonPropertyName("idarquitecto")]
        public int? Idarquitecto { get; set; }

        [JsonPropertyName("idcliente")]
        public int? Idcliente { get; set; }

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
    }

    public class ActualizarProyectoRequest
    {
        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("idcliente")]
        public int? Idcliente { get; set; }

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
    }

    public class SugerenciaDto
    {
        [JsonPropertyName("idsugerencia")]
        public int Idsugerencia { get; set; }

        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("idusuario")]
        public int Idusuario { get; set; }

        [JsonPropertyName("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [JsonPropertyName("estado")]
        public string? Estado { get; set; }

        [JsonPropertyName("fechacreacion")]
        public DateTime? Fechacreacion { get; set; }
    }

    public class CrearSugerenciaRequest
    {
        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("idusuario")]
        public int Idusuario { get; set; }

        [JsonPropertyName("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = string.Empty;
    }

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
        public string? Puntosreferencia { get; set; }

        [JsonPropertyName("orientacionazimuth")]
        public decimal? Orientacionazimuth { get; set; }

        [JsonPropertyName("fechacaptura")]
        public DateTime? Fechacaptura { get; set; }

        [JsonIgnore]
        public bool PuedeEliminar => Services.UserSession.EsArquitecto;

        [JsonIgnore]
        public string DimensionesTexto =>
            $"{(Largoaproximado ?? 0m):0.##}m × {(Anchoaproximado ?? 0m):0.##}m × {(Altoaproximado ?? 2.5m):0.##}m";
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
        public string? Puntosreferencia { get; set; } = "[]";

        [JsonPropertyName("orientacionazimuth")]
        public decimal? Orientacionazimuth { get; set; }

        [JsonPropertyName("fechacaptura")]
        public DateTime? Fechacaptura { get; set; }

        [JsonIgnore]
        public bool PuedeEliminar => Services.UserSession.EsArquitecto;

        [JsonIgnore]
        public string DimensionesTexto =>
            $"{(Largoaproximado ?? 0m):0.##}m × {(Anchoaproximado ?? 0m):0.##}m × {(Altoaproximado ?? 2.5m):0.##}m";
    }

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

        [JsonPropertyName("puntosreferencia")]
        public string? Puntosreferencia { get; set; }

        public string TituloVersion => $"Versión {Numeroversion}" + (Esactual == true ? " (Activa)" : "");
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

        [JsonPropertyName("puntosreferencia")]
        public string? Puntosreferencia { get; set; }
    }

    public class MarcarActualRequest
    {
        [JsonPropertyName("idProyecto")]
        public int IdProyecto { get; set; }
    }

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
    }

    public class CrearElementoEstructuralRequest
    {
        [JsonPropertyName("idversiondiseno")]
        public int Idversiondiseno { get; set; }

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [JsonPropertyName("material")]
        public string? Material { get; set; }

        [JsonPropertyName("posicionx")]
        public decimal? Posicionx { get; set; } = 0;

        [JsonPropertyName("posiciony")]
        public decimal? Posiciony { get; set; } = 0;

        [JsonPropertyName("posicionz")]
        public decimal? Posicionz { get; set; } = 0;

        [JsonPropertyName("dimensionancho")]
        public decimal? Dimensionancho { get; set; } = 0;

        [JsonPropertyName("dimensionalto")]
        public decimal? Dimensionalto { get; set; } = 0;

        [JsonPropertyName("dimensionprofundidad")]
        public decimal? Dimensionprofundidad { get; set; } = 0;
    }

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

    public class MediciónDto
    {
        [JsonPropertyName("idmedicion")]
        public int Idmedicion { get; set; }
        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }
        [JsonPropertyName("puntoinicial")]
        public string? Puntoinicial { get; set; }
        [JsonPropertyName("puntofinal")]
        public string? Puntofinal { get; set; }
        [JsonPropertyName("distancia")]
        [JsonConverter(typeof(NullableDecimalAsZeroConverter))]
        public decimal Distancia { get; set; }
        public string DistanciaFormateada => $"{Distancia.ToString("0.00", CultureInfo.InvariantCulture)} m";
        [JsonPropertyName("fechamedicion")]
        public DateTime? Fechamedicion { get; set; }
        public string FechaFormateada => Fechamedicion?.ToString("dd/MM/yyyy HH:mm") ?? "Reciente";

        [JsonPropertyName("etapa")]
        public string? Etapa { get; set; }
        [JsonPropertyName("partida")]
        public string? Partida { get; set; }
        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }
        [JsonPropertyName("veces")]
        public int? Veces { get; set; }
        [JsonPropertyName("largo")]
        public decimal? Largo { get; set; }
        [JsonPropertyName("ancho")]
        public decimal? Ancho { get; set; }
        [JsonPropertyName("alto")]
        public decimal? Alto { get; set; }
        [JsonPropertyName("unidad")]
        public string? Unidad { get; set; }
        [JsonPropertyName("totalparcial")]
        public decimal? Totalparcial { get; set; }

        [JsonIgnore]
        public bool PuedeEliminar => Services.UserSession.EsArquitecto;

        [JsonIgnore]
        public string DetalleResumen
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Descripcion)) return Descripcion;
                if (!string.IsNullOrWhiteSpace(Partida)) return Partida;
                if (Largo.HasValue || Ancho.HasValue)
                    return $"{(Largo ?? 0):0.##}m x {(Ancho ?? 0):0.##}m" + (Alto.HasValue && Alto > 0 ? $" x {Alto:0.##}m" : "");
                return FechaFormateada;
            }
        }
    }

    public class CrearMediciónRequest
    {
        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }
        [JsonPropertyName("puntoinicial")]
        public string? Puntoinicial { get; set; }
        [JsonPropertyName("puntofinal")]
        public string? Puntofinal { get; set; }
        [JsonPropertyName("distancia")]
        public decimal Distancia { get; set; }
        [JsonIgnore]
        public string DistanciaFormateada => $"{Distancia.ToString("0.00", CultureInfo.InvariantCulture)} m";
        [JsonPropertyName("fechamedicion")]
        public DateTime? Fechamedicion { get; set; }

        [JsonPropertyName("etapa")]
        public string? Etapa { get; set; }
        [JsonPropertyName("partida")]
        public string? Partida { get; set; }
        [JsonPropertyName("descripcion")]
        public string? Descripcion { get; set; }
        [JsonPropertyName("veces")]
        public int? Veces { get; set; }
        [JsonPropertyName("largo")]
        public decimal? Largo { get; set; }
        [JsonPropertyName("ancho")]
        public decimal? Ancho { get; set; }
        [JsonPropertyName("alto")]
        public decimal? Alto { get; set; }
        [JsonPropertyName("unidad")]
        public string? Unidad { get; set; }
        [JsonPropertyName("totalparcial")]
        public decimal? Totalparcial { get; set; }
    }

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

        [JsonPropertyName("avatarurl")]
        public string? Avatarurl { get; set; }

        [JsonPropertyName("activo")]
        [JsonConverter(typeof(NullableBoolAsTrueConverter))]
        public bool Activo { get; set; } = true;

        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();

        public string PrimeraLetra => string.IsNullOrWhiteSpace(Nombre)
            ? "?"
            : Nombre.Substring(0, 1).ToUpper();

        public string EstadoTexto => Activo ? "Activo" : "Inactivo";
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
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Contrasena { get; set; }

        [JsonPropertyName("rol")]
        public string Rol { get; set; } = string.Empty;

        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }

        [JsonPropertyName("direccion")]
        public string? Direccion { get; set; }

        [JsonPropertyName("tipodocumento")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Tipodocumento { get; set; }

        [JsonPropertyName("numerodocumento")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Numerodocumento { get; set; }

        [JsonPropertyName("avatarurl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Avatarurl { get; set; }

        [JsonPropertyName("activo")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Activo { get; set; }
    }

    public class CrearModeloImportadoRequest
    {
        [JsonPropertyName("idversiondiseno")]
        public int Idversiondiseno { get; set; }

        [JsonPropertyName("nombrearchivo")]
        public string Nombrearchivo { get; set; } = string.Empty;

        [JsonPropertyName("formato")]
        public string Formato { get; set; } = string.Empty;

        [JsonPropertyName("rutastorage")]
        public string Rutastorage { get; set; } = string.Empty;

        [JsonPropertyName("posicionx")]
        public decimal? Posicionx { get; set; } = 0;

        [JsonPropertyName("posiciony")]
        public decimal? Posiciony { get; set; } = 0;

        [JsonPropertyName("posicionz")]
        public decimal? Posicionz { get; set; } = 0;

        [JsonPropertyName("rotacionx")]
        public decimal? Rotacionx { get; set; } = 0;

        [JsonPropertyName("rotaciony")]
        public decimal? Rotaciony { get; set; } = 0;

        [JsonPropertyName("rotacionz")]
        public decimal? Rotacionz { get; set; } = 0;

        [JsonPropertyName("escalax")]
        public decimal? Escalax { get; set; } = 1;

        [JsonPropertyName("escalay")]
        public decimal? Escalay { get; set; } = 1;

        [JsonPropertyName("escalaz")]
        public decimal? Escalaz { get; set; } = 1;

        [JsonPropertyName("fechaimportacion")]
        public DateTime? Fechaimportacion { get; set; } = DateTime.UtcNow;
    }

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
}
