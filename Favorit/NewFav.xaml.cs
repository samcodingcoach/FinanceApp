using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;

namespace FinanceApp.Favorit;

public partial class NewFav : ContentPage, INotifyPropertyChanged
{
    private bool _isPemasukan = false;
    private ObservableCollection<FavKategoriData> _kategoris;
    private int? _selectedIdKategori = null;

    public ObservableCollection<FavFormDetailItem> DetailItems { get; set; } = new ObservableCollection<FavFormDetailItem>();

    public NewFav()
    {
        InitializeComponent();

        _kategoris = new ObservableCollection<FavKategoriData>();
        KategoriCollectionView.ItemsSource = _kategoris;

        // Inisialisasi pilihan tanggal 1 - 31
        for (int i = 1; i <= 31; i++)
        {
            PickerTanggal.Items.Add(i.ToString());
        }
        PickerTanggal.SelectedIndex = 0; // Default tanggal 1

        DetailItems.CollectionChanged += (s, e) => CalculateTotalFromDetails();
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_kategoris.Count == 0)
        {
            LoadKategori();
        }
        if (DetailItems.Count == 0)
        {
            AddNewDetailItem();
        }
    }

    private async void LoadKategori()
    {
        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                string url = $"{App.API_HOST}/kategori?is_active=eq.true&tipe=eq.{_isPemasukan.ToString().ToLower()}";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<FavKategoriData>>(json);

                    _kategoris.Clear();
                    if (data != null)
                    {
                        foreach (var item in data)
                        {
                            if (_selectedIdKategori != null && item.id_kategori == _selectedIdKategori.Value)
                            {
                                item.IsSelected = true;
                            }
                            _kategoris.Add(item);
                        }
                    }
                }
                else
                {
                    await Toast.Make("Gagal memuat kategori").Show();
                }
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Error: {ex.Message}").Show();
        }
    }

    private void BPemasukan_Clicked(object sender, EventArgs e)
    {
        if (_isPemasukan) return;
        _isPemasukan = true;
        _selectedIdKategori = null;
        BPemasukan.BackgroundColor = Colors.DarkCyan;
        BPemasukan.TextColor = Colors.White;
        BPengeluaran.BackgroundColor = Colors.Transparent;
        BPengeluaran.TextColor = Colors.DarkGrey;
        LoadKategori();
    }

    private void BPengeluaran_Clicked(object sender, EventArgs e)
    {
        if (!_isPemasukan) return;
        _isPemasukan = false;
        _selectedIdKategori = null;
        BPengeluaran.BackgroundColor = Colors.DarkCyan;
        BPengeluaran.TextColor = Colors.White;
        BPemasukan.BackgroundColor = Colors.Transparent;
        BPemasukan.TextColor = Colors.DarkGrey;
        LoadKategori();
    }

    private void Kategori_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not FavKategoriData selectedItem) return;

        _selectedIdKategori = selectedItem.id_kategori;
        foreach (var item in _kategoris)
        {
            item.IsSelected = (item.id_kategori == selectedItem.id_kategori);
        }
    }

    private void AddNewDetailItem()
    {
        var newItem = new FavFormDetailItem();
        newItem.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(FavFormDetailItem.HargaNumeric))
            {
                CalculateTotalFromDetails();
            }
        };
        DetailItems.Add(newItem);
    }

    private void BtnAddDetailItem_Clicked(object sender, EventArgs e)
    {
        AddNewDetailItem();
    }

    private void DeleteDetailItem_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is FavFormDetailItem item)
        {
            if (DetailItems.Count <= 1)
            {
                item.NamaBarang = string.Empty;
                item.HargaString = string.Empty;
                return;
            }
            DetailItems.Remove(item);
            CalculateTotalFromDetails();
        }
    }

    private bool _isUpdatingTotal = false;

    private void CalculateTotalFromDetails()
    {
        if (_isUpdatingTotal) return;

        decimal total = 0;
        foreach (var item in DetailItems)
        {
            total += item.HargaNumeric ?? 0;
        }

        _isUpdatingTotal = true;
        if (total > 0)
        {
            T_Nominal.Text = total.ToString("N0");
        }
        _isUpdatingTotal = false;
    }

    private void T_Nominal_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingTotal) return;
        // User juga bisa ketik total langsung jika tidak pakai rincian banyak
    }

    private async void BSimpan_Clicked(object sender, EventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 80);
            await view.ScaleTo(1.0, 80);
        }

        var selectedKategori = _kategoris.FirstOrDefault(k => k.IsSelected);
        if (selectedKategori == null)
        {
            await Toast.Make("Pilih kategori terlebih dahulu!").Show();
            return;
        }

        if (PickerTanggal.SelectedIndex < 0)
        {
            await Toast.Make("Pilih tanggal jadwal rutin!").Show();
            return;
        }

        int setiapTanggal = PickerTanggal.SelectedIndex + 1;

        string cleanNominal = new string((T_Nominal.Text ?? "").Where(char.IsDigit).ToArray());
        decimal.TryParse(cleanNominal, out decimal nominalValue);

        OverlayLoading.IsVisible = true;
        OverlayText.Text = "Menyimpan Transaksi Rutin...";

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
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");

                // 1. Simpan Header: POST APP_HOST + favorit_transaksi
                var favHeaderData = new
                {
                    setiap_tanggal = setiapTanggal,
                    id_kategori = selectedKategori.id_kategori,
                    keterangan = T_Keterangan.Text ?? selectedKategori.nama_kategori
                };

                string favJson = JsonConvert.SerializeObject(favHeaderData);
                var headerContent = new StringContent(favJson, Encoding.UTF8, "application/json");
                string urlHeader = $"{App.API_HOST}/favorit_transaksi";

                var headerResponse = await client.PostAsync(urlHeader, headerContent);

                if (!headerResponse.IsSuccessStatusCode)
                {
                    await delayTask;
                    await Toast.Make("Gagal menyimpan transaksi rutin").Show();
                    return;
                }

                string headerResultJson = await headerResponse.Content.ReadAsStringAsync();
                var insertedHeaders = JsonConvert.DeserializeObject<List<FavoritInsertedModel>>(headerResultJson);
                var insertedHeader = insertedHeaders?.FirstOrDefault();

                if (insertedHeader == null || insertedHeader.id_fav <= 0)
                {
                    await delayTask;
                    await Toast.Make("Gagal mendapatkan ID transaksi rutin").Show();
                    return;
                }

                int newIdFav = insertedHeader.id_fav;

                // 2. Simpan Detail Items: POST APP_HOST + favorit_transaksi_detail
                var validDetails = DetailItems.Where(d => !string.IsNullOrWhiteSpace(d.NamaBarang) || (d.HargaNumeric ?? 0) > 0).ToList();

                // Jika user tidak mengisi detail, buatkan 1 baris default dari nominal
                if (validDetails.Count == 0 && nominalValue > 0)
                {
                    validDetails.Add(new FavFormDetailItem
                    {
                        NamaBarang = favHeaderData.keterangan,
                        HargaString = nominalValue.ToString("0")
                    });
                }

                foreach (var detail in validDetails)
                {
                    var detailData = new
                    {
                        id_fav = newIdFav,
                        nama_barang_jasa = string.IsNullOrWhiteSpace(detail.NamaBarang) ? favHeaderData.keterangan : detail.NamaBarang,
                        harga = detail.HargaNumeric ?? nominalValue
                    };

                    string detailJson = JsonConvert.SerializeObject(detailData);
                    var detailContent = new StringContent(detailJson, Encoding.UTF8, "application/json");
                    string urlDetail = $"{App.API_HOST}/favorit_transaksi_detail";

                    await client.PostAsync(urlDetail, detailContent);
                }

                await delayTask;

                await Toast.Make("Transaksi rutin berhasil disimpan!").Show();

                // Reset cache ListFav dan kembali
                ListFav.ResetCache();
                await Navigation.PopAsync();
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Error: {ex.Message}").Show();
        }
        finally
        {
            OverlayLoading.IsVisible = false;
        }
    }

    private async void Back_Tapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}

public class FavKategoriData : INotifyPropertyChanged
{
    public int id_kategori { get; set; }
    public string nama_kategori { get; set; } = string.Empty;
    public string? icon { get; set; }
    public bool tipe { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BorderBackgroundColor));
                OnPropertyChanged(nameof(BorderStrokeColor));
                OnPropertyChanged(nameof(BorderThicknessVal));
            }
        }
    }

    [JsonIgnore]
    public Color BorderBackgroundColor => IsSelected ? Color.FromArgb("#e8f0fe") : Colors.White;

    [JsonIgnore]
    public Color BorderStrokeColor => IsSelected ? Colors.CornflowerBlue : Color.FromArgb("#cccccc");

    [JsonIgnore]
    public double BorderThicknessVal => IsSelected ? 2 : 0.5;

    [JsonIgnore]
    public Color IconBackgroundColor => tipe ? Color.FromArgb("#16841E") : Color.FromArgb("#FA5252");

    [JsonIgnore]
    public string DisplayIcon
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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class FavFormDetailItem : INotifyPropertyChanged
{
    private string _namaBarang = string.Empty;
    public string NamaBarang
    {
        get => _namaBarang;
        set { _namaBarang = value; OnPropertyChanged(); }
    }

    public decimal? HargaNumeric { get; private set; }

    private string _hargaString = string.Empty;
    public string HargaString
    {
        get => _hargaString;
        set
        {
            if (_hargaString == value) return;
            _hargaString = value;

            if (string.IsNullOrWhiteSpace(value))
            {
                HargaNumeric = null;
            }
            else
            {
                string cleanStr = new string(value.Where(char.IsDigit).ToArray());
                if (decimal.TryParse(cleanStr, out decimal parsedValue))
                {
                    HargaNumeric = parsedValue >= 0 ? parsedValue : 0;
                }
                else
                {
                    HargaNumeric = null;
                }
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(HargaNumeric));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class FavoritInsertedModel
{
    public int id_fav { get; set; }
    public DateTimeOffset created_at { get; set; }
    public int id_kategori { get; set; }
    public string? keterangan { get; set; }
    public int setiap_tanggal { get; set; }
}