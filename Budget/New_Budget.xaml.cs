using System.Net.Http.Headers;
using System.Text;
using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;

namespace FinanceApp.Budget;

public partial class New_Budget : ContentPage
{
    int id_users = 3;
    
    public New_Budget()
    {
        InitializeComponent();
        
        DateTime now = DateTime.Now;
        DateStart.Date = new DateTime(now.Year, now.Month, 1);
        DateEnd.Date = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
    }

    private bool _isFormattingSaldo = false;

    private void e_totalrencana_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isFormattingSaldo) return;
        if (string.IsNullOrEmpty(e.NewTextValue)) return;
        
        string cleanString = e.NewTextValue.Replace(".", "").Replace(",", "");
        if (long.TryParse(cleanString, out long result))
        {
            string formatted = result.ToString("N0", new System.Globalization.CultureInfo("id-ID"));
            if (formatted != (e.NewTextValue ?? ""))
            {
                Dispatcher.Dispatch(() =>
                {
                    _isFormattingSaldo = true;
                    e_totalrencana.Text = formatted;
                    e_totalrencana.CursorPosition = formatted.Length;
                    _isFormattingSaldo = false;
                });
            }
        }
    }

    private async void BSimpan_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e_totalrencana.Text))
        {
            await Toast.Make("Total rencana tidak boleh kosong").Show();
            return;
        }

        string rawNominal = e_totalrencana.Text.Replace(".", "").Replace(",", "");
        if (!decimal.TryParse(rawNominal, out decimal totalRencana))
        {
            await Toast.Make("Total rencana tidak valid").Show();
            return;
        }

        OverlayLoading.IsVisible = true;
        
        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                // 1. Cek periode
                DateTime dStart = DateStart.Date ?? DateTime.Now;
                DateTime dEnd = DateEnd.Date ?? DateTime.Now;

                string startFormat = dStart.ToString("yyyy-MM-dd");
                string endFormat = dEnd.ToString("yyyy-MM-dd");

                string checkUrl = $"{App.API_HOST}/budget?select=id_budget&is_active=eq.true&periode_awal=lte.{endFormat}&periode_akhir=gte.{startFormat}";
                
                var checkResponse = await client.GetAsync(checkUrl);
                if (checkResponse.IsSuccessStatusCode)
                {
                    string checkJson = await checkResponse.Content.ReadAsStringAsync();
                    if (checkJson != "[]" && !string.IsNullOrWhiteSpace(checkJson))
                    {
                        // Ada yang bentrok
                        await Toast.Make("Periode bulan ini sudah diatur sebelumnya!").Show();
                        return;
                    }
                }

                // 2. Jika aman, lanjut save (POST)
                string postUrl = $"{App.API_HOST}/budget";
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");

                var payload = new
                {
                    periode_awal = dStart.ToString("MM/dd/yyyy"),
                    periode_akhir = dEnd.ToString("MM/dd/yyyy"),
                    id_users = id_users,
                    deskripsi = e_deskripsi.Text ?? "",
                    total_rencana = totalRencana
                };

                var jsonContent = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(postUrl, jsonContent);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    await Toast.Make("Berhasil membuat anggaran baru").Show();
                    
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
                    await Toast.Make($"Gagal menyimpan: {response.StatusCode}").Show();
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