using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using CommunityToolkit.Maui.Alerts;

namespace FinanceApp.Transaksi;

public partial class List_Transaksi : ContentPage
{
    private ObservableCollection<TransaksiGroup> _allGroups;
    private List<TransaksiRowModel> _allRawData;
    private string _currentTab = "Semua";

    public ObservableCollection<TransaksiGroup> GroupedTransaksi { get; set; }

    public List_Transaksi()
    {
        InitializeComponent();
        _allGroups = new ObservableCollection<TransaksiGroup>();
        _allRawData = new List<TransaksiRowModel>();
        GroupedTransaksi = new ObservableCollection<TransaksiGroup>();
        
        BindableLayout.SetItemsSource(ListContainer, GroupedTransaksi);
        
        // Default to current month
        MonthPicker.Date = DateTime.Now;
    }

    private static DateTime _lastFetchTime = DateTime.MinValue;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Cache selama 30 menit agar tidak reload tiap pindah tab
        if ((DateTime.Now - _lastFetchTime).TotalMinutes < 30)
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
            // Delay 3 detik sesuai instruksi
            await Task.Delay(3000);
        }

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            DateTime date = Convert.ToDateTime(MonthPicker.Date);
            string p_tanggal_awal = new DateTime(date.Year, date.Month, 1).ToString("yyyy-MM-dd");
            string p_tanggal_akhir = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)).ToString("yyyy-MM-dd");

            var payload = new
            {
                p_tanggal_awal = p_tanggal_awal,
                p_tanggal_akhir = p_tanggal_akhir,
                p_keyword = T_Search.Text
            };

            using (var client = new HttpClient())
            {
                string url = $"{App.API_HOST}/rpc/get_transaksi";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                var content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<TransaksiRowModel>>(json);
                    
                    _allRawData.Clear();
                    if (data != null)
                    {
                        _allRawData.AddRange(data);
                    }
                    RefreshDisplay();
                }
                else
                {
                    await Toast.Make("Gagal memuat transaksi").Show();
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
        GroupedTransaksi.Clear();

        // 1. Filter by Tab (Semua, Pemasukan, Pengeluaran)
        var filteredData = _allRawData.Where(x => 
            _currentTab == "Semua" || 
            (_currentTab == "Pemasukan" && !x.tipe) || 
            (_currentTab == "Pengeluaran" && x.tipe)
        ).ToList();

        // 2. Group by Date
        var grouped = filteredData
            .GroupBy(x => x.created_at.Date)
            .OrderByDescending(g => g.Key)
            .ToList();

        foreach (var group in grouped)
        {
            string dateRelative = group.Key == DateTime.Now.Date ? "Hari ini" : group.Key == DateTime.Now.Date.AddDays(-1) ? "Kemarin" : group.Key.ToString("dddd");
            string dateDisplay = group.Key.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("id-ID"));
            
            GroupedTransaksi.Add(new TransaksiGroup(dateRelative, dateDisplay, group.ToList()));
        }
    }

    private void T_Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        LoadData(); // API handles keyword
    }

    private void MonthPicker_DateSelected(object sender, DateChangedEventArgs e)
    {
        LoadData();
    }

    private void TabFilter_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string tabName)
        {
            _currentTab = tabName;
            
            // Update UI for tabs
            TabSemua.BackgroundColor = _currentTab == "Semua" ? Colors.CornflowerBlue : Color.FromArgb("#bccbe6");
            TabPemasukan.BackgroundColor = _currentTab == "Pemasukan" ? Colors.CornflowerBlue : Color.FromArgb("#bccbe6");
            TabPengeluaran.BackgroundColor = _currentTab == "Pengeluaran" ? Colors.CornflowerBlue : Color.FromArgb("#bccbe6");

            RefreshDisplay(); // Local filter
        }
    }

    private async void BtnAdd_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new New_Transaksi());
    }

    private async void TransaksiItem_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border)
        {
            // Efek animasi tap
            await border.ScaleToAsync(0.95, 100);
            await border.ScaleToAsync(1.0, 100);

            if (e.Parameter is int id_transaksi)
            {
                //await Navigation.PushAsync(new );

                var page = new List_TransaksiDetail(id_transaksi);
                page.HasHandle = true;
                page.HasBackdrop = true;
                _ = page.ShowAsync(Window);
            

            }
        }
    }
}

public class TransaksiRowModel
{
    public int id_transaksi { get; set; }
    public DateTime created_at { get; set; }
    public string? no_faktur { get; set; }
    public int id_users { get; set; }
    public string? nama_lengkap { get; set; }
    public string? role { get; set; }
    public int id_rekening { get; set; }
    public string? nama_rekening { get; set; }
    public int id_kategori { get; set; }
    public string? nama_kategori { get; set; }
    public bool tipe { get; set; } // true = pengeluaran, false = pemasukan
    public string? keterangan { get; set; }
    public string? foto_transaksi { get; set; }
    public decimal total_transaksi { get; set; }

    [JsonIgnore]
    public string? ImageSource => string.IsNullOrEmpty(foto_transaksi) ? "nopic_nota.jpg" : ((App)Application.Current).BUCKET_URL + "/transaksi/" + foto_transaksi;
    
    [JsonIgnore]
    public string NominalDisplay => $"{(tipe ? "-" : "+")} Rp {total_transaksi:N0}";
    
    [JsonIgnore]
    public Color NominalColor => tipe ? Colors.OrangeRed : Colors.Green;

    [JsonIgnore]
    public string SubtitleDisplay => $"#{no_faktur ?? "TRX"} / {created_at:HH:mm} WIB";
    
    [JsonIgnore]
    public string? TitleDisplay => string.IsNullOrEmpty(keterangan) ? nama_kategori : keterangan;
}

public class TransaksiGroup
{
    public string DateRelative { get; set; }
    public string DateDisplay { get; set; }
    public List<TransaksiRowModel> Items { get; set; }

    public TransaksiGroup(string dateRelative, string dateDisplay, List<TransaksiRowModel> items)
    {
        DateRelative = dateRelative;
        DateDisplay = dateDisplay;
        Items = items;
    }
}