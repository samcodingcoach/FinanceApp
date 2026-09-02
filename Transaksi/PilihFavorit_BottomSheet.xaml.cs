using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using The49.Maui.BottomSheet;

namespace FinanceApp.Transaksi;

public partial class PilihFavorit_BottomSheet : BottomSheet
{
    private List<FavoritImportItemModel> _allData = new();
    private ObservableCollection<FavoritImportItemModel> _filteredData = new();

    public event EventHandler<FavoritImportItemModel>? FavoritSelected;

    public PilihFavorit_BottomSheet()
    {
        InitializeComponent();
        FavCollection.ItemsSource = _filteredData;
        _ = LoadData();
    }

    private async Task LoadData()
    {
        LoadingOverlay.IsVisible = true;
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
                    var data = JsonConvert.DeserializeObject<List<FavoritImportItemModel>>(json, settings);

                    _allData.Clear();
                    _filteredData.Clear();

                    if (data != null)
                    {
                        foreach (var item in data.OrderBy(x => x.setiap_tanggal))
                        {
                            _allData.Add(item);
                            _filteredData.Add(item);
                        }
                    }
                }
                else
                {
                    await Toast.Make("Gagal memuat daftar favorit").Show();
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
        }
    }

    private void T_Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        string keyword = (e.NewTextValue ?? "").Trim().ToLower();
        _filteredData.Clear();
        foreach (var item in _allData)
        {
            if (string.IsNullOrEmpty(keyword) ||
                (item.keterangan != null && item.keterangan.ToLower().Contains(keyword)) ||
                (item.nama_kategori != null && item.nama_kategori.ToLower().Contains(keyword)))
            {
                _filteredData.Add(item);
            }
        }
    }

    private async void FavItem_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is FavoritImportItemModel selectedFav)
        {
            FavoritSelected?.Invoke(this, selectedFav);
            await this.DismissAsync();
        }
    }

    private async void Close_Tapped(object sender, TappedEventArgs e)
    {
        await this.DismissAsync();
    }
}

public class FavoritImportItemModel
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
    public string ScheduleDisplay => $"Tgl {setiap_tanggal}";

    [JsonIgnore]
    public string TypeText => tipe ? "Pemasukan" : "Pengeluaran";

    [JsonIgnore]
    public Color TypeBadgeBg => tipe ? Color.FromArgb("#e6f4ea") : Color.FromArgb("#fce8e6");

    [JsonIgnore]
    public Color TypeBadgeTextColor => tipe ? Color.FromArgb("#137333") : Color.FromArgb("#c5221f");
}
