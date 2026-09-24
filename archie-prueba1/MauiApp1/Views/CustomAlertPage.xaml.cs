using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MauiApp1.Views;

public partial class CustomAlertPage : ContentPage
{
    private TaskCompletionSource<bool> _tcs;

    public CustomAlertPage(string title, string message, string acceptText, string? cancelText, TaskCompletionSource<bool> tcs)
    {
        InitializeComponent();
        _tcs = tcs;

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        ActionButton.Text = acceptText;

        string t = title.ToLower();

        if (t.Contains("error") || t.Contains("fallo") || t.Contains("excep") || t.Contains("incorrect"))
        {
            IconBg.BackgroundColor = Color.FromArgb("#FEE2E2"); 
            IconLabel.Text = "✕";
            IconLabel.TextColor = Color.FromArgb("#DC2626"); 
            
            ActionButton.BackgroundColor = Color.FromArgb("#FEE2E2");
            ActionButton.TextColor = Color.FromArgb("#991B1B");
        }
        
        else if (t.Contains("aviso") || t.Contains("advertencia") || t.Contains("sesi") || t.Contains("seguro"))
        {
            IconBg.BackgroundColor = Color.FromArgb("#FEF3C7"); 
            IconLabel.Text = "!";
            IconLabel.TextColor = Color.FromArgb("#D97706"); 
            
            ActionButton.BackgroundColor = Color.FromArgb("#FEF3C7");
            ActionButton.TextColor = Color.FromArgb("#92400E");
        }
        
        else
        {
            IconBg.BackgroundColor = Color.FromArgb("#D1FAE5"); 
            IconLabel.Text = "✓";
            IconLabel.TextColor = Color.FromArgb("#059669"); 
            
            ActionButton.BackgroundColor = Color.FromArgb("#D1FAE5");
            ActionButton.TextColor = Color.FromArgb("#064E3B");
        }

        if (!string.IsNullOrEmpty(cancelText))
        {
            CancelButton.Text = cancelText;
            CancelButton.IsVisible = true;
            Grid.SetColumnSpan(ActionButton, 1);
            Grid.SetColumn(ActionButton, 1);
        }
    }

    private async void OnDismissClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync(false);
        _tcs.TrySetResult(true);
    }
    
    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync(false);
        _tcs.TrySetResult(false);
    }

    protected override bool OnBackButtonPressed()
    {
        Navigation.PopModalAsync(false);
        _tcs.TrySetResult(false);
        return true;
    }
}

