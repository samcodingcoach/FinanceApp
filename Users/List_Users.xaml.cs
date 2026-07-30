using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;

namespace FinanceApp.Users;

public partial class List_Users : ContentPage
{
    private ObservableCollection<UserModel> _allUsers;
    private ObservableCollection<UserModel> _displayUsers;
    private string _currentFilter = "Semua";

    public List_Users()
    {
        _allUsers = new ObservableCollection<UserModel>();
        _displayUsers = new ObservableCollection<UserModel>();
        InitializeComponent();
        ListUsersCollection.ItemsSource = _displayUsers;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadData();
    }

    private async void LoadData()
    {
        OverlayLoading.IsVisible = true;
        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;
            string url = App.API_HOST + "users";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<UserModel>>(json);

                    _allUsers.Clear();
                    if (data != null)
                    {
                        string bucketUrl = app?.BUCKET_URL ?? string.Empty;
                        foreach (var item in data)
                        {
                            item.BaseBucketUrl = bucketUrl;
                            _allUsers.Add(item);
                        }
                    }

                    RefreshLocalFilter();
                }
                else
                {
                    await Toast.Make($"Gagal memuat data: {response.StatusCode}").Show();
                }
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Terjadi kesalahan: {ex.Message}").Show();
        }
        finally
        {
            OverlayLoading.IsVisible = false;
            UsersRefresh.IsRefreshing = false;
        }
    }

    private void Filter_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap)
        {
            _currentFilter = tap.CommandParameter?.ToString() ?? "Semua";
            UpdateFilterUI();
            RefreshLocalFilter();
        }
    }

    private void UpdateFilterUI()
    {
        FilterSemua.BackgroundColor = Colors.White;
        L_FilterSemua.TextColor = Color.FromArgb("#444");
        FilterAyah.BackgroundColor = Colors.White;
        L_FilterAyah.TextColor = Color.FromArgb("#444");
        FilterIbu.BackgroundColor = Colors.White;
        L_FilterIbu.TextColor = Color.FromArgb("#444");
        FilterAnak.BackgroundColor = Colors.White;
        L_FilterAnak.TextColor = Color.FromArgb("#444");
        FilterLainnya.BackgroundColor = Colors.White;
        L_FilterLainnya.TextColor = Color.FromArgb("#444");

        Border selectedBorder = null;
        Label selectedLabel = null;

        switch (_currentFilter)
        {
            case "Semua":
                selectedBorder = FilterSemua;
                selectedLabel = L_FilterSemua;
                break;
            case "Ayah":
                selectedBorder = FilterAyah;
                selectedLabel = L_FilterAyah;
                break;
            case "Ibu":
                selectedBorder = FilterIbu;
                selectedLabel = L_FilterIbu;
                break;
            case "Anak":
                selectedBorder = FilterAnak;
                selectedLabel = L_FilterAnak;
                break;
            case "Lainnya":
                selectedBorder = FilterLainnya;
                selectedLabel = L_FilterLainnya;
                break;
        }

        if (selectedBorder != null && selectedLabel != null)
        {
            selectedBorder.BackgroundColor = Colors.CornflowerBlue;
            selectedLabel.TextColor = Colors.White;
        }
    }

    private void RefreshLocalFilter()
    {
        if (_allUsers == null || _displayUsers == null) return;

        var keyword = T_Search.Text?.ToLower() ?? string.Empty;

        var filtered = _allUsers.Where(u => 
            ((u.nama_lengkap?.ToLower().Contains(keyword) ?? false) ||
             (u.username?.ToLower().Contains(keyword) ?? false)) &&
            (_currentFilter == "Semua" || 
             (_currentFilter == "Lainnya" ? 
                 !(u.role?.Equals("Ayah", StringComparison.OrdinalIgnoreCase) == true || 
                   u.role?.Equals("Ibu", StringComparison.OrdinalIgnoreCase) == true || 
                   u.role?.Equals("Anak", StringComparison.OrdinalIgnoreCase) == true) :
                 u.role?.Equals(_currentFilter, StringComparison.OrdinalIgnoreCase) == true))
        ).ToList();

        _displayUsers.Clear();
        foreach(var item in filtered)
        {
            _displayUsers.Add(item);
        }

        L_ItemCount.Text = $"{_displayUsers.Count} Items";
        
        EmptyStateView.IsVisible = _displayUsers.Count == 0;
    }

    private void T_Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshLocalFilter();
    }

    private async void BtnMore_Tapped(object sender, TappedEventArgs e)
    {
        var img = sender as Image;
        if (img == null) return;

        await img.ScaleToAsync(0.8, 100);
        await img.ScaleToAsync(1, 100);

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
            string action = await DisplayActionSheetAsync("Opsi", "Batal", null, "Search");
            if (action == "Search")
            {
                StackLayoutTitle.IsVisible = false;
                StackLayoutSearch.IsVisible = true;
                img.Source = "close100.png";
                img.Rotation = 0;
            }
        }
    }

    private void UsersRefresh_Refreshing(object sender, EventArgs e)
    {
        LoadData();
    }

    private async void FAB_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new New_Users());
    }

    private async void ListUsersCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is UserModel selectedItem)
        {
            ListUsersCollection.SelectedItem = null;
            // Navigasi edit atau detail
            await Toast.Make($"Terpilih: {selectedItem.nama_lengkap}").Show();
        }
    }

    private async void ChatWa_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap)
        {
            string whatsappNumber = tap.CommandParameter?.ToString();
            if (!string.IsNullOrEmpty(whatsappNumber))
            {
                // Format if begins with 0
                if (whatsappNumber.StartsWith("0"))
                {
                    whatsappNumber = "62" + whatsappNumber.Substring(1);
                }

                string url = $"https://wa.me/{whatsappNumber}";
                try
                {
                    await Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
                }
                catch (Exception)
                {
                    await Toast.Make("Gagal membuka WhatsApp").Show();
                }
            }
            else
            {
                await Toast.Make("Nomor WhatsApp tidak tersedia").Show();
            }
        }
    }
}

public class UserModel
{
    public int id_users { get; set; }
    public DateTime created_at { get; set; }
    public string username { get; set; }
    public string password { get; set; }
    public string email { get; set; }
    public bool is_active { get; set; }
    public string role { get; set; }
    public string nama_lengkap { get; set; }
    public string photo { get; set; }
    public string whatsapp { get; set; }

    [JsonIgnore]
    public string BaseBucketUrl { get; set; }

    [JsonIgnore]
    public string Initial => !string.IsNullOrEmpty(nama_lengkap) ? nama_lengkap.Substring(0, 1).ToUpper() : "U";

    [JsonIgnore]
    public bool HasPhoto => !string.IsNullOrEmpty(photo);

    [JsonIgnore]
    public string FullPhotoUrl => HasPhoto ? $"{BaseBucketUrl}/photo_user/{photo}" : null;

    [JsonIgnore]
    public Color StatusColor => is_active ? Colors.Green : Colors.Red;

    [JsonIgnore]
    public string StatusText => is_active ? "AKTIF" : "NONAKTIF";

    [JsonIgnore]
    public string DisplayUsername => $"@{username}";
}