using Microsoft.Maui.Controls;
using System;
using MauiApp1.Services;
using MauiApp1.Models;
using System.Threading.Tasks;

namespace MauiApp1
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private void OnToggleRegPasswordVisibility(object? sender, EventArgs e)
        {
            RegPasswordEntry.IsPassword = !RegPasswordEntry.IsPassword;
            RegPasswordToggleIcon.Source = RegPasswordEntry.IsPassword ? "ic_eye_off.svg" : "ic_eye_on.svg";
        }

        private async void OnSubmitRegisterClicked(object? sender, EventArgs e)
        {
            // Lógica pendiente de mover (Actualmente funciona en el bottom sheet de LoginPage)
            await this.DisplayAlertAsync("Aviso", "El registro ha sido migrado al panel principal por ahora.", "OK");
            await Navigation.PopModalAsync();
        }
    }
}
