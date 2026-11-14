using System.Globalization;

namespace MauiApp1.Services
{
    public static class UiHelper
    {
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public static async Task OpenSheetAsync(BoxView backdrop, VisualElement sheet)
        {
            backdrop.IsVisible = true;
            sheet.IsVisible = true;

            await Task.WhenAll(
                backdrop.FadeToAsync(1, 200, Easing.CubicOut),
                sheet.FadeToAsync(1, 250, Easing.CubicOut),
                sheet.TranslateToAsync(0, 0, 280, Easing.CubicOut)
            );
        }

        public static async Task CloseSheetAsync(BoxView backdrop, VisualElement sheet, double translateY = 500)
        {
            await Task.WhenAll(
                backdrop.FadeToAsync(0, 180, Easing.CubicIn),
                sheet.FadeToAsync(0, 200, Easing.CubicIn),
                sheet.TranslateToAsync(0, translateY, 240, Easing.CubicIn)
            );

            backdrop.IsVisible = false;
            sheet.IsVisible = false;
        }

        public static async Task ShowToastAsync(VisualElement toast, Label label, string message, int durationMs = 2500)
        {
            label.Text = message;
            toast.Opacity = 0;
            toast.IsVisible = true;

            await toast.FadeToAsync(1, 220, Easing.CubicOut);
            await Task.Delay(durationMs);
            await toast.FadeToAsync(0, 250, Easing.CubicIn);
            toast.IsVisible = false;
        }

        public static async Task<FileResult?> PickValidatedImageAsync(string pickerTitle)
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = pickerTitle,
                FileTypes = FilePickerFileType.Images
            });

            if (result == null) return null;

            string ext = Path.GetExtension(result.FileName);
            if (!AllowedImageExtensions.Contains(ext))
            {
                await AlertService.ShowAlertAsync("Formato no válido", "Solo se permiten imágenes JPG, PNG o WEBP.", "OK");
                return null;
            }

            return result;
        }

        public static decimal ParseDecimal(string? raw, decimal fallback = 0m)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            var clean = raw.Replace("$", "").Trim().Replace(',', '.');
            if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                return val;
            if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.CurrentCulture, out val))
                return val;
            return fallback;
        }
    }
}
