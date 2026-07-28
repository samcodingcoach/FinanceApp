using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;

namespace FinanceApp.Rekening;

public partial class Edit_Rekening : ContentPage
{
    private AkunRekening _rekening;
    private bool _isFormattingSaldo = false;

    public Edit_Rekening(AkunRekening rekening)
    {
        InitializeComponent();
        _rekening = rekening;
        
        e_nama_rekening.Text = _rekening.nama_rekening;
        e_saldoawal.Text = _rekening.saldo_awal.ToString(); // Otomatis format karena TextChanged
        c_isactive.IsChecked = _rekening.is_active;
    }

    protected override bool OnBackButtonPressed()
    {
        HandleCancel();
        return true;
    }

    private void Cancel_Clicked(object sender, EventArgs e)
    {
        HandleCancel();
    }

    private async void HandleCancel()
    {
        bool answer = await DisplayAlert("Konfirmasi", "Apakah Anda yakin ingin membatalkan?", "Ya", "Tidak");
        if (answer)
        {
            await Navigation.PopAsync();
        }
    }

    private void e_saldoawal_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isFormattingSaldo) return;

        var culture = new System.Globalization.CultureInfo("id-ID");

        string raw = (e.NewTextValue ?? "")
            .Replace(".", "")
            .Replace("Rp", "")
            .Trim();

        if (string.IsNullOrEmpty(raw))
        {
            return;
        }

        if (!double.TryParse(raw, out double nominal) || nominal < 0)
            return;

        string formatted = nominal.ToString("N0", culture);

        if (formatted != (e.NewTextValue ?? ""))
        {
            Dispatcher.Dispatch(() =>
            {
                _isFormattingSaldo = true;         
                e_saldoawal.Text = formatted; 
                e_saldoawal.CursorPosition = formatted.Length; 
                _isFormattingSaldo = false;        
            });
        }
    }

    private async void BUpdate_Clicked(object sender, EventArgs e)
    {
        string namaRekening = e_nama_rekening.Text?.Trim();
        string saldoStr = e_saldoawal.Text?.Trim().Replace(".", "");

        if (string.IsNullOrEmpty(namaRekening))
        {
            ShowToast("Nama rekening harus diisi");
            return;
        }

        if (string.IsNullOrEmpty(saldoStr) || !double.TryParse(saldoStr, out double saldoAwal) || saldoAwal < 0)
        {
            ShowToast("Saldo awal tidak valid");
            return;
        }

        bool isActive = c_isactive.IsChecked;

        OverlayLoading.IsVisible = true;
        var delayTask = Task.Delay(3000);
        bool isSuccess = false;
        string errorMsg = "";

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;
            string baseUrl = App.API_HOST + $"akun_rekening?id_rekening=eq.{_rekening.id_rekening}";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                // Uniqueness check (pastikan nama tidak dipakai akun lain)
                var checkUrl = App.API_HOST + $"akun_rekening?nama_rekening=eq.{Uri.EscapeDataString(namaRekening)}&id_rekening=neq.{_rekening.id_rekening}";
                var checkResponse = await client.GetAsync(checkUrl);
                if (checkResponse.IsSuccessStatusCode)
                {
                    string checkContent = await checkResponse.Content.ReadAsStringAsync();
                    var existingData = JsonConvert.DeserializeObject<List<object>>(checkContent);
                    if (existingData != null && existingData.Count > 0)
                    {
                        errorMsg = "Nama rekening sudah digunakan oleh akun lain";
                        return; // Menuju block finally
                    }
                }

                // PATCH Update
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");
                var requestData = new
                {
                    nama_rekening = namaRekening,
                    saldo_awal = saldoAwal,
                    saldo_akhir = saldoAwal, // Sesuai arahan, saldo_akhir juga di-update
                    is_active = isActive ? 1 : 0
                };

                string jsonPayload = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), baseUrl)
                {
                    Content = content
                };

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    isSuccess = true;
                }
                else
                {
                    string errContent = await response.Content.ReadAsStringAsync();
                    errorMsg = "Gagal update: " + errContent;
                }
            }
        }
        catch (Exception ex)
        {
            errorMsg = "Error: " + ex.Message;
        }
        finally
        {
            await delayTask;
            
            OverlayLoading.IsVisible = false;

            if (isSuccess)
            {
                ShowToast("Rekening berhasil diperbarui");
                await Navigation.PopAsync();
            }
            else if (!string.IsNullOrEmpty(errorMsg))
            {
                ShowToast(errorMsg);
            }
        }
    }

    private void ShowToast(string message)
    {
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        var toast = Toast.Make(message, ToastDuration.Short, 14);
        toast.Show(cancellationTokenSource.Token);
    }
}