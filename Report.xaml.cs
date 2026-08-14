using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.Maui.Controls.Shapes;

namespace FinanceApp;

public partial class Report : ContentPage
{
    private string currentMode = "Mingguan";
    private StatistikResponse? currentData;
    private bool isInitializing = true;

    // Define Colors Palette for Category Donut
    private readonly string[] PaletteColors = { "#006948", "#505f76", "#ba1a1a", "#f6a500", "#673ab7", "#009688", "#e91e63" };

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

    public class KategoriData
    {
        public int id_kategori { get; set; }
        public string nama_kategori { get; set; } = "";
        public string bulan { get; set; } = "";
        public decimal total_subtotal { get; set; }
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

                int year = int.Parse(PickerTahun.SelectedItem?.ToString() ?? DateTime.Now.Year.ToString());
                int month = PickerBulan.SelectedIndex >= 0 ? PickerBulan.SelectedIndex + 1 : DateTime.Now.Month;

                var bodyObj = new
                {
                    p_tahun = year,
                    p_bulan = month
                };
                var content = new StringContent(JsonConvert.SerializeObject(bodyObj), System.Text.Encoding.UTF8, "application/json");

                // === 1. Fetch Statistik Transaksi ===
                string urlStat = $"{App.API_HOST}/rpc/get_statistik_transaksi";
                var responseStat = await client.PostAsync(urlStat, content);
                if (responseStat.IsSuccessStatusCode)
                {
                    string resStatJson = await responseStat.Content.ReadAsStringAsync();
                    if (resStatJson.TrimStart().StartsWith("["))
                    {
                        var list = JsonConvert.DeserializeObject<List<StatistikResponse>>(resStatJson);
                        if (list != null && list.Count > 0) currentData = list[0];
                    }
                    else
                    {
                        currentData = JsonConvert.DeserializeObject<StatistikResponse>(resStatJson);
                    }
                    UpdateChartUI();
                }

                // === 2. Fetch Kategori Bulanan ===
                string urlKat = $"{App.API_HOST}/rpc/get_transaksi_kategori_bulanan";
                var contentKat = new StringContent(JsonConvert.SerializeObject(bodyObj), System.Text.Encoding.UTF8, "application/json"); // Renew content
                var responseKat = await client.PostAsync(urlKat, contentKat);
                if (responseKat.IsSuccessStatusCode)
                {
                    string resKatJson = await responseKat.Content.ReadAsStringAsync();
                    var catList = JsonConvert.DeserializeObject<List<KategoriData>>(resKatJson);
                    UpdateCategoryUI(catList ?? new List<KategoriData>());
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

    private void UpdateCategoryUI(List<KategoriData> categories)
    {
        DonutContainer.Children.Clear();
        LegendContainer.Children.Clear();

        // Base background ring
        var baseRing = new Ellipse
        {
            Stroke = Color.FromArgb("#e9efe9"),
            StrokeThickness = 12,
            Fill = Colors.Transparent
        };
        DonutContainer.Children.Add(baseRing);

        decimal totalAmount = 0;
        foreach (var c in categories) totalAmount += c.total_subtotal;

        if (totalAmount <= 0 || categories.Count == 0)
        {
            DonutContainer.Children.Add(new Label
            {
                Text = "0%",
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                TextColor = Color.FromArgb("#171d19"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            });
            return;
        }

        double totalCircumference = 23.04; // Derived from (100-12) * pi / 12
        double currentOffset = 0;
        int colorIndex = 0;

        foreach (var cat in categories)
        {
            if (cat.total_subtotal <= 0) continue;

            double percentage = (double)(cat.total_subtotal / totalAmount);
            double length = percentage * totalCircumference;
            string colorHex = PaletteColors[colorIndex % PaletteColors.Length];

            // 1. Tambahkan irisan Donut
            var slice = new Ellipse
            {
                Stroke = Color.FromArgb(colorHex),
                StrokeThickness = 12,
                Fill = Colors.Transparent
            };

            if (currentOffset == 0)
            {
                slice.StrokeDashArray = new DoubleCollection { length, 1000 };
            }
            else
            {
                slice.StrokeDashArray = new DoubleCollection { 0, currentOffset, length, 1000 };
            }
            DonutContainer.Children.Add(slice);
            currentOffset += length;

            // 2. Tambahkan Item Legenda
            var gridLegend = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            gridLegend.Children.Add(new BoxView
            {
                Color = Color.FromArgb(colorHex),
                WidthRequest = 10,
                HeightRequest = 10,
                CornerRadius = 5,
                VerticalOptions = LayoutOptions.Center
            });

            var lblName = new Label
            {
                Text = cat.nama_kategori,
                FontSize = 14,
                TextColor = Color.FromArgb("#3d4a42"),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(lblName, 1);
            gridLegend.Children.Add(lblName);

            var lblPct = new Label
            {
                Text = $"{(percentage * 100):0.#}%",
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                TextColor = Color.FromArgb("#171d19"),
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(lblPct, 2);
            gridLegend.Children.Add(lblPct);

            LegendContainer.Children.Add(gridLegend);

            colorIndex++;
        }

        // Label persentase total di tengah donat
        DonutContainer.Children.Add(new Label
        {
            Text = "100%",
            FontAttributes = FontAttributes.Bold,
            FontSize = 12,
            TextColor = Color.FromArgb("#171d19"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        });
    }

    private void UpdateChartUI()
    {
        if (currentData == null) return;

        ResetBars();

        List<int> values = new List<int> { 0, 0, 0, 0, 0 };
        List<string> labels = new List<string> { "", "", "", "", "" };
        int activeIndex = -1;

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

        int maxValue = 0;
        foreach (int v in values)
        {
            if (v > maxValue) maxValue = v;
        }

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

        double maxHeight = 130.0;
        double calculatedHeight = 5.0;
        if (maxValue > 0)
        {
            calculatedHeight = ((double)value / maxValue) * maxHeight;
            if (calculatedHeight < 5) calculatedHeight = 5;
        }
        bar.HeightRequest = calculatedHeight;

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

        UpdateChartUI();
    }

    private void ResetTabStyle(Button btn)
    {
        btn.BackgroundColor = Colors.Transparent;
        btn.TextColor = Color.FromArgb("#3d4a42");
    }
}