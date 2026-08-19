using CommunityToolkit.Maui.Alerts;

namespace FinanceApp.Pengaturan;

public partial class SettingFinger : ContentPage
{
    private bool _initialState;

    public SettingFinger()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _initialState = Preferences.Get("use_biometric", false);
        BiometricSwitch.IsToggled = _initialState;
        
        PasswordContainer.IsVisible = false;
        BtnSimpan.IsVisible = false;
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

    private void BiometricSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        if (e.Value != _initialState)
        {
            PasswordContainer.IsVisible = true;
            BtnSimpan.IsVisible = true;
        }
        else
        {
            PasswordContainer.IsVisible = false;
            BtnSimpan.IsVisible = false;
        }
    }

    private async void BtnSimpan_Clicked(object sender, EventArgs e)
    {
        string password = E_Password.Text ?? "";
        if (string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Peringatan", "Harap masukkan password untuk memverifikasi perubahan.", "OK");
            return;
        }

        Preferences.Set("use_biometric", BiometricSwitch.IsToggled);
        _initialState = BiometricSwitch.IsToggled;
        
        E_Password.Text = string.Empty;
        PasswordContainer.IsVisible = false;
        BtnSimpan.IsVisible = false;

        await DisplayAlert("Berhasil", "Pengaturan login biometrik berhasil disimpan.", "OK");
    }
}