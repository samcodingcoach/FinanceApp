using CommunityToolkit.Maui.Alerts;

namespace FinanceApp.Pengaturan;

public partial class SettingRefreshBeranda : ContentPage
{
    private int _currentMinutes = 30;

    public SettingRefreshBeranda()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _currentMinutes = Preferences.Get("refresh_interval_minutes", 30);
        if (_currentMinutes < 1) _currentMinutes = 1;
        if (_currentMinutes > 30) _currentMinutes = 30;

        SliderInterval.Value = _currentMinutes;
        UpdateDisplay(_currentMinutes);
    }

    private void SliderInterval_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        _currentMinutes = (int)Math.Round(e.NewValue);
        if (_currentMinutes < 1) _currentMinutes = 1;
        if (_currentMinutes > 30) _currentMinutes = 30;

        UpdateDisplay(_currentMinutes);
    }

    private async void Preset_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 50);
            await view.ScaleTo(1, 50);
        }

        if (e.Parameter is string valStr && int.TryParse(valStr, out int minutes))
        {
            _currentMinutes = minutes;
            SliderInterval.Value = minutes;
            UpdateDisplay(minutes);
        }
    }

    private void UpdateDisplay(int minutes)
    {
        L_SelectedMinutes.Text = $"{minutes} Menit";
    }

    private async void Back_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 50);
            await view.ScaleTo(1, 50);
        }
        await Navigation.PopAsync();
    }

    private async void BtnSimpan_Clicked(object sender, EventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.95, 50);
            await view.ScaleTo(1, 50);
        }

        Preferences.Set("refresh_interval_minutes", _currentMinutes);
        Beranda.ResetCache();

        bool logoutNow = await DisplayAlert("Pengaturan Disimpan", 
            $"Interval refresh berhasil disetel ke {_currentMinutes} menit.\n\nSilakan logout dan login ulang agar perubahan waktu aktif secara optimal di halaman Beranda.", 
            "Logout Sekarang", "Nanti Saja");

        if (logoutNow)
        {
            // Bersihkan sesi & logout bersih
            Preferences.Remove("user_data");
            Preferences.Remove("id_user");
            Beranda.ResetCache();

            MainThread.BeginInvokeOnMainThread(() => 
            {
                if (Application.Current != null)
                {
                    Application.Current.MainPage = new MainPage();
                }
            });
        }
        else
        {
            await Navigation.PopAsync();
        }
    }
}