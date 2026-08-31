using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Plugin.Maui.Biometric;

namespace FinanceApp;

public partial class Login : ContentPage
{
    private int _failedAttempts = 0;

    public Login()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text;
        string password = PasswordEntry.Text;

        if (string.IsNullOrEmpty(username))
        {
            await Toast.Make("Username tidak boleh kosong", ToastDuration.Short).Show();
            UsernameEntry.Focus();
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            await Toast.Make("Password tidak boleh kosong", ToastDuration.Short).Show();
            PasswordEntry.Focus();
            return;
        }

        OverlayLoading.IsVisible = true;
        OverlayPercentage.Text = "Cek Otentikasi... 0%";
        
        var delayTask = AnimateOverlayPercentage();

        try
        {
            var app = (App)Application.Current;
            string apiKey = app.TOKEN_KEY;
            string apiUrl = App.API_HOST + "rpc/login_user";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("apikey", apiKey);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var requestData = new
                {
                    p_username = username,
                    p_password = password
                };

                string jsonPayload = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                await delayTask; // Pastikan minimal 3 detik (animasi selesai)

                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine("================ [LOGIN DEBUG] ================");
                    System.Diagnostics.Debug.WriteLine($"[LOGIN RAW RESPONSE]: {responseContent}");

                    var resultList = JsonConvert.DeserializeObject<List<LoginResponse>>(responseContent);
                    if (resultList != null && resultList.Count > 0)
                    {
                        var result = resultList[0];
                        if (result.success)
                        {
                            int extractedId = result.user_id > 0 ? result.user_id : result.id_users;
                            if (extractedId <= 0)
                            {
                                try
                                {
                                    var jArr = Newtonsoft.Json.Linq.JArray.Parse(responseContent);
                                    if (jArr.Count > 0)
                                    {
                                        var jObj = jArr[0];
                                        extractedId = (int?)jObj["id_users"] ?? (int?)jObj["user_id"] ?? (int?)jObj["id_user"] ?? (int?)jObj["id"] ?? 0;
                                    }
                                }
                                catch { }
                            }

                            if (extractedId > 0)
                            {
                                result.user_id = extractedId;
                                result.id_users = extractedId;
                            }

                            System.Diagnostics.Debug.WriteLine($"[LOGIN SUCCESS] ID USERS KELUAR: {extractedId}");
                            System.Diagnostics.Debug.WriteLine($"[LOGIN SUCCESS] USERNAME: {result.username}, NAMA: {result.nama_lengkap}, ROLE: {result.role}");
                            System.Diagnostics.Debug.WriteLine("===============================================");

                            _failedAttempts = 0;
                            Preferences.Set("user_data", JsonConvert.SerializeObject(result));
                            Preferences.Set("id_user", extractedId);
                            
                            // Save credentials for Biometric fast-login
                            await SecureStorage.Default.SetAsync("last_username", username);
                            await SecureStorage.Default.SetAsync("last_password", password);

                            // Reset cache Beranda agar memuat data baru secara otomatis
                            Beranda.ResetCache();

                            MainThread.BeginInvokeOnMainThread(async () => 
                            {
                                if (Navigation.ModalStack.Count > 0)
                                {
                                    await Navigation.PopModalAsync();
                                }
                                else if (Application.Current != null)
                                {
                                    Application.Current.MainPage = new MainPage();
                                }
                            });
                        }
                        else
                        {
                            await HandleFailedAttempt(result.message);
                        }
                    }
                    else
                    {
                        await HandleFailedAttempt("Format response tidak valid");
                    }
                }
                else
                {
                    await HandleFailedAttempt($"Gagal login. Status code: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            await delayTask;
            await HandleFailedAttempt(ex.Message);
        }
        finally
        {
            OverlayLoading.IsVisible = false;
        }
    }

    private async Task AnimateOverlayPercentage()
    {
        for (int i = 0; i <= 100; i += 10)
        {
            OverlayPercentage.Text = $"Cek Otentikasi... {i}%";
            await Task.Delay(300);
        }
    }

    private async Task HandleFailedAttempt(string message)
    {
        await Toast.Make(message, ToastDuration.Short).Show();
        _failedAttempts++;

        if (_failedAttempts >= 3)
        {
            LoginBtn.IsEnabled = false;
            await Toast.Make("Gagal 3x. Tunggu 30 detik untuk mencoba lagi.", ToastDuration.Long).Show();
            
            #pragma warning disable CS4014
            Task.Run(async () =>
            {
                await Task.Delay(30000);
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _failedAttempts = 0;
                    LoginBtn.IsEnabled = true;
                    await Toast.Make("Silakan coba login kembali.", ToastDuration.Short).Show();
                });
            });
            #pragma warning restore CS4014
        }
    }

    private async void Biometric_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 100);
            await view.ScaleTo(1, 100);
        }

        bool isBiometricEnabled = Preferences.Get("use_biometric", false);
        if (!isBiometricEnabled)
        {
            await DisplayAlert("Biometrik Nonaktif", "Anda belum mengaktifkan fitur ini. Silakan login manual terlebih dahulu, lalu aktifkan di Pengaturan.", "OK");
            return;
        }

        string savedUsername = await SecureStorage.Default.GetAsync("last_username");
        string savedPassword = await SecureStorage.Default.GetAsync("last_password");

        if (string.IsNullOrEmpty(savedUsername) || string.IsNullOrEmpty(savedPassword))
        {
            await DisplayAlert("Data Tidak Ditemukan", "Sesi biometrik kedaluwarsa atau belum tersedia. Silakan login manual satu kali terlebih dahulu.", "OK");
            return;
        }

        var authResult = await BiometricAuthenticationService.Default.AuthenticateAsync(new AuthenticationRequest()
        {
            Title = "Login Cepat",
            Subtitle = "Akses akun FinanceApp Anda",
            Description = "Gunakan sidik jari atau Face ID untuk masuk tanpa mengetik password.",
            NegativeText = "Batal"
        }, CancellationToken.None);

        if (authResult.Status == BiometricResponseStatus.Success)
        {
            // Auto fill UI and trigger login programmatically
            UsernameEntry.Text = savedUsername;
            PasswordEntry.Text = savedPassword;
            OnLoginClicked(LoginBtn, EventArgs.Empty);
        }
        else
        {
            await Toast.Make("Otentikasi biometrik dibatalkan atau gagal.").Show();
        }
    }
}

public class LoginResponse
{
    public bool success { get; set; }
    public string message { get; set; }
    public int user_id { get; set; }
    public int id_users { get; set; }
    public string username { get; set; }
    public string email { get; set; }
    public string role { get; set; }
    public string nama_lengkap { get; set; }
    public string photo { get; set; }
    public string whatsapp { get; set; }
}