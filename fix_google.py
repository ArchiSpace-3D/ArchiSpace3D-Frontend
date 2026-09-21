import re

with open('archie-prueba1/MauiApp1/LoginPage.xaml.cs', 'r', encoding='utf-8') as f:
    content = f.read()

pattern = r'private async void OnGoogleLoginClicked\(object\? sender, EventArgs e\).*?}'
replacement = '''private async void OnGoogleLoginClicked(object? sender, EventArgs e)
    {
        await GoogleLoginButton.ScaleToAsync(0.95, 70);
        await GoogleLoginButton.ScaleToAsync(1.0, 70);

        try
        {
            LoadingOverlay.IsVisible = true;
            if (SignalRService.HubConnection != null) await SignalRService.DisconnectAsync();

            var authState = await SupabaseService.Client.Auth.SignIn(
                Supabase.Gotrue.Constants.Provider.Google,
                new Supabase.Gotrue.SignInOptions
                {
                    RedirectTo = "com.archispace.archie://login-callback"
                });

            var result = await Microsoft.Maui.Authentication.WebAuthenticator.Default.AuthenticateAsync(
                new Microsoft.Maui.Authentication.WebAuthenticatorOptions
                {
                    Url = new System.Uri(authState.Uri.ToString()),
                    CallbackUrl = new System.Uri("com.archispace.archie://login-callback")
                });

            if (result.Properties.TryGetValue("access_token", out var accessToken))
            {
                var (success, message, response) = await ApiService.GoogleLoginAsync(accessToken);
                LoadingOverlay.IsVisible = false;

                if (success && response != null)
                {
                    Microsoft.Maui.Controls.Application.Current!.Windows[0].Page = new AppShell();
                }
                else
                {
                    await ShowToastAsync(message);
                }
            }
        }
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            LoadingOverlay.IsVisible = false;
        }
        catch (System.Exception ex)
        {
            LoadingOverlay.IsVisible = false;
            await ShowToastAsync("Error Google Auth: " + ex.Message);
        }
    }'''

content = re.sub(pattern, replacement, content, flags=re.DOTALL)

with open('archie-prueba1/MauiApp1/LoginPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(content)
