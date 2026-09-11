using MauiApp1.Services;

namespace MauiApp1
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            bool isDark = Preferences.Get("dark_mode", false);
            UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new NavigationPage(new LoginPage()));
            
            Task.Run(async () =>
            {
                bool hasSession = await UserSession.LoadSessionAsync();
                if (hasSession)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        window.Page = new AppShell();
                    });
                }
            });

            return window;
        }

        public static void SetRootPage(Page page)
        {
            if (Current?.Windows.Count > 0)
            {
                Current.Windows[0].Page = page;
            }
        }
    }
}
