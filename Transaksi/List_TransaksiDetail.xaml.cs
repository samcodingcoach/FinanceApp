using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using The49.Maui.BottomSheet;

namespace FinanceApp.Transaksi;

public partial class List_TransaksiDetail : BottomSheet
{
    private int _id_transaksi;
    private ObservableCollection<TransaksiDetailModel> _allData;
    private ObservableCollection<TransaksiDetailModel> _filteredData;

    public List_TransaksiDetail(int id_transaksi)
    {
        InitializeComponent();
        _id_transaksi = id_transaksi;
        _allData = new ObservableCollection<TransaksiDetailModel>();
        _filteredData = new ObservableCollection<TransaksiDetailModel>();
        
        DetailCollection.ItemsSource = _filteredData;

        _ = LoadData();
    }

    

    private async Task LoadData()
    {
        LoadingOverlay.IsVisible = true;
        
        // Simulasikan delay minimal 3 detik sesuai instruksi "overlay dulu 3 detik + waktu meload data dari api"
        var delayTask = Task.Delay(3000);
        
        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                string url = $"{App.API_HOST}/transaksi_detail?id_transaksi=eq.{_id_transaksi}";
                var responseTask = client.GetAsync(url);
                
                await Task.WhenAll(delayTask, responseTask);
                
                var response = await responseTask;

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<TransaksiDetailModel>>(json);
                    
                    _allData.Clear();
                    _filteredData.Clear();
                    
                    if (data != null)
                    {
                        decimal sumTotal = 0;
                        foreach (var item in data)
                        {
                            _allData.Add(item);
                            _filteredData.Add(item);
                            sumTotal += item.subtotal;
                        }
                        L_TotalSubtotal.Text = $"Rp {sumTotal:N0}";
                    }
                }
                else
                {
                    await Toast.Make("Gagal memuat detail transaksi").Show();
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
        string keyword = (e.NewTextValue ?? "").ToLower();
        
        _filteredData.Clear();
        foreach(var item in _allData)
        {
            if (string.IsNullOrEmpty(keyword) || 
               (item.nama_barang_jasa?.ToLower().Contains(keyword) ?? false))
            {
                _filteredData.Add(item);
            }
        }
    }

    private async void Close_Tapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void B_Export_Clicked(object sender, EventArgs e)
    {
        await Toast.Make("Fitur Export PDF segera hadir!").Show();
    }
}

public class TransaksiDetailModel
{
    public int id_transaksi_detail { get; set; }
    public int id_transaksi { get; set; }
    public string? nama_barang_jasa { get; set; }
    public decimal harga { get; set; }
    public int jumlah { get; set; }
    public decimal subtotal { get; set; }

    [JsonIgnore]
    public string DetailHarga => $"{jumlah:N0} X Rp {harga:N0}";

    [JsonIgnore]
    public string SubtotalDisplay => $"Rp {subtotal:N0}";
}