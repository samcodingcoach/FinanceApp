using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using The49.Maui.BottomSheet;

namespace FinanceApp.Favorit;

public partial class List_FavDetail : BottomSheet
{
    private int _id_fav;
    private Action? _onUpdated;
    private ObservableCollection<FavoritDetailModel> _allData;
    private ObservableCollection<FavoritDetailModel> _filteredData;

    public List_FavDetail(int id_fav, Action? onUpdated = null)
    {
        InitializeComponent();
        _id_fav = id_fav;
        _onUpdated = onUpdated;
        _allData = new ObservableCollection<FavoritDetailModel>();
        _filteredData = new ObservableCollection<FavoritDetailModel>();

        DetailCollection.ItemsSource = _filteredData;

        _ = LoadData();
    }

    private async Task LoadData()
    {
        L_LoadingText.Text = "Memuat Detail...";
        LoadingOverlay.IsVisible = true;
        var delayTask = Task.Delay(3000);

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                string url = $"{App.API_HOST}/favorit_transaksi_detail?id_fav=eq.{_id_fav}";
                var responseTask = client.GetAsync(url);

                await Task.WhenAll(delayTask, responseTask);

                var response = await responseTask;

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<FavoritDetailModel>>(json);

                    _allData.Clear();
                    _filteredData.Clear();

                    if (data != null)
                    {
                        foreach (var item in data)
                        {
                            item.OnPriceChanged = UpdateTotal;
                            _allData.Add(item);
                            _filteredData.Add(item);
                        }
                        UpdateTotal();
                    }
                }
                else
                {
                    await Toast.Make("Gagal memuat detail transaksi rutin").Show();
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

    private void UpdateTotal()
    {
        decimal sumTotal = 0;
        foreach (var item in _allData)
        {
            sumTotal += item.harga;
        }
        MainThread.BeginInvokeOnMainThread(() =>
        {
            string formattedTotal = sumTotal == 0 ? "0" : sumTotal.ToString("N0");
            L_TotalSubtotal.Text = "Rp " + formattedTotal;
        });
    }

    private void T_Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        string keyword = (e.NewTextValue ?? "").Trim().ToLower();

        _filteredData.Clear();
        foreach (var item in _allData)
        {
            if (string.IsNullOrEmpty(keyword) ||
               (item.nama_barang_jasa?.ToLower().Contains(keyword) ?? false))
            {
                _filteredData.Add(item);
            }
        }
    }

    private async void BtnUpdate_Clicked(object sender, EventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.95, 80);
            await view.ScaleTo(1.0, 80);
        }

        if (_allData.Count == 0)
        {
            await Toast.Make("Tidak ada data untuk diperbarui").Show();
            return;
        }

        // Validasi input
        foreach (var item in _allData)
        {
            if (string.IsNullOrWhiteSpace(item.nama_barang_jasa))
            {
                await Toast.Make("Nama barang/jasa tidak boleh kosong").Show();
                return;
            }
        }

        L_LoadingText.Text = "Menyimpan Perubahan...";
        LoadingOverlay.IsVisible = true;

        // Overlay delay 3 detik standar
        var delayTask = Task.Delay(3000);

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);
                client.DefaultRequestHeaders.Add("Prefer", "return=minimal");

                bool allSuccess = true;
                decimal newTotalHarga = 0;

                // 1. Update setiap baris detail ke tabel favorit_transaksi_detail
                foreach (var item in _allData)
                {
                    newTotalHarga += item.harga;

                    string patchUrl = $"{App.API_HOST}/favorit_transaksi_detail?id_fav_detail=eq.{item.id_fav_detail}";
                    var payload = new
                    {
                        nama_barang_jasa = item.nama_barang_jasa,
                        harga = item.harga
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    var req = new HttpRequestMessage(new HttpMethod("PATCH"), patchUrl) { Content = content };
                    var res = await client.SendAsync(req);

                    if (!res.IsSuccessStatusCode)
                    {
                        allSuccess = false;
                    }
                }

                // 2. Update total_harga di tabel header favorit_transaksi
                string updateHeaderUrl = $"{App.API_HOST}/favorit_transaksi?id_fav=eq.{_id_fav}";
                var headerPayload = new
                {
                    total_harga = newTotalHarga
                };
                var headerContent = new StringContent(JsonConvert.SerializeObject(headerPayload), Encoding.UTF8, "application/json");
                var headerReq = new HttpRequestMessage(new HttpMethod("PATCH"), updateHeaderUrl) { Content = headerContent };
                await client.SendAsync(headerReq);

                await delayTask;

                if (allSuccess)
                {
                    await Toast.Make("Rincian berhasil diperbarui!").Show();

                    // Panggil callback reload data di halaman induk ListFav
                    _onUpdated?.Invoke();

                    await this.DismissAsync();
                }
                else
                {
                    await Toast.Make("Sebagian data gagal diperbarui").Show();
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

    private async void Close_Tapped(object sender, TappedEventArgs e)
    {
        await this.DismissAsync();
    }
}

public class FavoritDetailModel : INotifyPropertyChanged
{
    private string? _nama_barang_jasa;
    private decimal _harga;
    private string _hargaInput = "0";

    public int id_fav_detail { get; set; }
    public int id_fav { get; set; }

    public string? nama_barang_jasa
    {
        get => _nama_barang_jasa;
        set
        {
            if (_nama_barang_jasa != value)
            {
                _nama_barang_jasa = value;
                OnPropertyChanged();
            }
        }
    }

    public decimal harga
    {
        get => _harga;
        set
        {
            if (_harga != value)
            {
                _harga = value;
                _hargaInput = value.ToString("0");
                OnPropertyChanged();
                OnPropertyChanged(nameof(HargaInput));
                OnPriceChanged?.Invoke();
            }
        }
    }

    [JsonIgnore]
    public string HargaInput
    {
        get => _hargaInput;
        set
        {
            if (_hargaInput != value)
            {
                _hargaInput = value;
                OnPropertyChanged();

                // Parse input numeric secara real-time untuk kalkulasi live subtotal
                string cleanVal = value?.Replace(".", "").Replace(",", "").Trim() ?? "0";
                if (decimal.TryParse(cleanVal, out decimal parsed))
                {
                    _harga = parsed;
                    OnPropertyChanged(nameof(harga));
                    OnPriceChanged?.Invoke();
                }
            }
        }
    }

    [JsonIgnore]
    public Action? OnPriceChanged { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}