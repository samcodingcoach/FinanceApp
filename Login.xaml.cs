using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

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
                    var resultList = JsonConvert.DeserializeObject<List<LoginResponse>>(responseContent);
                    if (resultList != null && resultList.Count > 0)
                    {
                        var result = resultList[0];
                        if (result.success)
                        {
                            _failedAttempts = 0;
                            Preferences.Set("user_data", JsonConvert.SerializeObject(result));
                            MainThread.BeginInvokeOnMainThread(async () => 
                            {
                                await Navigation.PopModalAsync();
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
}

public class LoginResponse
{
    public bool success { get; set; }
    public string message { get; set; }
    public int user_id { get; set; }
    public string username { get; set; }
    public string email { get; set; }
    public string role { get; set; }
    public string nama_lengkap { get; set; }
    public string photo { get; set; }
    public string whatsapp { get; set; }
}