using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MauiApp1.Views;

/// <summary>
/// Envuelve un SugerenciaDto con todo lo que la fila necesita (zona, icono, colores, fecha).
/// No cambia el modelo ni la API.
/// </summary>
public sealed class SugerenciaItem
{
    private static readonly Dictionary<string, string> Iconos = new()
    {
        ["kitchen"] = "M320-640v-120h80v120h-80Zm0 360v-200h80v200h-80ZM240-80q-33 0-56.5-23.5T160-160v-640q0-33 23.5-56.5T240-880h480q33 0 56.5 23.5T800-800v640q0 33-23.5 56.5T720-80H240Zm0-80h480v-360H240v360Zm0-440h480v-200H240v200Z",
        ["deck"] = "M440-80v-520H80l400-280 400 280H520v520h-80Zm40-600h146-292 146ZM120-80v-210L88-466l78-14 30 160h164v240h-80v-160h-80v160h-80Zm480 0v-240h164l30-160 78 14-32 176v210h-80v-160h-80v160h-80ZM334-680h292L480-782 334-680Z",
        ["bed"] = "M80-200v-240q0-27 11-49t29-39v-112q0-50 35-85t85-35h160q23 0 43 8.5t37 23.5q17-15 37-23.5t43-8.5h160q50 0 85 35t35 85v112q18 17 29 39t11 49v240h-80v-80H160v80H80Zm440-360h240v-80q0-17-11.5-28.5T720-680H560q-17 0-28.5 11.5T520-640v80Zm-320 0h240v-80q0-17-11.5-28.5T400-680H240q-17 0-28.5 11.5T200-640v80Zm-40 200h640v-80q0-17-11.5-28.5T760-480H200q-17 0-28.5 11.5T160-440v80Zm640 0H160h640Z",
        ["weekend"] = "M160-680q0-50 35-85t85-35h400q50 0 85 35t35 85v80q50 0 85 35t35 85v200q0 50-35 85t-85 35H160q-50 0-85-35t-35-85v-200q0-50 35-85t85-35v-80Zm80 0v112q18 17 29 39t11 49v80h400v-80q0-27 11-49t29-39v-112q0-17-11.5-28.5T680-720H280q-17 0-28.5 11.5T240-680Zm520 360H200v-160q0-8-3-15.5t-8.5-13q-5.5-5.5-13-8.5t-15.5-3q-17 0-28.5 11.5T120-480v200q0 17 11.5 28.5T160-240h640q17 0 28.5-11.5T840-280v-200q0-17-11.5-28.5T800-520q-8 0-15.5 3t-13 8.5q-5.5 5.5-8.5 13t-3 15.5v160Zm-280 0Zm0 80Zm0-160Z",
        ["bathtub"] = "M280-600q-33 0-56.5-23.5T200-680q0-33 23.5-56.5T280-760q33 0 56.5 23.5T360-680q0 33-23.5 56.5T280-600ZM200-80q-17 0-28.5-11.5T160-120q-33 0-56.5-23.5T80-200v-240h120v-30q0-38 26-64t64-26q20 0 37 8t31 22l56 62q8 8 15.5 15t16.5 13h274v-326q0-14-10-24t-24-10q-6 0-11.5 2.5T664-790l-50 50q5 17 2 33.5T604-676L494-788q14-9 30-11.5t32 3.5l50-50q16-16 36.5-25t43.5-9q48 0 81 33t33 81v326h80v240q0 33-23.5 56.5T800-120q0 17-11.5 28.5T760-80H200Zm-40-120h640v-160H160v160Zm0 0h640-640Z",
        ["checkroom"] = "M120-160q-17 0-28.5-11.5T80-200q0-10 4-18.5T96-232l344-258v-70q0-17 12-28.5t29-11.5q25 0 42-18t17-43q0-25-17.5-42T480-720q-25 0-42.5 17.5T420-660h-80q0-58 41-99t99-41q58 0 99 40.5t41 98.5q0 47-27.5 84T520-526v36l344 258q8 5 12 13.5t4 18.5q0 17-11.5 28.5T840-160H120Zm120-80h480L480-420 240-240Z",
        ["door_front"] = "M120-120v-80h80v-560q0-33 23.5-56.5T280-840h400q33 0 56.5 23.5T760-760v560h80v80H120Zm560-80v-560H280v560h400ZM560-440q17 0 28.5-11.5T600-480q0-17-11.5-28.5T560-520q-17 0-28.5 11.5T520-480q0 17 11.5 28.5T560-440ZM280-760v560-560Z",
        ["window"] = "M200-120q-33 0-56.5-23.5T120-200v-560q0-33 23.5-56.5T200-840h560q33 0 56.5 23.5T840-760v560q0 33-23.5 56.5T760-120H200Zm320-320v240h240v-240H520Zm0-80h240v-240H520v240Zm-80 0v-240H200v240h240Zm0 80H200v240h240v-240Z",
        ["location_on"] = "M480-480q33 0 56.5-23.5T560-560q0-33-23.5-56.5T480-640q-33 0-56.5 23.5T400-560q0 33 23.5 56.5T480-480Zm0 294q122-112 181-203.5T720-552q0-109-69.5-178.5T480-800q-101 0-170.5 69.5T240-552q0 71 59 162.5T480-186Zm0 106Q319-217 239.5-334.5T160-552q0-150 96.5-239T480-880q127 0 223.5 89T800-552q0 100-79.5 217.5T480-80Zm0-480Z",
    };

    public SugerenciaDto Dto { get; }

    public string Zona { get; }
    public string Texto { get; }
    public string Meta { get; }
    public string EstadoKey { get; }      // aprobada | revision | rechazada
    public string EstadoTexto { get; }
    public bool MostrarAcciones { get; }
    public bool MostrarDivisor { get; set; }

    public Geometry? IconoGeometry { get; }
    public Brush IconoFill { get; }
    public Color TileBg { get; }
    public Color ChipBg { get; }
    public Color ChipText { get; }

    public SugerenciaItem(SugerenciaDto dto, bool oscuro)
    {
        Dto = dto;
        (Zona, Texto) = Separar(dto.Titulo, dto.Descripcion);

        string estado = (dto.Estado ?? "").Trim().ToLowerInvariant();
        Color iconColor;
        switch (estado)
        {
            case "aprobada":
                EstadoKey = "aprobada";
                EstadoTexto = "Aprobada";
                TileBg = Color.FromArgb(oscuro ? "#3C4961" : "#D2E0FE");
                ChipBg = TileBg;
                ChipText = Color.FromArgb(oscuro ? "#A9C7FF" : "#002753");
                iconColor = ChipText;
                break;
            case "rechazada":
                EstadoKey = "rechazada";
                EstadoTexto = "Rechazada";
                TileBg = Color.FromArgb(oscuro ? "#93000A" : "#FFDAD6");
                ChipBg = TileBg;
                ChipText = Color.FromArgb(oscuro ? "#FFDAD6" : "#93000A");
                iconColor = ChipText;
                break;
            default:
                EstadoKey = "revision";
                EstadoTexto = "En revisión";
                TileBg = Color.FromArgb(oscuro ? "#282A2E" : "#EEEDF3");
                ChipBg = TileBg;
                ChipText = Color.FromArgb(oscuro ? "#C3C6D1" : "#434750");
                iconColor = Color.FromArgb(oscuro ? "#8D919B" : "#737781");
                break;
        }
        IconoFill = new SolidColorBrush(iconColor);

        string data = Iconos[ClaveIcono(Zona)];
        try { IconoGeometry = new PathGeometryConverter().ConvertFromInvariantString(data) as Geometry; }
        catch { IconoGeometry = null; }

        Meta = FechaRelativa(LeerFecha(dto));
        MostrarAcciones = UserSession.EsArquitecto;
    }

    // Elige el icono según el nombre de la zona
    private static string ClaveIcono(string zona)
    {
        string z = Normalizar(zona);
        if (z.Contains("cocina")) return "kitchen";
        if (z.Contains("terraza") || z.Contains("patio") || z.Contains("balcon") || z.Contains("jardin")) return "deck";
        if (z.Contains("recamara") || z.Contains("habitacion") || z.Contains("dormitorio") || z.Contains("cuarto")) return "bed";
        if (z.Contains("sala") || z.Contains("comedor")) return "weekend";
        if (z.Contains("bano")) return "bathtub";
        if (z.Contains("vestidor") || z.Contains("closet")) return "checkroom";
        if (z.Contains("puerta")) return "door_front";
        if (z.Contains("ventana")) return "window";
        return "location_on";
    }

    private static string Normalizar(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in (s ?? "").Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        }
        return sb.ToString().ToLowerInvariant();
    }

    // La app del cliente guarda "[Zona · x:.. m, y:.. m] texto" en la descripción
    // y "[Zona] resumen" en el título. Las ideas escritas a mano usan título + detalle.
    private static (string zona, string texto) Separar(string? titulo, string? descripcion)
    {
        string desc = (descripcion ?? "").Trim();
        string tit = (titulo ?? "").Trim();

        var md = Regex.Match(desc, @"^\[(?<z>[^\]]+)\]\s*(?<t>.*)$", RegexOptions.Singleline);
        if (md.Success)
        {
            string z = md.Groups["z"].Value;
            int idx = z.IndexOf(" · x:", StringComparison.Ordinal);
            if (idx >= 0) z = z[..idx];
            return (z.Trim(), md.Groups["t"].Value.Trim());
        }

        var mt = Regex.Match(tit, @"^\[(?<z>[^\]]+)\]");
        if (mt.Success) return (mt.Groups["z"].Value.Trim(), desc);

        return (tit.Length > 0 ? tit : "Sugerencia", desc);
    }

    // No dependo del nombre exacto de la propiedad de fecha del DTO
    private static DateTime? LeerFecha(SugerenciaDto dto)
    {
        try
        {
            var type = dto.GetType();
            foreach (var name in new[] { "Fechacreacion", "FechaCreacion", "Fecha" })
            {
                var prop = type.GetProperty(name);
                if (prop == null) continue;
                var val = prop.GetValue(dto);
                if (val is DateTime dt) return dt;
                if (val is string s && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var p)) return p;
            }
        }
        catch { }
        return null;
    }

    private static string FechaRelativa(DateTime? fecha)
    {
        if (fecha == null) return "";

        int dias = (DateTime.UtcNow.Date - fecha.Value.Date).Days;
        if (dias <= 0) return "Hoy";
        if (dias == 1) return "Ayer";
        if (dias < 7) return $"Hace {dias} días";

        string corto = fecha.Value.ToString("d MMM", new CultureInfo("es-CO")).Replace(".", "");
        return corto.ToLowerInvariant();
    }
}

public partial class SuggestionsPage : ContentPage
{
    private readonly List<SugerenciaItem> _todas = new();
    private readonly ObservableCollection<SugerenciaItem> _visibles = new();
    private readonly int _idProyecto;
    private int _tab = 0; // 0 = Todas, 1 = En revisión, 2 = Aprobadas
    private bool _cargando = false;

    public bool IsArquitecto => UserSession.EsArquitecto;

    public SuggestionsPage(int idProyecto)
    {
        BindingContext = this;
        InitializeComponent();
        _idProyecto = idProyecto;
        BindableLayout.SetItemsSource(ListHost, _visibles);

        ProjectChipLabel.Text = UserSession.ActiveProject != null && UserSession.ActiveProject.Idproyecto == idProyecto
            ? UserSession.ActiveProject.Nombre
            : "Proyecto";

        ActualizarTabs();
        AplicarFiltro();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        OnPropertyChanged(nameof(IsArquitecto));
        if (Application.Current != null) Application.Current.RequestedThemeChanged += OnThemeChanged;
        CargarSugerencias();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (Application.Current != null) Application.Current.RequestedThemeChanged -= OnThemeChanged;
    }

    private static bool EsOscuro()
    {
        var app = Application.Current;
        if (app == null) return false;
        var tema = app.UserAppTheme != AppTheme.Unspecified ? app.UserAppTheme : app.RequestedTheme;
        return tema == AppTheme.Dark;
    }

    // Los colores del estado se calculan por tema: si cambia, se reconstruyen las filas
    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var dtos = _todas.Select(x => x.Dto).ToList();
            _todas.Clear();
            foreach (var d in dtos) _todas.Add(new SugerenciaItem(d, EsOscuro()));
            AplicarFiltro();
        });
    }

    private async void CargarSugerencias()
    {
        _cargando = true;
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        _todas.Clear();
        AplicarFiltro();

        try
        {
            var items = await ApiService.GetSugerenciasByProyectoAsync(_idProyecto);
            if (items != null)
            {
                bool oscuro = EsOscuro();
                foreach (var item in items)
                {
                    _todas.Add(new SugerenciaItem(item, oscuro));
                }
            }
        }
        catch { }
        finally
        {
            _cargando = false;
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            AplicarFiltro();
        }
    }

    // ---- Pestañas ----

    private void OnTabTapped(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, TabTodas)) _tab = 0;
        else if (ReferenceEquals(sender, TabRevision)) _tab = 1;
        else if (ReferenceEquals(sender, TabAprobadas)) _tab = 2;
        else return;

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        ActualizarTabs();
        AplicarFiltro();
    }

    private void ActualizarTabs()
    {
        EstiloTab(TabTodas, LblTodas, _tab == 0);
        EstiloTab(TabRevision, LblRevision, _tab == 1);
        EstiloTab(TabAprobadas, LblAprobadas, _tab == 2);
    }

    private static void EstiloTab(Border tab, Label label, bool activa)
    {
        label.FontAttributes = FontAttributes.Bold;
        if (activa)
        {
            tab.SetAppThemeColor(VisualElement.BackgroundColorProperty, Color.FromArgb("#D2E0FE"), Color.FromArgb("#3C4961"));
            label.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#002753"), Color.FromArgb("#A9C7FF"));
        }
        else
        {
            tab.SetAppThemeColor(VisualElement.BackgroundColorProperty, Colors.Transparent, Colors.Transparent);
            label.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#434750"), Color.FromArgb("#C3C6D1"));
        }
    }

    private void AplicarFiltro()
    {
        var lista = _todas.Where(it =>
                _tab == 0
                || (_tab == 1 && it.EstadoKey == "revision")
                || (_tab == 2 && it.EstadoKey == "aprobada"))
            .ToList();

        for (int i = 0; i < lista.Count; i++) lista[i].MostrarDivisor = i < lista.Count - 1;

        _visibles.Clear();
        foreach (var it in lista) _visibles.Add(it);

        CountLabel.Text = $"{_todas.Count} en total";
        ListCard.IsVisible = lista.Count > 0;
        EmptyState.IsVisible = lista.Count == 0 && !_cargando;
    }

    // ---- Cerrar ----

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    // ---- Hoja "Enviar idea" ----

    private async void OnNewSuggestionClicked(object? sender, EventArgs e)
    {
        EntryTitulo.Text = "";
        EntryDesc.Text = "";

        SheetBackdrop.IsVisible = true;
        SheetBackdrop.InputTransparent = false;
        await SheetBackdrop.FadeToAsync(1, 200);
        NewSuggestionSheet.IsVisible = true;
        await NewSuggestionSheet.TranslateToAsync(0, 0, 250, Easing.SpringOut);
        await NewSuggestionSheet.FadeToAsync(1, 250);
    }

    private async void OnCloseSheetClicked(object? sender, EventArgs e)
    {
        await CloseSheetAsync();
    }

    private async Task CloseSheetAsync()
    {
        SheetBackdrop.InputTransparent = true;
        await Task.WhenAll(
            NewSuggestionSheet.TranslateToAsync(0, 500, 250, Easing.CubicIn),
            NewSuggestionSheet.FadeToAsync(0, 250),
            SheetBackdrop.FadeToAsync(0, 200)
        );
        NewSuggestionSheet.IsVisible = false;
        SheetBackdrop.IsVisible = false;
    }

    private async void OnSubmitSuggestionClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EntryTitulo.Text) || string.IsNullOrWhiteSpace(EntryDesc.Text))
        {
            await AlertService.ShowAlertAsync("Aviso", "Debes ingresar un título y descripción.", "OK");
            return;
        }

        var req = new CrearSugerenciaRequest
        {
            Idproyecto = _idProyecto,
            Idusuario = UserSession.Idusuario,
            Titulo = EntryTitulo.Text.Trim(),
            Descripcion = EntryDesc.Text.Trim()
        };

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        var (success, msg, data) = await ApiService.CrearSugerenciaAsync(req);
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        if (success && data != null)
        {
            _todas.Insert(0, new SugerenciaItem(data, EsOscuro()));
            AplicarFiltro();
            await CloseSheetAsync();
        }
        else
        {
            await AlertService.ShowAlertAsync("Error", msg, "OK");
        }
    }

    // ---- Aprobar / Rechazar (solo arquitecto) ----

    private async void OnApproveClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is SugerenciaItem item)
            await UpdateEstado(item, "Aprobada");
    }

    private async void OnRejectClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is SugerenciaItem item)
            await UpdateEstado(item, "Rechazada");
    }

    private async Task UpdateEstado(SugerenciaItem item, string nuevoEstado)
    {
        if (!UserSession.EsArquitecto) return;

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        var (success, msg) = await ApiService.ActualizarEstadoSugerenciaAsync(item.Dto.Idsugerencia, nuevoEstado);
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        if (success)
        {
            item.Dto.Estado = nuevoEstado;
            int idx = _todas.FindIndex(x => x.Dto.Idsugerencia == item.Dto.Idsugerencia);
            if (idx >= 0) _todas[idx] = new SugerenciaItem(item.Dto, EsOscuro());
            AplicarFiltro();
        }
        else
        {
            await AlertService.ShowAlertAsync("Error", msg, "OK");
        }
    }
}