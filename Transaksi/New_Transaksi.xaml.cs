using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;

namespace FinanceApp.Transaksi;

public partial class New_Transaksi : ContentPage
{
    private bool _isPemasukan = false;
    private ObservableCollection<KategoriData> _kategoris;

    public New_Transaksi()
	{
		InitializeComponent();
        _kategoris = new ObservableCollection<KategoriData>();
        KategoriCollectionView.ItemsSource = _kategoris;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Load data pengeluaran (karena _isPemasukan = false secara default)
        LoadKategori();
    }

    private async void LoadKategori()
    {
        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                // api endpoint: kategori?is_active=eq.true&tipe=eq.{_isPemasukan}
                string url = $"{App.API_HOST}/kategori?is_active=eq.true&tipe=eq.{_isPemasukan.ToString().ToLower()}";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<KategoriData>>(json);
                    
                    _kategoris.Clear();
                    if (data != null)
                    {
                        foreach (var item in data)
                        {
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
            await Toast.Make($"Error memuat kategori: {ex.Message}").Show();
        }
    }

    private void BPemasukan_Clicked(object sender, EventArgs e)
    {
        if (_isPemasukan) return;
        _isPemasukan = true;
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
        BPengeluaran.BackgroundColor = Colors.DarkCyan;
        BPengeluaran.TextColor = Colors.White;
        BPemasukan.BackgroundColor = Colors.Transparent;
        BPemasukan.TextColor = Colors.DarkGrey;
        LoadKategori();
    }

    private async void Kategori_Tapped(object sender, TappedEventArgs e)
    {
        var selectedItem = e.Parameter as KategoriData;
        if (selectedItem == null) return;
        
        foreach (var item in _kategoris)
        {
            item.IsSelected = (item == selectedItem);
        }

        await Toast.Make($"Memilih {selectedItem.nama_kategori} (ID: {selectedItem.id_kategori})").Show();
    }

    private int? _id_rekening = null;

    private async void TapRekening_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is StackLayout stackLayout)
        {
            await stackLayout.FadeToAsync(0.3, 100); // Turunkan opacity ke 0.3 dalam 100ms
            await stackLayout.FadeToAsync(1, 200);   // Kembalikan opacity ke 1 dalam 200ms

            var page = new Transaksi.PilihRekening_BottomSheet();
            page.HasHandle = true;
            page.HasBackdrop = true;
            
            page.RekeningSelected += async (s, rekening) => 
            {
                _id_rekening = rekening.id_rekening;
                LabelPilihRekening.Text = rekening.nama_rekening;
                
                await Toast.Make($"Rekening terpilih: {rekening.nama_rekening} (ID: {rekening.id_rekening})").Show();
            };

            _ = page.ShowAsync(Window);
        }
    }
}

public class KategoriData : INotifyPropertyChanged
{
    public int id_kategori { get; set; }
    public string? nama_kategori { get; set; }
    public bool tipe { get; set; }
    public bool is_active { get; set; }
    public string icon { get; set; }

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
                OnPropertyChanged(nameof(IconBackgroundColor));
            }
        }
    }

    [JsonIgnore]
    public string DisplayIcon 
    {
        get
        {
            if (string.IsNullOrEmpty(icon)) return "sampelicon1.png";
            var app = Application.Current as App;
            string bucket = app?.BUCKET_URL ?? "";
            if (!bucket.EndsWith("/")) bucket += "/";
            if (icon.StartsWith("/")) icon = icon.Substring(1);
            if (!icon.StartsWith("icon/")) icon = "icon/" + icon;
            return bucket + icon;
        }
    }

    [JsonIgnore]
    public Color IconBackgroundColor => IsSelected ? Colors.CornflowerBlue : Colors.LightGray;

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}