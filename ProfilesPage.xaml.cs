namespace FinanceApp;

public partial class ProfilesPage : ContentPage
{
	public ProfilesPage()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadUserData();
    }

    private void LoadUserData()
    {
        string jsonUser = Preferences.Get("user_data", string.Empty);
        if (!string.IsNullOrEmpty(jsonUser))
        {
            try 
            {
                var user = Newtonsoft.Json.JsonConvert.DeserializeObject<LoginResponse>(jsonUser);
                if (user != null)
                {
                    L_NamaLengkap.Text = string.IsNullOrEmpty(user.nama_lengkap) ? "-" : user.nama_lengkap;
                    L_Email.Text = string.IsNullOrEmpty(user.email) ? "-" : user.email;
                    L_Telepon.Text = string.IsNullOrEmpty(user.whatsapp) ? "-" : user.whatsapp;
                    L_Username.Text = string.IsNullOrEmpty(user.username) ? "-" : "@" + user.username;
                    L_Posisi.Text = string.IsNullOrEmpty(user.role) ? "-" : user.role.ToUpper();
                    L_Password.Text = "********"; // Disembunyikan

                    if (!string.IsNullOrEmpty(user.photo))
                    {
                        var app = Application.Current as App;
                        if (app != null && !user.photo.StartsWith("http"))
                        {
                            ImgPhoto.ImageSource = ImageSource.FromUri(new Uri(app.BUCKET_URL + "/photo_user/" + user.photo));
                        }
                        else 
                        {
                            ImgPhoto.ImageSource = ImageSource.FromUri(new Uri(user.photo));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Abaikan error parse jika format berubah
            }
        }
    }

    private async void Field_Tapped(object sender, TappedEventArgs e)
    {
        // 1. Animasi Tap
        if (sender is Grid grid)
        {
            await grid.FadeTo(0.5, 100);
            await grid.FadeTo(1, 100);
        }

        // 2. Ambil parameter (nama field) dari XAML
        string fieldName = e.Parameter?.ToString() ?? "Data";

        // 3. Tentukan pesan contoh (Example) sesuai konteks field
        string message = "Ubah data ini";
        if (fieldName == "Nama Lengkap") message = "Ex. Budi Santoso";
        else if (fieldName == "Email") message = "Ex. budi@email.com";
        else if (fieldName == "Telepon Whatsapp") message = "Ex. 081234567890";
        else if (fieldName == "Username") message = "Ex. budi_s";
        else if (fieldName == "Password") message = "Masukkan password baru yang aman";
        else if (fieldName == "Posisi") message = "Ex. AYAH, IBU, atau ANAK";

        // Dapatkan nilai lama untuk perbandingan
        string currentValue = "";
        if (fieldName == "Nama Lengkap") currentValue = L_NamaLengkap.Text;
        else if (fieldName == "Email") currentValue = L_Email.Text;
        else if (fieldName == "Telepon Whatsapp") currentValue = L_Telepon.Text;
        else if (fieldName == "Username") currentValue = L_Username.Text?.Replace("@", "");

        // 4. Tampilkan Prompt
        string result = await DisplayPromptAsync(
            $"Ubah {fieldName}", 
            message, 
            accept: "SIMPAN", 
            cancel: "BATAL",
            initialValue: currentValue,
            placeholder: $"Ketik {fieldName} baru...");

        // Jika user membatalkan (null) atau mengosongkan, abaikan
        if (string.IsNullOrWhiteSpace(result))
            return;
            
        // Jangan panggil endpoint jika isiannya sama
        if (result == currentValue)
            return;

        // Validasi Field yang diizinkan untuk API ini
        if (fieldName == "Posisi")
        {
            await DisplayAlert("Info", $"{fieldName} tidak dapat diubah dari sini.", "OK");
            return;
        }

        // Tampilkan Overlay Loading
        LoadingOverlay.IsVisible = true;

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;
            string jsonUser = Preferences.Get("user_data", string.Empty);
            var user = Newtonsoft.Json.JsonConvert.DeserializeObject<LoginResponse>(jsonUser);

            if (user != null)
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenKey);
                    client.DefaultRequestHeaders.Add("apikey", tokenKey);
                    
                    HttpResponseMessage response;
                    
                    if (fieldName == "Password")
                    {
                        // Endpoint khusus password (RPC POST)
                        string url = $"{App.API_HOST}/rpc/update_password";
                        var bodyObj = new
                        {
                            p_id_users = user.user_id.ToString(),
                            p_password_baru = result
                        };
                        
                        string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(bodyObj);
                        var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
                        
                        response = await client.PostAsync(url, content);
                    }
                    else
                    {
                        // Endpoint reguler untuk profil (PATCH)
                        client.DefaultRequestHeaders.Add("Prefer", "return=representation");
                        string url = $"{App.API_HOST}/users?id_users=eq.{user.user_id}";
                        
                        var bodyObj = new Dictionary<string, string>();
                        if (fieldName == "Nama Lengkap") bodyObj["nama_lengkap"] = result;
                        else if (fieldName == "Email") bodyObj["email"] = result;
                        else if (fieldName == "Telepon Whatsapp") bodyObj["whatsapp"] = result;
                        else if (fieldName == "Username") bodyObj["username"] = result;

                        string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(bodyObj);
                        var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

                        var request = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
                        response = await client.SendAsync(request);
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        // Update UI
                        if (fieldName == "Nama Lengkap") L_NamaLengkap.Text = result;
                        else if (fieldName == "Email") L_Email.Text = result;
                        else if (fieldName == "Telepon Whatsapp") L_Telepon.Text = result;
                        else if (fieldName == "Username") L_Username.Text = "@" + result;
                        else if (fieldName == "Password") L_Password.Text = "********";

                        // Update user_data di Preferences agar persisten
                        if (fieldName == "Nama Lengkap") user.nama_lengkap = result;
                        else if (fieldName == "Email") user.email = result;
                        else if (fieldName == "Telepon Whatsapp") user.whatsapp = result;
                        else if (fieldName == "Username") user.username = result;
                        // Password tidak perlu disimpan ke Preferences
                        
                        Preferences.Set("user_data", Newtonsoft.Json.JsonConvert.SerializeObject(user));
                        
                        await CommunityToolkit.Maui.Alerts.Toast.Make("Data berhasil diperbarui!").Show();

                        // Paksa logout jika yang diubah adalah Password
                        if (fieldName == "Password")
                        {
                            await Task.Delay(1000); // Beri waktu sejenak agar toast terlihat
                            Preferences.Remove("user_data");
                            MainThread.BeginInvokeOnMainThread(() => 
                            {
                                Application.Current.MainPage = new NavigationPage(new Login());
                            });
                        }
                    }
                    else
                    {
                        string errObj = await response.Content.ReadAsStringAsync();
                        await DisplayAlert("Gagal Update", $"Status: {response.StatusCode}\nError: {errObj}", "OK");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    [Obsolete]
    private async void AvatarTapped(object sender, TappedEventArgs e)
    {
        try
        {
            // Animasi Tap (efek membal/scale mengecil sejenak)
            if (sender is View avatarView)
            {
                await avatarView.ScaleTo(0.9, 100);
                await avatarView.ScaleTo(1.0, 100);
            }

            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Pilih Foto Profil"
            });

            if (result != null)
            {
                var fileInfo = new FileInfo(result.FullPath);
                if (fileInfo.Length > 500 * 1024)
                {
                    await DisplayAlert("Gagal", "Ukuran gambar maksimal 500KB", "OK");
                    return;
                }

                // Tampilkan loading overlay
                LoadingOverlay.IsVisible = true;

                var app = Application.Current as App;
                string tokenKey = app?.TOKEN_KEY ?? string.Empty;
                string jsonUser = Preferences.Get("user_data", string.Empty);
                var user = Newtonsoft.Json.JsonConvert.DeserializeObject<LoginResponse>(jsonUser);

                if (user != null && app != null)
                {
                    // Gunakan nama file yang sama dengan sebelumnya (jika ada) agar tertimpa,
                    // jika belum pernah punya foto, buat nama unik baru
                    string safeFileName = string.IsNullOrEmpty(user.photo) 
                        ? $"user_{user.user_id}_{DateTime.Now.Ticks}.png" 
                        : user.photo;

                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenKey);
                        client.DefaultRequestHeaders.Add("apikey", tokenKey);

                        // TAHAP 1: Upload ke Storage Bucket
                        string storageUrl = app.BUCKET_URL.Replace("/public", "") + "/photo_user/" + safeFileName;
                        
                        client.DefaultRequestHeaders.Add("x-upsert", "true");

                        var content = new ByteArrayContent(File.ReadAllBytes(result.FullPath));
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

                        var uploadResponse = await client.PostAsync(storageUrl, content);

                        if (!uploadResponse.IsSuccessStatusCode)
                        {
                            string err = await uploadResponse.Content.ReadAsStringAsync();
                            await DisplayAlert("Gagal Upload", $"Error: {err}", "OK");
                            LoadingOverlay.IsVisible = false;
                            return;
                        }

                        // TAHAP 2: Update kolom photo di database tabel users
                        client.DefaultRequestHeaders.Remove("x-upsert");
                        client.DefaultRequestHeaders.Add("Prefer", "return=representation");

                        string dbUrl = $"{App.API_HOST}/users?id_users=eq.{user.user_id}";
                        
                        var bodyObj = new Dictionary<string, string>();
                        bodyObj["photo"] = safeFileName;

                        string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(bodyObj);
                        var dbContent = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

                        var request = new HttpRequestMessage(new HttpMethod("PATCH"), dbUrl) { Content = dbContent };
                        var dbResponse = await client.SendAsync(request);

                        if (dbResponse.IsSuccessStatusCode)
                        {
                            // Perbarui state lokal dan UI
                            user.photo = safeFileName;
                            Preferences.Set("user_data", Newtonsoft.Json.JsonConvert.SerializeObject(user));
                            
                            var stream = await result.OpenReadAsync();
                            ImgPhoto.ImageSource = ImageSource.FromStream(() => stream);

                            await CommunityToolkit.Maui.Alerts.Toast.Make("Foto profil berhasil diperbarui!").Show();
                        }
                        else
                        {
                            string errDb = await dbResponse.Content.ReadAsStringAsync();
                            await DisplayAlert("Gagal Update Data", $"Status: {dbResponse.StatusCode}\nError: {errDb}", "OK");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private async void Logout_Tapped(object sender, TappedEventArgs e)
    {
        bool confirm = await DisplayAlert("Konfirmasi", "Apakah Anda yakin ingin keluar?", "Ya, Logout", "Batal");
        if (confirm)
        {
            // Hapus data sesi / login dari penyimpanan lokal
            Preferences.Remove("user_data");
            
            // Arahkan kembali ke halaman Login (menggantikan struktur Shell saat ini)
            MainThread.BeginInvokeOnMainThread(() => 
            {
                Application.Current.MainPage = new NavigationPage(new Login());
            });
        }
    }
}