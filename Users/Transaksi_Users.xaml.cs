using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace FinanceApp.Users;

public partial class Transaksi_Users : ContentPage
{
    public Transaksi_Users()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        BindableLayout.SetItemsSource(GroupedDataCollection, null);

        try 
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;
            int id_users = Preferences.Get("id_user", 3);

            string url = $"{App.API_HOST}/rpc/get_transaksi_detail_harian";

            var payload = new { p_id_users = id_users };
            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");
                
                var response = await client.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    string resJson = await response.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<TransaksiDetailHarianModel>>(resJson);

                    if (list != null && list.Count > 0)
                    {
                        var currentMonth = DateTime.Now.Month;
                        var currentYear = DateTime.Now.Year;

                        var groups = list
                            .Where(x => {
                                if (DateTime.TryParse(x.tanggal, out DateTime dt))
                                    return dt.Month == currentMonth && dt.Year == currentYear;
                                return false;
                            })
                            .GroupBy(x => x.tanggal)
                            .OrderByDescending(g => g.Key)
                            .Select(g => new TransaksiHarianGroup
                            {
                                TanggalFormat = FormatTanggalBulan(g.Key),
                                TotalFormat = $"Rp {g.Sum(x => x.subtotal):N0}",
                                Items = new ObservableCollection<TransaksiDetailHarianModel>(g)
                            }).ToList();

                        BindableLayout.SetItemsSource(GroupedDataCollection, groups);
                    }
                }
                else
                {
                    await DisplayAlert("Error", "Gagal memuat data", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Koneksi bermasalah: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            RefreshData.IsRefreshing = false;
        }
    }
    
    private string FormatTanggalBulan(string tanggal)
    {
        if (DateTime.TryParse(tanggal, out DateTime dt))
        {
            return dt.ToString("MMMM dd, yyyy", new System.Globalization.CultureInfo("en-US"));
        }
        return tanggal;
    }

    private async void Back_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 100);
            await view.ScaleTo(1, 100);
        }
        await Navigation.PopAsync();
    }

    private async void RefreshData_Refreshing(object sender, EventArgs e)
    {
        await LoadDataAsync();
    }

    private async void FAB_ExportPdf_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 50);
            await view.ScaleTo(1, 50);
        }
        await DisplayAlert("Info", "Fitur Export PDF akan dieksekusi nanti (Tunggu UI Mantap!)", "OK");
    }
}

public class TransaksiDetailHarianModel
{
    public string tanggal { get; set; }
    public int id_users { get; set; }
    public string nama_barang_jasa { get; set; }
    public int jumlah { get; set; }
    public decimal harga { get; set; }
    public decimal subtotal { get; set; }

    [JsonIgnore]
    public string HargaFormat => $"{jumlah} x Rp {harga:N0}";
    [JsonIgnore]
    public string SubtotalFormat => $"Rp {subtotal:N0}";
}

public class TransaksiHarianGroup
{
    public string TanggalFormat { get; set; }
    public string TotalFormat { get; set; }
    public ObservableCollection<TransaksiDetailHarianModel> Items { get; set; }
}