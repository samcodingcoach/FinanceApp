using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;

namespace FinanceApp.Rekening;

public partial class List_Akun : ContentPage
{
    private ObservableCollection<AkunRekening> _allAkun;
    private ObservableCollection<AkunRekening> _filteredAkun;

    private int _offset = 0;
    private const int _limit = 50;
    private bool _isLoadingMore = false;
    private bool _hasMoreData = true;

    public List_Akun()
    {
        InitializeComponent();
        _allAkun = new ObservableCollection<AkunRekening>();
        _filteredAkun = new ObservableCollection<AkunRekening>();
        ListAkunCollection.ItemsSource = _filteredAkun;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _offset = 0;
        _hasMoreData = true;
        _allAkun.Clear();
        _filteredAkun.Clear();
        await LoadDataAsync(true);
    }

    private async void AkunRefresh_Refreshing(object sender, EventArgs e)
    {
        _offset = 0;
        _hasMoreData = true;
        _allAkun.Clear();
        _filteredAkun.Clear();
        await LoadDataAsync(false);
    }

    private async void ListAkunCollection_RemainingItemsThresholdReached(object sender, EventArgs e)
    {
        if (_isLoadingMore || !_hasMoreData) return;
        
        _offset += _limit;
        await LoadDataAsync(false);
    }

    private async Task LoadDataAsync(bool showOverlay = true)
    {
        if (_isLoadingMore) return;
        _isLoadingMore = true;

        if (showOverlay)
            OverlayLoading.IsVisible = true;

        var delayTask = showOverlay ? Task.Delay(3000) : Task.CompletedTask;

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;
            string apiUrl = App.API_HOST + $"akun_rekening?limit={_limit}&offset={_offset}";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");

                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<AkunRekening>>(responseContent);

                    if (result != null && result.Count > 0)
                    {
                        var sorted = result.OrderByDescending(x => x.last_update ?? x.created_at).ToList();
                        foreach (var item in sorted)
                        {
                            _allAkun.Add(item);
                        }

                        if (result.Count < _limit)
                        {
                            _hasMoreData = false;
                        }
                    }
                    else
                    {
                        _hasMoreData = false;
                    }

                    RefreshLocalFilter();
                }
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            });
        }
        finally
        {
            if (showOverlay) await delayTask;
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (showOverlay) OverlayLoading.IsVisible = false;
                AkunRefresh.IsRefreshing = false;
                _isLoadingMore = false;
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

        _filteredAkun.Clear();
        foreach (var item in _allAkun)
        {
            if (string.IsNullOrEmpty(keyword) || (!string.IsNullOrEmpty(item.nama_rekening) && item.nama_rekening.ToLower().Contains(keyword)))
            {
                _filteredAkun.Add(item);
            }
        }

        UpdateGrandTotal();
    }

    private void UpdateGrandTotal()
    {
        double total = _filteredAkun.Sum(x => x.saldo_akhir);
        L_GrandTotal.Text = $"Rp {total.ToString("N0", new System.Globalization.CultureInfo("id-ID"))}";
    }

    private async void FAB_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new FinanceApp.Rekening.New_Rekening());
    }

    private async void ListAkunCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is AkunRekening selectedItem)
        {
            ListAkunCollection.SelectedItem = null;
            await Navigation.PushAsync(new FinanceApp.Rekening.Edit_Rekening(selectedItem));
        }
    }
}

public class AkunRekening
{
    public int id_rekening { get; set; }
    public DateTime created_at { get; set; }
    public string? nama_rekening { get; set; }
    public double saldo_awal { get; set; }
    public double saldo_akhir { get; set; }
    public bool is_active { get; set; }
    public DateTime? last_update { get; set; }

    [JsonIgnore]
    public string AvatarInitial => !string.IsNullOrEmpty(nama_rekening) ? nama_rekening.Substring(0, 1).ToUpper() : "A";

    [JsonIgnore]
    public string FormattedDate => last_update.HasValue ? last_update.Value.ToString("dd MMMM yyyy HH:mm") : created_at.ToString("dd MMMM yyyy HH:mm");

    [JsonIgnore]
    public string FormattedSaldo
    {
        get { return "Rp " + saldo_akhir.ToString("N0", new System.Globalization.CultureInfo("id-ID")); }
    }

    public string AvatarImage
    {
        get
        {
            if (string.IsNullOrEmpty(nama_rekening)) return "wallet100.png";
            
            string lowerName = nama_rekening.ToLower();
            if (lowerName.Contains("tunai"))
                return "tunai100.png";
            if (lowerName.Contains("bank"))
                return "bank100.png";
                
            return "wallet100.png";
        }
    }

    public Color AvatarColor
    {
        get
        {
            if (string.IsNullOrEmpty(nama_rekening)) return Color.FromArgb("#8B002E");
            
            string lowerName = nama_rekening.ToLower();
            if (lowerName.Contains("tunai"))
                return Color.FromArgb("#6E9FDA");
            if (lowerName.Contains("bank"))
                return Color.FromArgb("#008B8B");
                
            return Color.FromArgb("#8B002E");
        }
    }
}