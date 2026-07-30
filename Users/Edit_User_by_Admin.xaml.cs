using System.Net.Http.Headers;
using System.Text;
using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;

namespace FinanceApp.Users;

public partial class Edit_User_by_Admin : ContentPage
{
    private UserModel _user;

    public Edit_User_by_Admin(UserModel user)
    {
        InitializeComponent();
        _user = user;
        
        // Populate UI
        e_username.Text = _user.username;
     
        c_isactive.IsToggled = _user.is_active;

       
        if (_user.HasPhoto)
        {
            ImgPhoto.ImageSource = ImageSource.FromUri(new Uri(_user.FullPhotoUrl));
        }
        
        // Disable fields that are not meant to be updated in this endpoint
      
    }

    private void E_username_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Event dummy, not used since it's readonly
    }

    private async void SelectPhoto_Tapped(object sender, TappedEventArgs e)
    {
        await Toast.Make("Foto tidak dapat diubah dari halaman ini").Show();
    }

    private async void BSimpan_Clicked(object sender, EventArgs e)
    {
        OverlayLoading.IsVisible = true;
        
        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                // PATCH: API_HOST + users?id_users=eq.X 
                string patchUrl = $"{App.API_HOST}/users?id_users=eq.{_user.id_users}";

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");

                // Sesuai agy.txt: body is_active
                var payload = new
                {
                    is_active = c_isactive.IsToggled
                };

                var jsonContent = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await client.PatchAsync(patchUrl, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    await Toast.Make("Berhasil update status user").Show();
                    
                    // Jeda 3 detik
                    await Task.Delay(3000);

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Navigation.PopAsync();
                    });
                }
                else
                {
                    string errDb = await response.Content.ReadAsStringAsync();
                    await Toast.Make($"Gagal update: {response.StatusCode}").Show();
                }
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Terjadi kesalahan: {ex.Message}").Show();
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OverlayLoading.IsVisible = false;
            });
        }
    }

    private bool _isDeleting = false;

    private async void BDelete_Clicked(object sender, EventArgs e)
    {
        _isDeleting = true;
        OverlayDelete.IsVisible = true;
        BCancelDelete.IsVisible = true;
        
        for (int i = 5; i > 0; i--)
        {
            if (!_isDeleting) return; // dibatalkan
            L_DeleteCountdown.Text = $"Menghapus dalam {i}...";
            await Task.Delay(1000);
        }

        if (!_isDeleting) return; // dibatalkan pada detik terakhir
        
        // Mulai proses delete
        L_DeleteCountdown.Text = "Menghapus...";
        BCancelDelete.IsVisible = false; // Tombol batal dihilangkan
        
        await PerformDeleteAsync();
    }

    private void BCancelDelete_Clicked(object sender, EventArgs e)
    {
        _isDeleting = false;
        OverlayDelete.IsVisible = false;
    }

    private async Task PerformDeleteAsync()
    {
        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                // DELETE: API_HOST + users?id_users=eq.X 
                string deleteUrl = $"{App.API_HOST}/users?id_users=eq.{_user.id_users}";

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                var response = await client.DeleteAsync(deleteUrl);

                if (response.IsSuccessStatusCode)
                {
                    await Toast.Make("Berhasil menghapus user").Show();
                    
                    // Jeda 3 detik
                    await Task.Delay(3000);

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Navigation.PopAsync();
                    });
                }
                else
                {
                    string errDb = await response.Content.ReadAsStringAsync();
                    await Toast.Make($"Gagal menghapus: {response.StatusCode}").Show();
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OverlayDelete.IsVisible = false;
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Terjadi kesalahan: {ex.Message}").Show();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OverlayDelete.IsVisible = false;
            });
        }
    }
}