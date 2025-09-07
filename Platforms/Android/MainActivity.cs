using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace AritmetrisMAUI
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public override bool DispatchKeyEvent(KeyEvent e)
        {
            // Redirigimos todas las teclas a la página de juego si está activa.
            // Si la página las consume, no seguimos con el pipeline normal.
            if (Aritmetris.Pages.GamePage.HandleAndroidKeyEventStatic?.Invoke(e) == true)
                return true;

            return base.DispatchKeyEvent(e);
        }
    }
}
