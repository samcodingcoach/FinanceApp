using Android.App;
using Android.Content.PM;
using Android.OS;

namespace FinanceApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // WORKAROUND FIX: Mencegah TabBar MAUI 8+ tenggelam di bawah 3 tombol navigasi (Edge-to-Edge bug)
            // Dengan mengeset flag ini ke true, Android akan menjamin aplikasi digambar murni di atas Navigation Bar
            AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(Window, true);
        }
    }
}
