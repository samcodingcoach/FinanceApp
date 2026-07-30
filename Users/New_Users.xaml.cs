using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;

namespace FinanceApp.Users;

public partial class New_Users : ContentPage
{
    private string? _selectedImagePath;

    public New_Users()
    {
        InitializeComponent();
    }

    private void E_username_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.NewTextValue)) return;
        
        // Mencegah simbol dan spasi sesuai petunjuk (hanya alfanumerik)
        string clean = Regex.Replace(e.NewTextValue, @"[^a-zA-Z0-9]", "");
        if (e.NewTextValue != clean)
        {
            e_username.Text = clean;
        }
    }

    private async void SelectPhoto_Tapped(object sender, TappedEventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Pilih Foto User"
            });

            if (result != null)
            {
                var fileInfo = new FileInfo(result.FullPath);
                if (fileInfo.Length > 500 * 1024)
                {
                    await Toast.Make("Ukuran gambar maksimal 500KB").Show();
                    return;
                }

                _selectedImagePath = result.FullPath;
                var stream = await result.OpenReadAsync();
                ImgPhoto.ImageSource = ImageSource.FromStream(() => stream);
            }
        }
        catch (Exception ex)
        {
            await Toast.Make("Pemilihan gambar dibatalkan.").Show();
        }
    }

    private async void BSimpan_Clicked(object sender, EventArgs e)
    {
        string? username = e_username.Text?.Trim();
        string? namaLengkap = e_nama_lengkap.Text?.Trim();
        string? email = e_email.Text?.Trim();
        string? whatsapp = e_whatsapp.Text?.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(namaLengkap) || string.IsNullOrEmpty(email))
        {
            await Toast.Make("Semua form wajib diisi").Show();
            return;
        }

        if (!email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
        {
            await Toast.Make("Email wajib menggunakan @gmail.com").Show();
            return;
        }

        if (string.IsNullOrEmpty(_selectedImagePath))
        {
            await Toast.Make("Silakan pilih foto profil terlebih dahulu").Show();
            return;
        }

        string originalFileName = Path.GetFileName(_selectedImagePath);
        string safeFileName = originalFileName.Replace(" ", "");

        OverlayLoading.IsVisible = true;

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                // TAHAP 1: Upload Gambar ke photo_user/
                string uploadUrl = $"https://oiotjlunbaohzypbslla.supabase.co/storage/v1/object/photo_user/{safeFileName}";
                
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);
                client.DefaultRequestHeaders.Add("x-upsert", "true");

                var content = new ByteArrayContent(File.ReadAllBytes(_selectedImagePath));
                content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

                var uploadResponse = await client.PostAsync(uploadUrl, content);

                if (!uploadResponse.IsSuccessStatusCode)
                {
                    string err = await uploadResponse.Content.ReadAsStringAsync();
                    await Toast.Make($"Gagal upload foto: {uploadResponse.StatusCode}").Show();
                    OverlayLoading.IsVisible = false;
                    return;
                }

                // TAHAP 2: Simpan Data ke Table via RPC
                client.DefaultRequestHeaders.Remove("x-upsert");
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");

                string dbUrl = App.API_HOST_USER; 
                var payload = new
                {
                    p_username = username,
                    p_password = "123456", // default sesuai contoh
                    p_email = email,
                    p_is_active = c_isactive.IsToggled,
                    p_role = "Lainnya", // default role
                    p_nama_lengkap = namaLengkap,
                    p_photo = safeFileName,
                    p_whatsapp = whatsapp ?? ""
                };

                var jsonContent = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
                var dbResponse = await client.PostAsync(dbUrl, jsonContent);

                if (dbResponse.IsSuccessStatusCode || dbResponse.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    await Toast.Make("Berhasil menyimpan user").Show();
                    
                    // Jeda 3 detik sesuai instruksi untuk propagasi data/storage
                    await Task.Delay(3000);

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Navigation.PopAsync();
                    });
                }
                else
                {
                    string errDb = await dbResponse.Content.ReadAsStringAsync();
                    await Toast.Make($"Gagal simpan user: {dbResponse.StatusCode}").Show();
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
}