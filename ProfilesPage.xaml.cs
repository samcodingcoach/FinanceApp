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

        // 4. Tampilkan Prompt
        string result = await DisplayPromptAsync(
            $"Ubah {fieldName}", 
            message, 
            accept: "SIMPAN", 
            cancel: "BATAL",
            placeholder: $"Ketik {fieldName} baru...");

        // Jika user membatalkan (null) atau mengosongkan, abaikan
        if (string.IsNullOrWhiteSpace(result))
            return;

        // 4. Update UI Label yang bersangkutan
        if (fieldName == "Nama Lengkap")
            L_NamaLengkap.Text = result;
        else if (fieldName == "Email")
            L_Email.Text = result;
        else if (fieldName == "Telepon Whatsapp")
            L_Telepon.Text = result;
        else if (fieldName == "Username")
            L_Username.Text = result;
        else if (fieldName == "Password")
            L_Password.Text = result;
        else if (fieldName == "Posisi")
            L_Posisi.Text = result;
            
        // TODO: Update ke API (Supabase) bisa ditambahkan di sini nantinya
    }
}