using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using MauiApp1.Views;
using Microsoft.Maui.ApplicationModel;

namespace MauiApp1.Services;

public static class AlertService
{
    public static Task<bool> ShowAlertAsync(string title, string message, string acceptText = "OK", string cancelText = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        MainThread.BeginInvokeOnMainThread(async () => {
            var alertPage = new CustomAlertPage(title, message, acceptText, cancelText, tcs);
            var current = Application.Current?.Windows[0]?.Page;
            
            if (current != null)
            {
                await current.Navigation.PushModalAsync(alertPage, false);
            }
            else
            {
                tcs.TrySetResult(false);
            }
        });
        
        return tcs.Task;
    }
}

