namespace MauiApp1
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new NavigationPage(new LoginPage()));
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