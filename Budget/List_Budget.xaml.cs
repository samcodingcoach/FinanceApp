using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using CommunityToolkit.Maui.Alerts;

namespace FinanceApp.Budget;

public partial class List_Budget : ContentPage
{
    private ObservableCollection<BudgetModel> _allBudgets;
    private ObservableCollection<BudgetModel> _displayBudgets;

    public List_Budget()
    {
        _allBudgets = new ObservableCollection<BudgetModel>();
        _displayBudgets = new ObservableCollection<BudgetModel>();
        InitializeComponent();
        
        ListBudgetCollection.ItemsSource = _displayBudgets;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadData();
    }

    private async void LoadData()
    {
        BudgetRefresh.IsRefreshing = true;
        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                string url = $"{App.API_HOST}/budget";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<BudgetModel>>(json);
                    
                    _allBudgets.Clear();
                    if (data != null)
                    {
                        foreach (var b in data)
                        {
                            _allBudgets.Add(b);
                        }
                    }
                    RefreshDisplay();
                }
                else
                {
                    await Toast.Make("Gagal memuat budget").Show();
                }
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Error: {ex.Message}").Show();
        }
        finally
        {
            BudgetRefresh.IsRefreshing = false;
        }
    }

    private void RefreshDisplay()
    {
        _displayBudgets.Clear();
        string q = T_Search.Text?.ToLower() ?? "";
        foreach (var b in _allBudgets)
        {
            if (string.IsNullOrEmpty(q) || (!string.IsNullOrEmpty(b.deskripsi) && b.deskripsi.ToLower().Contains(q)))
            {
                _displayBudgets.Add(b);
            }
        }
        L_ItemCount.Text = $"{_displayBudgets.Count} Items";
    }

    private void BudgetRefresh_Refreshing(object sender, EventArgs e)
    {
        LoadData();
    }

    private async void FAB_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new FinanceApp.Budget.New_Budget());
    }

    private void T_Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshDisplay();
    }

    private async void TapFilterDate_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is Image image)
        {
            await image.FadeToAsync(0.3, 100); // Turunkan opacity ke 0.3 dalam 100ms
            await image.FadeToAsync(1, 200);   // Kembalikan opacity ke 1 dalam 200ms

           

                var page = new Budget.Bottom_Sheet_FilterDate();
                page.HasHandle = true;
                page.HasBackdrop = true;
                //page.HandleColor = Color.FromArgb()
                _ = page.ShowAsync(Window);


            }
        }
    }


public class BudgetModel
{
    public int id_budget { get; set; }
    public string deskripsi { get; set; }
    public DateTime periode_awal { get; set; }
    public DateTime periode_akhir { get; set; }
    public bool is_active { get; set; }
    public decimal total_rencana { get; set; }
    public decimal total_pemakaian { get; set; }
    
    [JsonIgnore]
    public string PeriodeDisplay => $"{periode_awal:dd MMM yyyy} - {periode_akhir:dd MMM yyyy}";
    
    [JsonIgnore]
    public string StatusText => is_active ? "AKTIF" : "TDK AKTIF";
    
    [JsonIgnore]
    public Color StatusColor => is_active ? Colors.Green : Colors.Grey;
    
    [JsonIgnore]
    public string TotalRencanaDisplay => $"Rp {total_rencana:N0}";
    
    [JsonIgnore]
    public string TotalPemakaianDisplay => $"Rp {total_pemakaian:N0}";
    
    [JsonIgnore]
    public double ProgressValue
    {
        get
        {
            if (total_rencana <= 0) return 0;
            double p = (double)(total_pemakaian / total_rencana);
            return p > 1 ? 1 : p;
        }
    }
    
    [JsonIgnore]
    public string PersentaseDisplay
    {
        get
        {
            if (total_rencana <= 0) return "0%";
            return $"{((double)total_pemakaian / (double)total_rencana):P0}";
        }
    }
    
    [JsonIgnore]
    public Color ProgressColor
    {
        get
        {
            if (ProgressValue < 0.5) return Colors.Green;
            if (ProgressValue < 0.8) return Colors.Orange;
            return Colors.IndianRed;
        }
    }
}