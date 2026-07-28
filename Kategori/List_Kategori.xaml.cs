using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;

namespace FinanceApp.Kategori;

public partial class List_Kategori : ContentPage
{
    private ObservableCollection<KategoriModel> _kategoriList;
    private ObservableCollection<KategoriModel> _allKategoriList;
    
    public List_Kategori()
    {
        InitializeComponent();
        _kategoriList = new ObservableCollection<KategoriModel>();
        _allKategoriList = new ObservableCollection<KategoriModel>();
        ListKategoriCollection.ItemsSource = _kategoriList;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync(true);
    }

    private async void KategoriRefresh_Refreshing(object sender, EventArgs e)
    {
        await LoadDataAsync(false);
    }

    private async Task LoadDataAsync(bool showOverlay = true)
    {
        if (showOverlay)
            OverlayLoading.IsVisible = true;

        var delayTask = showOverlay ? Task.Delay(3000) : Task.CompletedTask;

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;
            string apiUrl = App.API_HOST + "kategori";
            string bucketUrl = app?.BUCKET_URL ?? string.Empty;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");

                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<KategoriModel>>(responseContent);

                    _allKategoriList.Clear();

                    if (result != null)
                    {
                        var sorted = result.OrderByDescending(x => x.created_at).ToList();
                        foreach (var item in sorted)
                        {
                            item.BaseBucketUrl = bucketUrl;
                            _allKategoriList.Add(item);
                        }
                    }

                    RefreshLocalFilter();
                }
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Error", ex.Message, "OK");
            });
        }
        finally
        {
            if (showOverlay) await delayTask;
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (showOverlay) OverlayLoading.IsVisible = false;
                KategoriRefresh.IsRefreshing = false;
            });
        }
    }

    private void T_Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshLocalFilter();
    }

    private void RefreshLocalFilter()
    {
        var keyword = T_Search.Text?.ToLower() ?? string.Empty;

        _kategoriList.Clear();
        foreach (var item in _allKategoriList)
        {
            if (string.IsNullOrEmpty(keyword) || (!string.IsNullOrEmpty(item.nama_kategori) && item.nama_kategori.ToLower().Contains(keyword)))
            {
                _kategoriList.Add(item);
            }
        }
        L_ItemCount.Text = $"{_kategoriList.Count} Items";
    }

    private async void BtnMore_Tapped(object sender, TappedEventArgs e)
    {
        var img = sender as Image;
        if (img == null) return;

        // Animation feedback
        await img.ScaleTo(0.8, 100);
        await img.ScaleTo(1, 100);

        if (img.Source.ToString().Contains("close100.png"))
        {
            T_Search.Text = string.Empty; 
            StackLayoutSearch.IsVisible = false;
            StackLayoutTitle.IsVisible = true;
            img.Source = "more50gray.png";
            img.Rotation = 90;
        }
        else
        {
            string action = await DisplayActionSheet("Opsi", "Batal", null, "Search");
            if (action == "Search")
            {
                StackLayoutTitle.IsVisible = false;
                StackLayoutSearch.IsVisible = true;
                img.Source = "close100.png";
                img.Rotation = 0;
            }
        }
    }

    private async void FAB_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new FinanceApp.Kategori.New_Kategori());
    }
}

public class KategoriModel
{
    public int id_kategori { get; set; }
    public DateTime created_at { get; set; }
    public string nama_kategori { get; set; }
    public bool tipe { get; set; }
    public bool is_active { get; set; }
    public string icon { get; set; }

    [JsonIgnore]
    public string BaseBucketUrl { get; set; }

    [JsonIgnore]
    public Color BgColor
    {
        get
        {
            // true = Pemasukan (#16841E), false = Pengeluaran (#FA5252)
            return tipe ? Color.FromArgb("#16841E") : Color.FromArgb("#FA5252");
        }
    }

    [JsonIgnore]
    public double VisualOpacity
    {
        get
        {
            return is_active ? 1.0 : 0.5;
        }
    }

    [JsonIgnore]
    public string FullIconUrl
    {
        get
        {
            if (string.IsNullOrEmpty(icon)) return "nopic100.png";
            
            string cleanIcon = icon.StartsWith("/") ? icon.Substring(1) : icon;
            string cleanBucket = BaseBucketUrl.EndsWith("/") ? BaseBucketUrl : BaseBucketUrl + "/";
            
            return $"{cleanBucket}icon/{cleanIcon}";
        }
    }
}