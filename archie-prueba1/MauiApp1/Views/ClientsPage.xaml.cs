using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace MauiApp1.Views;

public partial class ClientsPage : ContentPage
{
    private readonly ObservableCollection<UsuarioDto> _clientesFiltrados = new();
    private List<UsuarioDto> _todos = new();

    private UsuarioDto? _clienteEditando;
    private FileResult? _avatarClienteSeleccionado;

    public ClientsPage()
    {
        InitializeComponent();
        ClientesCollectionView.ItemsSource = _clientesFiltrados;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        bool esArquitecto = UserSession.Rol == "Arquitecto";
        LblHeaderTitle.Text = esArquitecto ? "Clientes" : "Mi Arquitecto";
        LblSeccionLista.Text = esArquitecto ? "TODOS" : "ARQUITECTOS";
        LblEmptyText.Text = esArquitecto
            ? "No hay clientes vinculados"
            : "No hay arquitectos vinculados";

        await CargarPersonasAsync();
    }

    private async Task CargarPersonasAsync()
    {
        try
        {
            bool esArquitecto = UserSession.Rol == "Arquitecto";

            List<ProyectoDto> proyectos = esArquitecto
                ? await ApiService.GetProyectosByArquitectoAsync(UserSession.Idusuario)
                : await ApiService.GetProyectosByClienteAsync(UserSession.Idusuario);

            HashSet<int> idsVinculados = new();
            foreach (var p in proyectos)
            {
                if (esArquitecto)
                {
                    if (p.Idcliente.HasValue && p.Idcliente.Value > 0)
                        idsVinculados.Add(p.Idcliente.Value);
                }
                else
                {
                    if (p.Idarquitecto.HasValue && p.Idarquitecto.Value > 0)
                        idsVinculados.Add(p.Idarquitecto.Value);
                }
            }

            if (idsVinculados.Count == 0)
            {
                _todos = new List<UsuarioDto>();
                ActualizarLista();
                ActualizarActivos();
                BtnEditarHeader.IsVisible = false;
                return;
            }

            var usuarios = await ApiService.GetUsuariosAsync();
            var rolFiltro = esArquitecto ? "Cliente" : "Arquitecto";

            _todos = usuarios
                .Where(u => u.Rol == rolFiltro && idsVinculados.Contains(u.Idusuario))
                .OrderBy(u => u.Nombre)
                .ToList();

            BtnEditarHeader.IsVisible = esArquitecto && _todos.Count > 0;

            ActualizarActivos();
            ActualizarLista();
        }
        catch (Exception ex)
        {
            await AlertService.ShowAlertAsync("Error", $"No se pudieron cargar: {ex.Message}", "OK");
        }
    }

    private void ActualizarLista()
    {
        _clientesFiltrados.Clear();
        foreach (var c in _todos)
            _clientesFiltrados.Add(c);
    }

    private void ActualizarActivos()
    {
        ActivosStack.Children.Clear();
        var activos = _todos.Where(c => c.Activo).ToList();

        if (activos.Count == 0)
        {
            ActivosContainer.IsVisible = false;
            return;
        }

        ActivosContainer.IsVisible = true;

        foreach (var persona in activos)
            ActivosStack.Children.Add(CreateActivoCircle(persona));
    }

    private View CreateActivoCircle(UsuarioDto persona)
    {
        var border = new Border
        {
            WidthRequest = 70,
            HeightRequest = 70,
            BackgroundColor = Color.FromArgb("#E2E8F0"),
            StrokeThickness = 2,
            Stroke = Color.FromArgb("#34C759"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 35 }
        };

        var grid = new Grid();
        grid.Children.Add(new Label
        {
            Text = persona.PrimeraLetra,
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0F172A"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        });

        if (!string.IsNullOrWhiteSpace(persona.Avatarurl))
        {
            grid.Children.Add(new Image
            {
                Source = persona.Avatarurl,
                Aspect = Aspect.AspectFill,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            });
        }

        border.Content = grid;
        border.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => OnClienteTapped(persona))
        });

        var label = new Label
        {
            Text = persona.Nombre,
            FontSize = 11,
            TextColor = Color.FromArgb("#64748B"),
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
            WidthRequest = 70
        };

        var stack = new VerticalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Center, WidthRequest = 70 };
        stack.Children.Add(border);
        stack.Children.Add(label);
        return stack;
    }

    private void OnBusquedaTextChanged(object? sender, TextChangedEventArgs e)
    {
        var filtro = e.NewTextValue?.Trim().ToLowerInvariant() ?? "";
        _clientesFiltrados.Clear();

        if (string.IsNullOrEmpty(filtro))
        {
            foreach (var c in _todos) _clientesFiltrados.Add(c);
        }
        else
        {
            foreach (var c in _todos.Where(c =>
                c.Nombre.ToLowerInvariant().Contains(filtro) ||
                c.Apellido.ToLowerInvariant().Contains(filtro) ||
                (c.Email ?? "").ToLowerInvariant().Contains(filtro)))
            {
                _clientesFiltrados.Add(c);
            }
        }
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//DashboardPage");
    }

    private void OnClienteTapped(UsuarioDto persona)
    {
        if (UserSession.Rol == "Arquitecto")
            _ = AbrirModalEditarCliente(persona);
        else
            _ = AbrirModalVerArquitecto(persona);
    }

    private async void OnClienteTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not UsuarioDto persona) return;

        if (UserSession.Rol == "Arquitecto")
            await AbrirModalEditarCliente(persona);
        else
            await AbrirModalVerArquitecto(persona);
    }

    private void OnToggleEditModeClicked(object? sender, TappedEventArgs e)
    {
        _ = AlertService.ShowAlertAsync("Modo edición",
            "Toca cualquier cliente de la lista para editarlo.", "OK");
    }

    // ====================== MODAL EDITAR ======================
    private async Task AbrirModalEditarCliente(UsuarioDto cliente)
    {
        _clienteEditando = cliente;
        _avatarClienteSeleccionado = null;

        EntryEditNombre.Text = cliente.Nombre;
        EntryEditApellido.Text = cliente.Apellido;
        EntryEditEmail.Text = cliente.Email;
        EntryEditTelefono.Text = cliente.Telefono ?? "";
        EntryEditDireccion.Text = cliente.Direccion ?? "";
        EntryEditNumeroDocumento.Text = cliente.Numerodocumento ?? "";
        SwitchActivo.IsToggled = cliente.Activo;

        if (!string.IsNullOrWhiteSpace(cliente.Tipodocumento) &&
            PickerEditTipoDocumento.Items.Contains(cliente.Tipodocumento))
            PickerEditTipoDocumento.SelectedItem = cliente.Tipodocumento;
        else
            PickerEditTipoDocumento.SelectedItem = null;

        if (!string.IsNullOrWhiteSpace(cliente.Avatarurl))
        {
            ImgAvatarEdit.Source = cliente.Avatarurl;
            ImgAvatarEdit.IsVisible = true;
            LblAvatarInicialEdit.IsVisible = false;
        }
        else
        {
            ImgAvatarEdit.Source = null;
            ImgAvatarEdit.IsVisible = false;
            LblAvatarInicialEdit.IsVisible = true;
            LblAvatarInicialEdit.Text = cliente.PrimeraLetra;
        }

        EditClienteBackdrop.IsVisible = true;
        await EditClienteBackdrop.FadeToAsync(1, 200);
        EditClienteSheetModal.IsVisible = true;
        await Task.WhenAll(
            EditClienteSheetModal.FadeToAsync(1, 250),
            EditClienteSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut)
        );
    }

    private async void OnCloseEditClienteTapped(object? sender, TappedEventArgs e)
    {
        await CloseEditCliente();
    }

    private async Task CloseEditCliente()
    {
        await Task.WhenAll(
            EditClienteSheetModal.FadeToAsync(0, 200),
            EditClienteSheetModal.TranslateToAsync(0, 600, 250, Easing.CubicIn)
        );
        EditClienteSheetModal.IsVisible = false;
        await EditClienteBackdrop.FadeToAsync(0, 200);
        EditClienteBackdrop.IsVisible = false;
        _avatarClienteSeleccionado = null;
    }

    private async void OnSeleccionarAvatarClienteClicked(object? sender, TappedEventArgs e)
    {
        try
        {
            var resultado = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona una foto",
                FileTypes = FilePickerFileType.Images
            });

            if (resultado == null) return;

            _avatarClienteSeleccionado = resultado;

            var stream = await resultado.OpenReadAsync();
            ImgAvatarEdit.Source = ImageSource.FromStream(() => stream);
            ImgAvatarEdit.IsVisible = true;
            LblAvatarInicialEdit.IsVisible = false;
        }
        catch (Exception ex)
        {
            await AlertService.ShowAlertAsync("Error", ex.Message, "OK");
        }
    }

    private async void OnGuardarClienteTapped(object? sender, TappedEventArgs e)
    {
        await GuardarClienteAsync();
    }

    private async void OnGuardarClienteTapped(object? sender, EventArgs e)
    {
        await GuardarClienteAsync();
    }

    private async Task GuardarClienteAsync()
    {
        if (_clienteEditando == null) return;

        var req = new ActualizarUsuarioRequest
        {
            Idusuario = _clienteEditando.Idusuario,
            Nombre = EntryEditNombre.Text ?? _clienteEditando.Nombre,
            Apellido = EntryEditApellido.Text ?? _clienteEditando.Apellido,
            Email = EntryEditEmail.Text ?? _clienteEditando.Email,
            Telefono = EntryEditTelefono.Text,
            Direccion = EntryEditDireccion.Text,
            Tipodocumento = PickerEditTipoDocumento.SelectedItem?.ToString(),
            Numerodocumento = EntryEditNumeroDocumento.Text,
            Rol = _clienteEditando.Rol,
            Avatarurl = _clienteEditando.Avatarurl
        };

        var res = await ApiService.ActualizarUsuarioAsync(_clienteEditando.Idusuario, req);
        if (!res.Success)
        {
            await AlertService.ShowAlertAsync("Error", res.Message, "OK");
            return;
        }

        if (_avatarClienteSeleccionado != null)
        {
            var (okImg, msgImg) = await ApiService.SubirAvatarUsuarioAsync(_clienteEditando.Idusuario, _avatarClienteSeleccionado);
            if (!okImg)
            {
                await AlertService.ShowAlertAsync("Aviso", $"Datos guardados, pero la foto falló: {msgImg}", "OK");
            }
        }

        await CloseEditCliente();
        await CargarPersonasAsync();
    }

    // ====================== MODAL VER ARQUITECTO ======================
    private async Task AbrirModalVerArquitecto(UsuarioDto arquitecto)
    {
        LblVerArquiTitulo.Text = arquitecto.NombreCompleto;
        LblVerArquiNombre.Text = arquitecto.NombreCompleto;
        LblVerArquiTelefono.Text = string.IsNullOrWhiteSpace(arquitecto.Telefono) ? "--" : arquitecto.Telefono;
        LblVerArquiDireccion.Text = string.IsNullOrWhiteSpace(arquitecto.Direccion) ? "--" : arquitecto.Direccion;

        if (!string.IsNullOrWhiteSpace(arquitecto.Avatarurl))
        {
            ImgVerArqui.Source = arquitecto.Avatarurl;
            ImgVerArqui.IsVisible = true;
            LblVerArquiInicial.IsVisible = false;
        }
        else
        {
            ImgVerArqui.Source = null;
            ImgVerArqui.IsVisible = false;
            LblVerArquiInicial.IsVisible = true;
            LblVerArquiInicial.Text = arquitecto.PrimeraLetra;
        }

        ViewArquiBackdrop.IsVisible = true;
        await ViewArquiBackdrop.FadeToAsync(1, 200);
        ViewArquiSheetModal.IsVisible = true;
        await Task.WhenAll(
            ViewArquiSheetModal.FadeToAsync(1, 250),
            ViewArquiSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut)
        );
    }

    private async void OnCloseViewArquiTapped(object? sender, TappedEventArgs e)
    {
        await CloseViewArqui();
    }

    private async void OnCloseViewArquiClicked(object? sender, EventArgs e)
    {
        await CloseViewArqui();
    }

    private async Task CloseViewArqui()
    {
        await Task.WhenAll(
            ViewArquiSheetModal.FadeToAsync(0, 200),
            ViewArquiSheetModal.TranslateToAsync(0, 500, 250, Easing.CubicIn)
        );
        ViewArquiSheetModal.IsVisible = false;
        await ViewArquiBackdrop.FadeToAsync(0, 200);
        ViewArquiBackdrop.IsVisible = false;
    }
}