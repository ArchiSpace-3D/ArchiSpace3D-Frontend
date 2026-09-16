using Supabase;

namespace MauiApp1.Services
{
    public static class SupabaseService
    {
        private static Client? _client;

        private const string SupabaseUrl = "https://ejxfilcbchzhmbblrvve.supabase.co";
        private const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVqeGZpbGNiY2h6aG1iYmxydnZlIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODgxNzI4NDEsImV4cCI6MjEwMzc0ODg0MX0.ctiqxCLnVNe8SIG9WRbUM7aZtGNJp7qtSKRVydAuZVM"; // <-- reemplaza

        public static Client Client
        {
            get
            {
                if (_client is null)
                    throw new InvalidOperationException("SupabaseService no ha sido inicializado.");
                return _client;
            }
        }

        public static async Task InitializeAsync()
        {
            if (_client != null) return;

            var options = new SupabaseOptions { AutoConnectRealtime = false };
            _client = new Client(SupabaseUrl, SupabaseAnonKey, options);
            await _client.InitializeAsync();
        }

    }
}