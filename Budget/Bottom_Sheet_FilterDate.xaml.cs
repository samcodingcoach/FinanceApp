using The49.Maui.BottomSheet;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using CommunityToolkit.Maui.Alerts;

namespace FinanceApp.Budget;

public partial class Bottom_Sheet_FilterDate : BottomSheet
{
    private List_Budget _parent;

    public Bottom_Sheet_FilterDate(List_Budget parent = null)
    {
        InitializeComponent();
        _parent = parent;
        
        DateTime now = DateTime.Now;
        DateStart.Date = new DateTime(now.Year, now.Month, 1);
        DateEnd.Date = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
    }

    private async void BtnCari_Clicked(object sender, EventArgs e)
    {
        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            DateTime dStart = DateStart.Date ?? DateTime.Now;
            DateTime dEnd = DateEnd.Date ?? DateTime.Now;
            
            string startFormat = dStart.ToString("yyyy-MM-dd");
            string endFormat = dEnd.ToString("yyyy-MM-dd");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                string url = $"{App.API_HOST}/budget?select=id_budget&is_active=eq.true&periode_awal=lte.{endFormat}&periode_akhir=gte.{startFormat}";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    if (json != "[]" && !string.IsNullOrWhiteSpace(json))
                    {
                        // Data ditemukan
                        if (_parent != null)
                        {
                            _parent.LoadDataWithFilter(startFormat, endFormat);
                        }
                        
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await this.DismissAsync();
                        });
                    }
                    else
                    {
                        // Tidak ditemukan
                        await Toast.Make("Data tidak ditemukan").Show();
                    }
                }
                else
                {
                    await Toast.Make("Gagal mencari data").Show();
                }
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Error: {ex.Message}").Show();
        }
    }
}