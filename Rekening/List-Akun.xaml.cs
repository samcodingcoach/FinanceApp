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
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var delayTask = Task.Delay(3000);

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;
            string apiUrl = App.API_HOST + "akun_rekening";

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

                    _allAkun.Clear();
                    _filteredAkun.Clear();

                    if (result != null)
                    {
                        var sorted = result.OrderByDescending(x => x.last_update ?? x.created_at).ToList();
                        foreach (var item in sorted)
                        {
                            _allAkun.Add(item);
                            _filteredAkun.Add(item);
                        }
                    }
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
            await delayTask;
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OverlayLoading.IsVisible = false;
            });
        }
    }

    private void T_Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = e.NewTextValue?.ToLower() ?? string.Empty;

        _filteredAkun.Clear();
        foreach (var item in _allAkun)
        {
            if (string.IsNullOrEmpty(keyword) || (!string.IsNullOrEmpty(item.nama_rekening) && item.nama_rekening.ToLower().Contains(keyword)))
            {
                _filteredAkun.Add(item);
            }
        }
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
    public string FormattedSaldo => $"Rp {saldo_akhir.ToString("N0", new System.Globalization.CultureInfo("id-ID"))}";
}