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
        if (fieldName == "Password" || fieldName == "Posisi")
        {
            await DisplayAlert("Info", $"{fieldName} memiliki endpoint/alur yang berbeda.", "OK");
            return; // Skip API call karena beda endpoint
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
                    client.DefaultRequestHeaders.Add("Prefer", "return=representation");

                    string url = $"{App.API_HOST}/users?id_users=eq.{user.user_id}";
                    
                    // Bangun JSON dinamis (hanya field yang diubah)
                    var bodyObj = new Dictionary<string, string>();
                    if (fieldName == "Nama Lengkap") bodyObj["nama_lengkap"] = result;
                    else if (fieldName == "Email") bodyObj["email"] = result;
                    else if (fieldName == "Telepon Whatsapp") bodyObj["whatsapp"] = result;
                    else if (fieldName == "Username") bodyObj["username"] = result;

                    string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(bodyObj);
                    var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        // Update UI
                        if (fieldName == "Nama Lengkap") L_NamaLengkap.Text = result;
                        else if (fieldName == "Email") L_Email.Text = result;
                        else if (fieldName == "Telepon Whatsapp") L_Telepon.Text = result;
                        else if (fieldName == "Username") L_Username.Text = "@" + result;

                        // Update user_data di Preferences agar persisten
                        if (fieldName == "Nama Lengkap") user.nama_lengkap = result;
                        else if (fieldName == "Email") user.email = result;
                        else if (fieldName == "Telepon Whatsapp") user.whatsapp = result;
                        else if (fieldName == "Username") user.username = result;
                        
                        Preferences.Set("user_data", Newtonsoft.Json.JsonConvert.SerializeObject(user));
                        
                        await CommunityToolkit.Maui.Alerts.Toast.Make("Data berhasil diperbarui!").Show();
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

    private async void Logout_Tapped(object sender, TappedEventArgs e)
    {
        bool confirm = await DisplayAlert("Konfirmasi", "Apakah Anda yakin ingin keluar?", "Ya, Logout", "Batal");
        if (confirm)
        {
            // Hapus data sesi / login dari penyimpanan lokal
            Preferences.Remove("user_data");
            
            // Arahkan kembali ke halaman Login (menggantikan struktur Shell saat ini)
            Application.Current.MainPage = new NavigationPage(new Login());
        }
    }
}