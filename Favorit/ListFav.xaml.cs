using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using CommunityToolkit.Maui.Alerts;

namespace FinanceApp.Favorit;

public partial class ListFav : ContentPage
{
    private List<FavoritTransaksiModel> _allRawData;
    private string _currentTab = "Semua";

    public ObservableCollection<FavoritTransaksiModel> FavoritList { get; set; }

    public ListFav()
    {
        InitializeComponent();
        _allRawData = new List<FavoritTransaksiModel>();
        FavoritList = new ObservableCollection<FavoritTransaksiModel>();

        BindableLayout.SetItemsSource(ListContainer, FavoritList);
    }

    private static DateTime _lastFetchTime = DateTime.MinValue;

    public static void ResetCache()
    {
        _lastFetchTime = DateTime.MinValue;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Cache 30 menit agar efisien
        if ((DateTime.Now - _lastFetchTime).TotalMinutes < 30 && _allRawData.Count > 0)
        {
            return;
        }

        _lastFetchTime = DateTime.Now;
        LoadData();
    }

    private async void LoadData(bool isRefresh = false)
    {
        if (!isRefresh)
        {
            LoadingOverlay.IsVisible = true;
            // Delay 3 detik sesuai standar project
            await Task.Delay(3000);
        }

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                string url = $"{App.API_HOST}/rpc/get_favorit_transaksi";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                var response = await client.PostAsync(url, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var settings = new JsonSerializerSettings
                    {
                        DateParseHandling = DateParseHandling.DateTimeOffset
                    };
                    var data = JsonConvert.DeserializeObject<List<FavoritTransaksiModel>>(json, settings);

                    _allRawData.Clear();
                    if (data != null)
                    {
                        _allRawData.AddRange(data);
                    }
                    RefreshDisplay();
                }
                else
                {
                    await Toast.Make("Gagal memuat transaksi favorit").Show();
                }
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Error: {ex.Message}").Show();
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
            RefreshViewContainer.IsRefreshing = false;
        }
    }

    private void RefreshViewContainer_Refreshing(object sender, EventArgs e)
    {
        LoadData(true);
    }

    private void RefreshDisplay()
    {
        FavoritList.Clear();

        string keyword = T_Search.Text?.Trim().ToLower() ?? string.Empty;

        var filteredData = _allRawData.Where(x =>
            string.IsNullOrEmpty(keyword) ||
            (x.keterangan != null && x.keterangan.ToLower().Contains(keyword)) ||
            (x.nama_kategori != null && x.nama_kategori.ToLower().Contains(keyword))
        ).OrderBy(x => x.setiap_tanggal).ToList();

        foreach (var item in filteredData)
        {
            FavoritList.Add(item);
        }
    }

    private void T_Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshDisplay();
    }

    private async void BtnAdd_Clicked(object sender, EventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 100);
            await view.ScaleTo(1.0, 100);
        }
        await Navigation.PushAsync(new NewFav());
    }

    private async void FavItem_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border)
        {
            await border.ScaleTo(0.95, 100);
            await border.ScaleTo(1.0, 100);

            if (e.Parameter is int id_fav)
            {
                // Action saat item favorit ditekan
            }
        }
    }
}

public class FavoritTransaksiModel
{
    public int id_fav { get; set; }
    public DateTimeOffset created_at { get; set; }
    public int id_kategori { get; set; }
    public string? keterangan { get; set; }
    public int setiap_tanggal { get; set; }
    public string? nama_kategori { get; set; }
    public bool tipe { get; set; } // false = pengeluaran, true = pemasukan
    public string? icon { get; set; }
    public decimal total_harga { get; set; }

    [JsonIgnore]
    public Color BgColor => tipe ? Color.FromArgb("#16841E") : Color.FromArgb("#FA5252");

    [JsonIgnore]
    public string FullIconUrl
    {
        get
        {
            if (string.IsNullOrEmpty(icon)) return "nopic100.png";
            if (icon.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return icon;
            var app = Application.Current as App;
            string? cleanBucket = app?.BUCKET_URL;
            if (!string.IsNullOrEmpty(cleanBucket) && !cleanBucket.EndsWith("/")) cleanBucket += "/";
            string cleanIcon = icon.StartsWith("/") ? icon.Substring(1) : icon;
            return $"{cleanBucket}icon/{cleanIcon}";
        }
    }

    [JsonIgnore]
    public string NominalDisplay => $"{(tipe ? "+" : "-")} Rp {total_harga:N0}";

    [JsonIgnore]
    public Color NominalColor => tipe ? Colors.Green : Colors.OrangeRed;

    [JsonIgnore]
    public string? TitleDisplay => string.IsNullOrEmpty(keterangan) ? nama_kategori : keterangan;

    [JsonIgnore]
    public string ScheduleDisplay => $"Setiap tanggal: {setiap_tanggal}";

    [JsonIgnore]
    public string TypeText => tipe ? "Pemasukan" : "Pengeluaran";

    [JsonIgnore]
    public Color TypeBadgeBg => tipe ? Color.FromArgb("#e6f4ea") : Color.FromArgb("#fce8e6");

    [JsonIgnore]
    public Color TypeBadgeTextColor => tipe ? Color.FromArgb("#137333") : Color.FromArgb("#c5221f");
}