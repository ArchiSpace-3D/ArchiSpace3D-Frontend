using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace MauiApp1;

public partial class LoginPage : ContentPage
{
    // Reutilizar HttpClient es una buena práctica
    private static readonly HttpClient client = new HttpClient();

    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string ip = IpEntry.Text?.Trim();
        string email = EmailEntry.Text?.Trim();
        string password = PasswordEntry.Text?.Trim();

        if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Error", "Por favor completa todos los campos.", "OK");
            return;
        }

        LoginButton.IsEnabled = false;
        LoginButton.Text = "Conectando...";

        try
        {
            string url = $"http://{ip}:5000/api/usuario/login";

            var loginData = new { Email = email, Password = password };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                string jsonResult = await response.Content.ReadAsStringAsync();
                await DisplayAlert("¡Conexión Exitosa!", $"Sesión iniciada correctamente.", "OK");
                
                // Navegar al Dashboard principal usando AppShell
                Application.Current.MainPage = new AppShell();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await DisplayAlert("Error", "Credenciales incorrectas.", "OK");
            }
            else
            {
                await DisplayAlert("Error de Backend", $"Código de error: {response.StatusCode}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fallo de Conexión", $"No se pudo contactar al backend en {ip}. ¿Está corriendo el backend? Detalle: {ex.Message}", "OK");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Text = "Iniciar Sesión";
        }
    }

    private void OnSkipClicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new AppShell();
    }
}
