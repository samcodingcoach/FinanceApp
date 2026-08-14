using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace FinanceApp;

public partial class Report : ContentPage
{
    private string currentMode = "Mingguan";
    private StatistikResponse? currentData;
    private bool isInitializing = true;

    public class MingguanData
    {
        public int minggu { get; set; }
        public int jumlah_transaksi { get; set; }
    }

    public class BulananData
    {
        public int bulan { get; set; }
        public string nama_bulan { get; set; } = "";
        public int jumlah_transaksi { get; set; }
    }

    public class TahunanData
    {
        public int tahun { get; set; }
        public int jumlah_transaksi { get; set; }
    }

    public class StatistikResponse
    {
        public List<MingguanData>? mingguan { get; set; }
        public List<BulananData>? bulanan { get; set; }
        public List<TahunanData>? tahunan { get; set; }
    }

    public Report()
    {
        InitializeComponent();
        InitializePickers();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (isInitializing)
        {
            isInitializing = false;
            await LoadData();
        }
    }

    private void InitializePickers()
    {
        // Setup Tahun
        int currentYear = DateTime.Now.Year;
        for (int i = currentYear - 5; i <= currentYear + 1; i++)
        {
            PickerTahun.Items.Add(i.ToString());
        }
        PickerTahun.SelectedItem = currentYear.ToString();

        // Setup Bulan
        string[] namaBulan = { "Januari", "Februari", "Maret", "April", "Mei", "Juni", "Juli", "Agustus", "September", "Oktober", "November", "Desember" };
        foreach (var b in namaBulan)
        {
            PickerBulan.Items.Add(b);
        }
        PickerBulan.SelectedIndex = DateTime.Now.Month - 1;

        PickerTahun.SelectedIndexChanged += async (s, e) => { if (!isInitializing) await LoadData(); };
        PickerBulan.SelectedIndexChanged += async (s, e) => { if (!isInitializing) await LoadData(); };
    }

    private async Task LoadData()
    {
        LoadingOverlay.IsVisible = true;

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                string url = $"{App.API_HOST}/rpc/get_statistik_transaksi";

                int year = int.Parse(PickerTahun.SelectedItem?.ToString() ?? DateTime.Now.Year.ToString());
                int month = PickerBulan.SelectedIndex >= 0 ? PickerBulan.SelectedIndex + 1 : DateTime.Now.Month;

                var bodyObj = new
                {
                    p_tahun = year,
                    p_bulan = month
                };

                string jsonBody = JsonConvert.SerializeObject(bodyObj);
                var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseJson = await response.Content.ReadAsStringAsync();
                    
                    // RPC sering mengembalikan array of object jika outputnya JSON, coba parse sebagai array dulu
                    if (responseJson.TrimStart().StartsWith("["))
                    {
                        var list = JsonConvert.DeserializeObject<List<StatistikResponse>>(responseJson);
                        if (list != null && list.Count > 0)
                            currentData = list[0];
                    }
                    else
                    {
                        currentData = JsonConvert.DeserializeObject<StatistikResponse>(responseJson);
                    }

                    UpdateChartUI();
                }
                else
                {
                    string err = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Gagal Memuat Laporan", $"Status: {response.StatusCode}\n{err}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private void UpdateChartUI()
    {
        if (currentData == null) return;

        // Reset semua bar ke 0
        ResetBars();

        List<int> values = new List<int> { 0, 0, 0, 0, 0 };
        List<string> labels = new List<string> { "", "", "", "", "" };
        int activeIndex = -1; // Index batang yang di-highlight (opsional)

        if (currentMode == "Mingguan" && currentData.mingguan != null)
        {
            for (int i = 0; i < Math.Min(5, currentData.mingguan.Count); i++)
            {
                values[i] = currentData.mingguan[i].jumlah_transaksi;
                labels[i] = $"W{currentData.mingguan[i].minggu}";
            }
        }
        else if (currentMode == "Bulanan" && currentData.bulanan != null)
        {
            for (int i = 0; i < Math.Min(5, currentData.bulanan.Count); i++)
            {
                values[i] = currentData.bulanan[i].jumlah_transaksi;
                
                // Ambil singkatan 3 huruf bulan (e.g. "August" -> "Aug")
                string monthName = currentData.bulanan[i].nama_bulan;
                labels[i] = monthName.Length > 3 ? monthName.Substring(0, 3) : monthName;
            }
        }
        else if (currentMode == "Tahunan" && currentData.tahunan != null)
        {
            for (int i = 0; i < Math.Min(5, currentData.tahunan.Count); i++)
            {
                values[i] = currentData.tahunan[i].jumlah_transaksi;
                labels[i] = currentData.tahunan[i].tahun.ToString();
            }
        }

        // Cari nilai maksimum untuk penskalaan tinggi batang grafik (Max Height = 130px)
        int maxValue = 0;
        foreach (int v in values)
        {
            if (v > maxValue) maxValue = v;
        }

        // Terapkan ke UI
        ApplyBar(0, LblVal1, Bar1, LblBar1, values[0], labels[0], maxValue, 0 == activeIndex);
        ApplyBar(1, LblVal2, Bar2, LblBar2, values[1], labels[1], maxValue, 1 == activeIndex);
        ApplyBar(2, LblVal3, Bar3, LblBar3, values[2], labels[2], maxValue, 2 == activeIndex);
        ApplyBar(3, LblVal4, Bar4, LblBar4, values[3], labels[3], maxValue, 3 == activeIndex);
        ApplyBar(4, LblVal5, Bar5, LblBar5, values[4], labels[4], maxValue, 4 == activeIndex);
    }

    private void ApplyBar(int index, Label lblVal, BoxView bar, Label lblBar, int value, string label, int maxValue, bool isActive)
    {
        lblVal.Text = value.ToString();
        lblBar.Text = string.IsNullOrEmpty(label) ? "-" : label;

        // Penskalaan tinggi bar (Minimal 5px supaya tetap terlihat meskipun 0)
        double maxHeight = 130.0;
        double calculatedHeight = 5.0;
        if (maxValue > 0)
        {
            calculatedHeight = ((double)value / maxValue) * maxHeight;
            if (calculatedHeight < 5) calculatedHeight = 5;
        }
        bar.HeightRequest = calculatedHeight;

        // Pewarnaan (Highlight bar dengan nilai tertinggi jika mau, atau set dinamis)
        Color barColor = isActive ? Color.FromArgb("#006948") : Color.FromArgb("#d0e1fb");
        Color valColor = isActive ? Color.FromArgb("#006948") : Color.FromArgb("#171d19");

        bar.BackgroundColor = barColor;
        lblVal.TextColor = valColor;
        lblBar.TextColor = isActive ? Color.FromArgb("#006948") : Color.FromArgb("#6d7a72");
        lblBar.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None;
    }

    private void ResetBars()
    {
        LblVal1.Text = "0"; LblVal2.Text = "0"; LblVal3.Text = "0"; LblVal4.Text = "0"; LblVal5.Text = "0";
        Bar1.HeightRequest = 5; Bar2.HeightRequest = 5; Bar3.HeightRequest = 5; Bar4.HeightRequest = 5; Bar5.HeightRequest = 5;
        LblBar1.Text = "-"; LblBar2.Text = "-"; LblBar3.Text = "-"; LblBar4.Text = "-"; LblBar5.Text = "-";
    }

    private void Tab_Clicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        if (btn == null) return;

        ResetTabStyle(BtnMingguan);
        ResetTabStyle(BtnBulanan);
        ResetTabStyle(BtnTahunan);

        btn.BackgroundColor = Color.FromArgb("#006948");
        btn.TextColor = Colors.White;

        if (btn == BtnMingguan)
        {
            currentMode = "Mingguan";
            PickerTahun.IsVisible = true;
            PickerBulan.IsVisible = true;
            LabelTahunIni.IsVisible = false;
        }
        else if (btn == BtnBulanan)
        {
            currentMode = "Bulanan";
            PickerTahun.IsVisible = true;
            PickerBulan.IsVisible = false;
            LabelTahunIni.IsVisible = false;
        }
        else if (btn == BtnTahunan)
        {
            currentMode = "Tahunan";
            PickerTahun.IsVisible = false;
            PickerBulan.IsVisible = false;
            LabelTahunIni.IsVisible = true;
        }

        // Langsung refresh chart tanpa tembak API lagi
        UpdateChartUI();
    }

    private void ResetTabStyle(Button btn)
    {
        btn.BackgroundColor = Colors.Transparent;
        btn.TextColor = Color.FromArgb("#3d4a42");
    }
}