using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.Maui.Controls.Shapes;
using CommunityToolkit.Maui.Alerts;
using MauiColor = Microsoft.Maui.Graphics.Color;

#if ANDROID
using Android.Content;
using Android.Graphics.Pdf;
using Android.Provider;
using AndroidColor = Android.Graphics.Color;
using AndroidPaint = Android.Graphics.Paint;
using AndroidRect = Android.Graphics.Rect;
#endif

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

        DateTime now = DateTime.Now;
        DP_LaporanStart.Date = new DateTime(now.Year, now.Month, 1);
        DP_LaporanEnd.Date = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
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
            await DisplayAlertAsync("Error", ex.Message, "OK");
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
            Stroke = MauiColor.FromArgb("#e9efe9"),
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
                TextColor = MauiColor.FromArgb("#171d19"),
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
                Stroke = MauiColor.FromArgb(colorHex),
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

            gridLegend.Children.Add(new Microsoft.Maui.Controls.Shapes.Ellipse
            {
                Fill = new SolidColorBrush(MauiColor.FromArgb(colorHex)),
                WidthRequest = 10,
                HeightRequest = 10,
                VerticalOptions = LayoutOptions.Center
            });

            var lblName = new Label
            {
                Text = cat.nama_kategori,
                FontSize = 14,
                TextColor = MauiColor.FromArgb("#3d4a42"),
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
                TextColor = MauiColor.FromArgb("#171d19"),
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
            TextColor = MauiColor.FromArgb("#171d19"),
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

            MauiColor barColor = isActive ? MauiColor.FromArgb("#006948") : MauiColor.FromArgb("#d0e1fb");
        MauiColor valColor = isActive ? MauiColor.FromArgb("#006948") : MauiColor.FromArgb("#171d19");

        bar.BackgroundColor = barColor;
        lblVal.TextColor = valColor;
        lblBar.TextColor = isActive ? MauiColor.FromArgb("#006948") : MauiColor.FromArgb("#6d7a72");
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

        btn.BackgroundColor = MauiColor.FromArgb("#006948");
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
        btn.TextColor = MauiColor.FromArgb("#3d4a42");
    }

    private async void BtnDownloadLaporan_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            await btn.ScaleTo(0.96, 70);
            await btn.ScaleTo(1.0, 70);
        }

        DateTime dStart = DP_LaporanStart.Date ?? DateTime.Now;
        DateTime dEnd = DP_LaporanEnd.Date ?? DateTime.Now;

        if (dStart > dEnd)
        {
            await DisplayAlertAsync("Peringatan", "Tanggal awal tidak boleh lebih besar dari tanggal akhir!", "OK");
            return;
        }

        LoadingOverlay.IsVisible = true;

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                var payload = new
                {
                    p_tanggal_awal = dStart.ToString("yyyy-MM-dd"),
                    p_tanggal_akhir = dEnd.ToString("yyyy-MM-dd")
                };

                string url = $"{App.API_HOST}/rpc/get_laporan_keuangan";
                var content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlertAsync("Gagal", "Gagal mengambil data laporan dari server.", "OK");
                    return;
                }

                string json = await response.Content.ReadAsStringAsync();
                LaporanKeuanganResponse? laporan = null;

                if (json.TrimStart().StartsWith("["))
                {
                    var list = JsonConvert.DeserializeObject<List<LaporanKeuanganResponse>>(json);
                    if (list != null && list.Count > 0) laporan = list[0];
                }
                else
                {
                    laporan = JsonConvert.DeserializeObject<LaporanKeuanganResponse>(json);
                }

                if (laporan == null)
                {
                    await DisplayAlertAsync("Informasi", "Data laporan tidak ditemukan untuk rentang periode tersebut.", "OK");
                    return;
                }

#if ANDROID
                await GenerateAndSavePdfAsync(laporan, dStart, dEnd);
#else
                await DisplayAlertAsync("Sukses", "Data laporan berhasil digenerate!", "OK");
#endif
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Gagal memproses laporan: {ex.Message}", "OK");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

#if ANDROID
    private async Task GenerateAndSavePdfAsync(LaporanKeuanganResponse laporan, DateTime dStart, DateTime dEnd)
    {
        try
        {
            var pdfDoc = new PdfDocument();
            int pageWidth = 595;
            int pageHeight = 842;
            int marginX = 24;
            int rightX = pageWidth - marginX;
            int pageNumber = 1;

            var pageInfo = new PdfDocument.PageInfo.Builder(pageWidth, pageHeight, pageNumber).Create();
            var page = pdfDoc.StartPage(pageInfo);
            var canvas = page.Canvas;

            // Paint styles
            var paintTitle = new AndroidPaint { Color = AndroidColor.Rgb(20, 20, 20), TextSize = 16, FakeBoldText = true, AntiAlias = true };
            var paintSubtitle = new AndroidPaint { Color = AndroidColor.Rgb(100, 100, 100), TextSize = 9, AntiAlias = true };
            var paintSectionHeader = new AndroidPaint { Color = AndroidColor.Rgb(20, 20, 20), TextSize = 10, FakeBoldText = true, AntiAlias = true };
            var paintBold = new AndroidPaint { Color = AndroidColor.Rgb(20, 20, 20), TextSize = 9, FakeBoldText = true, AntiAlias = true };
            var paintBoldRight = new AndroidPaint { Color = AndroidColor.Rgb(20, 20, 20), TextSize = 9, FakeBoldText = true, AntiAlias = true, TextAlign = AndroidPaint.Align.Right };
            var paintNormal = new AndroidPaint { Color = AndroidColor.Rgb(50, 50, 50), TextSize = 9, AntiAlias = true };
            var paintNormalRight = new AndroidPaint { Color = AndroidColor.Rgb(50, 50, 50), TextSize = 9, AntiAlias = true, TextAlign = AndroidPaint.Align.Right };
            var paintFooter = new AndroidPaint { Color = AndroidColor.Rgb(120, 120, 120), TextSize = 8, AntiAlias = true };
            var paintFooterRight = new AndroidPaint { Color = AndroidColor.Rgb(120, 120, 120), TextSize = 8, AntiAlias = true, TextAlign = AndroidPaint.Align.Right };

            var paintBorder = new AndroidPaint { Color = AndroidColor.Rgb(220, 224, 230), StrokeWidth = 0.8f, AntiAlias = true };
            paintBorder.SetStyle(AndroidPaint.Style.Stroke);

            var paintHeaderRowBg = new AndroidPaint { Color = AndroidColor.Rgb(240, 243, 246), AntiAlias = true };
            paintHeaderRowBg.SetStyle(AndroidPaint.Style.Fill);

            var paintThickDivider = new AndroidPaint { Color = AndroidColor.Rgb(200, 200, 200), StrokeWidth = 1.0f, AntiAlias = true };
            paintThickDivider.SetStyle(AndroidPaint.Style.Stroke);

            var paintThinDivider = new AndroidPaint { Color = AndroidColor.Rgb(235, 238, 242), StrokeWidth = 0.6f, AntiAlias = true };
            paintThinDivider.SetStyle(AndroidPaint.Style.Stroke);

            int y = 45;

            // 1. Header Judul Laporan
            paintTitle.TextAlign = AndroidPaint.Align.Center;
            paintSubtitle.TextAlign = AndroidPaint.Align.Center;
            canvas.DrawText("LAPORAN KEUANGAN", pageWidth / 2, y, paintTitle);
            y += 14;
            canvas.DrawText("Laporan transaksi dan posisi keuangan", pageWidth / 2, y, paintSubtitle);
            paintTitle.TextAlign = AndroidPaint.Align.Left;
            paintSubtitle.TextAlign = AndroidPaint.Align.Left;
            y += 24;

            // 2. Baris Periode Laporan
            canvas.DrawLine(marginX, y, rightX, y, paintThickDivider);
            y += 13;
            canvas.DrawText("PERIODE LAPORAN", marginX, y, paintBold);
            string strPeriode = $"{dStart:dd MMMM yyyy} — {dEnd:dd MMMM yyyy}".ToUpper();
            canvas.DrawText(strPeriode, rightX, y, paintBoldRight);
            y += 6;
            canvas.DrawLine(marginX, y, rightX, y, paintThickDivider);
            y += 22;

            // 3. Ringkasan Keuangan
            canvas.DrawText("RINGKASAN KEUANGAN", marginX, y, paintSectionHeader);
            y += 8;

            decimal saldoAwal = laporan.ringkasan?.saldo_awal ?? 0;
            decimal totalPemasukan = laporan.ringkasan?.total_pemasukan ?? 0;
            decimal totalPengeluaran = laporan.ringkasan?.total_pengeluaran ?? 0;
            decimal saldoAkhir = laporan.ringkasan?.saldo_akhir ?? 0;

            string[,] ringkasanItems = new string[,]
            {
                { "Saldo Awal", $"Rp {saldoAwal:N0}" },
                { "Total Pemasukan", $"Rp {totalPemasukan:N0}" },
                { "Total Pengeluaran", $"Rp {totalPengeluaran:N0}" }
            };

            for (int i = 0; i < 3; i++)
            {
                y += 16;
                canvas.DrawText(ringkasanItems[i, 0], marginX + 8, y, paintNormal);
                canvas.DrawText(ringkasanItems[i, 1], rightX - 8, y, paintNormalRight);
                y += 5;
                canvas.DrawLine(marginX, y, rightX, y, paintThinDivider);
            }

            y += 16;
            canvas.DrawText("Saldo Akhir", marginX + 8, y, paintBold);
            canvas.DrawText($"Rp {saldoAkhir:N0}", rightX - 8, y, paintBoldRight);
            y += 6;
            canvas.DrawLine(marginX, y, rightX, y, paintThickDivider);
            y += 24;

            // 4. Anggaran / Budget (Tabel Format)
            canvas.DrawText("ANGGARAN", marginX, y, paintSectionHeader);
            y += 12;

            int budgetBoxTop = y;
            int budgetRowHeight = 22;
            int budgetTotalRows = 4;
            int budgetBoxBottom = budgetBoxTop + (budgetRowHeight * budgetTotalRows);
            int budgetColSplit = marginX + 160;

            // Gambar Kotak Budget
            canvas.DrawRect(marginX, budgetBoxTop, rightX, budgetBoxBottom, paintBorder);
            canvas.DrawLine(budgetColSplit, budgetBoxTop, budgetColSplit, budgetBoxBottom, paintBorder);

            string budgetPeriode = "-";
            if (laporan.budget != null && !string.IsNullOrEmpty(laporan.budget.periode_awal) && !string.IsNullOrEmpty(laporan.budget.periode_akhir))
            {
                DateTime.TryParse(laporan.budget.periode_awal, out DateTime bStart);
                DateTime.TryParse(laporan.budget.periode_akhir, out DateTime bEnd);
                budgetPeriode = $"{bStart:dd MMMM yyyy} — {bEnd:dd MMMM yyyy}";
            }

            decimal bRencana = laporan.budget?.total_rencana ?? 0;
            decimal bPakai = laporan.budget?.total_pemakaian ?? 0;
            decimal bSisa = laporan.budget?.sisa_budget ?? 0;

            string[,] budgetData = new string[,]
            {
                { "Periode Budget", budgetPeriode, "" },
                { "Total Rencana", "", $"Rp {bRencana:N0}" },
                { "Total Pemakaian", "", $"Rp {bPakai:N0}" },
                { "Sisa Anggaran", "", $"Rp {bSisa:N0}" }
            };

            for (int r = 0; r < budgetTotalRows; r++)
            {
                int rowY = budgetBoxTop + (r * budgetRowHeight);
                if (r > 0) canvas.DrawLine(marginX, rowY, rightX, rowY, paintBorder);

                int textBaseline = rowY + 15;
                bool isLast = (r == budgetTotalRows - 1);
                var pLabel = isLast ? paintBold : paintNormal;
                var pVal = isLast ? paintBoldRight : paintNormalRight;

                canvas.DrawText(budgetData[r, 0], marginX + 8, textBaseline, pLabel);
                if (!string.IsNullOrEmpty(budgetData[r, 1]))
                {
                    canvas.DrawText(budgetData[r, 1], budgetColSplit + 8, textBaseline, paintNormal);
                }
                if (!string.IsNullOrEmpty(budgetData[r, 2]))
                {
                    canvas.DrawText(budgetData[r, 2], rightX - 8, textBaseline, pVal);
                }
            }

            y = budgetBoxBottom + 24;

            // 5. Ringkasan Pengeluaran Berdasarkan Kategori
            canvas.DrawText("RINGKASAN PENGELUARAN BERDASARKAN KATEGORI", marginX, y, paintSectionHeader);
            y += 12;

            int katCol0 = marginX;
            int katCol1 = marginX + 35;
            int katCol2 = marginX + 360;
            int katCol3 = rightX;
            int katRowHeight = 22;

            var listKat = laporan.pengeluaran_per_kategori ?? new List<LaporanKategoriItem>();
            int katCount = Math.Max(listKat.Count, 1);
            int katBoxTop = y;
            int katBoxBottom = katBoxTop + (katRowHeight * (katCount + 1));

            // Header Background
            canvas.DrawRect(katCol0, katBoxTop, katCol3, katBoxTop + katRowHeight, paintHeaderRowBg);
            canvas.DrawRect(katCol0, katBoxTop, katCol3, katBoxBottom, paintBorder);

            // Garis Kolom Header & Body
            canvas.DrawLine(katCol1, katBoxTop, katCol1, katBoxBottom, paintBorder);
            canvas.DrawLine(katCol2, katBoxTop, katCol2, katBoxBottom, paintBorder);

            // Teks Header Kategori
            canvas.DrawText("No.", katCol0 + 8, katBoxTop + 15, paintBold);
            canvas.DrawText("Kategori", katCol1 + 8, katBoxTop + 15, paintBold);
            canvas.DrawText("Jumlah", katCol2 + 8, katBoxTop + 15, paintBold);

            for (int k = 0; k < listKat.Count; k++)
            {
                int rY = katBoxTop + ((k + 1) * katRowHeight);
                canvas.DrawLine(katCol0, rY, katCol3, rY, paintBorder);

                int baseline = rY + 15;
                canvas.DrawText((k + 1).ToString(), katCol0 + 12, baseline, paintNormal);
                canvas.DrawText(listKat[k].nama_kategori ?? "-", katCol1 + 8, baseline, paintNormal);
                canvas.DrawText($"Rp {listKat[k].total:N0}", katCol3 - 8, baseline, paintNormalRight);
            }

            if (listKat.Count == 0)
            {
                int rY = katBoxTop + katRowHeight;
                canvas.DrawLine(katCol0, rY, katCol3, rY, paintBorder);
                canvas.DrawText("-", katCol0 + 12, rY + 15, paintNormal);
                canvas.DrawText("Tidak ada pengeluaran", katCol1 + 8, rY + 15, paintNormal);
                canvas.DrawText("Rp 0", katCol3 - 8, rY + 15, paintNormalRight);
            }

            y = katBoxBottom + 24;

            // 6. Daftar Transaksi
            canvas.DrawText("DAFTAR TRANSAKSI", marginX, y, paintSectionHeader);
            y += 12;

            int trxCol0 = marginX;
            int trxCol1 = marginX + 70;
            int trxCol2 = marginX + 150;
            int trxCol3 = marginX + 260;
            int trxCol4 = marginX + 320;
            int trxCol5 = marginX + 410;
            int trxCol6 = rightX;
            int trxRowHeight = 22;

            var listTrx = laporan.transaksi ?? new List<LaporanTransaksiItem>();

            void DrawTrxHeader(int currentY)
            {
                canvas.DrawRect(trxCol0, currentY, trxCol6, currentY + trxRowHeight, paintHeaderRowBg);
                canvas.DrawRect(trxCol0, currentY, trxCol6, currentY + trxRowHeight, paintBorder);
                canvas.DrawLine(trxCol1, currentY, trxCol1, currentY + trxRowHeight, paintBorder);
                canvas.DrawLine(trxCol2, currentY, trxCol2, currentY + trxRowHeight, paintBorder);
                canvas.DrawLine(trxCol3, currentY, trxCol3, currentY + trxRowHeight, paintBorder);
                canvas.DrawLine(trxCol4, currentY, trxCol4, currentY + trxRowHeight, paintBorder);
                canvas.DrawLine(trxCol5, currentY, trxCol5, currentY + trxRowHeight, paintBorder);

                int hBaseline = currentY + 15;
                canvas.DrawText("Tanggal", trxCol0 + 6, hBaseline, paintBold);
                canvas.DrawText("No. Faktur", trxCol1 + 6, hBaseline, paintBold);
                canvas.DrawText("Kategori", trxCol2 + 6, hBaseline, paintBold);
                canvas.DrawText("Jenis", trxCol3 + 6, hBaseline, paintBold);
                canvas.DrawText("Rekening", trxCol4 + 6, hBaseline, paintBold);
                canvas.DrawText("Nominal", trxCol5 + 6, hBaseline, paintBold);
            }

            DrawTrxHeader(y);
            y += trxRowHeight;

            for (int t = 0; t < listTrx.Count; t++)
            {
                if (y > 780)
                {
                    // Footer halaman saat ini
                    canvas.DrawText("Dokumen laporan keuangan — Finance App", marginX, 815, paintFooter);
                    canvas.DrawText($"Dicetak: {DateTime.Now:dd MMMM yyyy}", rightX, 815, paintFooterRight);

                    pdfDoc.FinishPage(page);
                    pageNumber++;
                    pageInfo = new PdfDocument.PageInfo.Builder(pageWidth, pageHeight, pageNumber).Create();
                    page = pdfDoc.StartPage(pageInfo);
                    canvas = page.Canvas;

                    y = 45;
                    DrawTrxHeader(y);
                    y += trxRowHeight;
                }

                var trx = listTrx[t];
                int rY = y;
                canvas.DrawRect(trxCol0, rY, trxCol6, rY + trxRowHeight, paintBorder);
                canvas.DrawLine(trxCol1, rY, trxCol1, rY + trxRowHeight, paintBorder);
                canvas.DrawLine(trxCol2, rY, trxCol2, rY + trxRowHeight, paintBorder);
                canvas.DrawLine(trxCol3, rY, trxCol3, rY + trxRowHeight, paintBorder);
                canvas.DrawLine(trxCol4, rY, trxCol4, rY + trxRowHeight, paintBorder);
                canvas.DrawLine(trxCol5, rY, trxCol5, rY + trxRowHeight, paintBorder);

                int baseline = rY + 15;
                string tglStr = "-";
                if (DateTime.TryParse(trx.created_at, out DateTime dt)) tglStr = dt.ToString("dd/MM/yyyy");

                string noFaktur = string.IsNullOrEmpty(trx.no_faktur) ? $"TRX#{trx.id_transaksi}" : trx.no_faktur;
                string jenis = trx.tipe ? "Masuk" : "Keluar";

                canvas.DrawText(tglStr, trxCol0 + 6, baseline, paintNormal);
                canvas.DrawText(noFaktur, trxCol1 + 6, baseline, paintNormal);
                canvas.DrawText(trx.nama_kategori ?? "-", trxCol2 + 6, baseline, paintNormal);
                canvas.DrawText(jenis, trxCol3 + 6, baseline, paintNormal);
                canvas.DrawText(trx.nama_rekening ?? "-", trxCol4 + 6, baseline, paintNormal);
                canvas.DrawText($"Rp {trx.total_transaksi:N0}", trxCol6 - 6, baseline, paintNormalRight);

                y += trxRowHeight;
            }

            if (listTrx.Count == 0)
            {
                int rY = y;
                canvas.DrawRect(trxCol0, rY, trxCol6, rY + trxRowHeight, paintBorder);
                canvas.DrawText("Tidak ada transaksi pada periode ini", trxCol0 + 8, rY + 15, paintNormal);
                y += trxRowHeight;
            }

            // Footer Dokumen
            canvas.DrawText("Dokumen laporan keuangan — Finance App", marginX, 815, paintFooter);
            canvas.DrawText($"Dicetak: {DateTime.Now:dd MMMM yyyy}", rightX, 815, paintFooterRight);

            pdfDoc.FinishPage(page);

            // Simpan PDF dengan format nama REPORT_YYYYMMDD-YYYYMMDD.pdf
            string fileName = $"REPORT_{dStart:yyyyMMdd}-{dEnd:yyyyMMdd}.pdf";

            var context = Android.App.Application.Context;
            string downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
            string filePath = System.IO.Path.Combine(downloadsPath, fileName);

            // Auto-overwrite / Timpa jika file sudah ada sebelumnya
            if (System.IO.File.Exists(filePath))
            {
                try { System.IO.File.Delete(filePath); } catch { }
            }

            var values = new ContentValues();
            values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
            values.Put(MediaStore.IMediaColumns.MimeType, "application/pdf");
            values.Put(MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryDownloads);

            var uri = context.ContentResolver.Insert(MediaStore.Downloads.ExternalContentUri, values);
            if (uri != null)
            {
                using (var stream = context.ContentResolver.OpenOutputStream(uri))
                {
                    pdfDoc.WriteTo(stream);
                }
                pdfDoc.Close();

                await Toast.Make($"Laporan {fileName} berhasil diunduh ke folder Downloads").Show();

                // Buka Share Intent untuk melihat / membagikan PDF
                var shareIntent = new Intent(Intent.ActionSend);
                shareIntent.SetType("application/pdf");
                shareIntent.PutExtra(Intent.ExtraStream, uri);
                shareIntent.PutExtra(Intent.ExtraText, $"Laporan Keuangan Periode {dStart:dd/MM/yyyy} - {dEnd:dd/MM/yyyy}");

                var chooserIntent = Intent.CreateChooser(shareIntent, "Buka / Bagikan Laporan Keuangan");
                chooserIntent.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(chooserIntent);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error PDF", $"Gagal membuat file PDF: {ex.Message}", "OK");
        }
    }
#endif
}

public class LaporanKeuanganResponse
{
    public LaporanPeriode? periode { get; set; }
    public LaporanRingkasan? ringkasan { get; set; }
    public LaporanBudget? budget { get; set; }
    public List<LaporanKategoriItem>? pengeluaran_per_kategori { get; set; }
    public List<LaporanTransaksiItem>? transaksi { get; set; }
}

public class LaporanPeriode
{
    public string? tanggal_awal { get; set; }
    public string? tanggal_akhir { get; set; }
}

public class LaporanRingkasan
{
    public decimal saldo_awal { get; set; }
    public decimal total_pemasukan { get; set; }
    public decimal total_pengeluaran { get; set; }
    public decimal saldo_akhir { get; set; }
}

public class LaporanBudget
{
    public int id_budget { get; set; }
    public string? periode_awal { get; set; }
    public string? periode_akhir { get; set; }
    public decimal total_rencana { get; set; }
    public decimal total_pemakaian { get; set; }
    public decimal sisa_budget { get; set; }
}

public class LaporanKategoriItem
{
    public int id_kategori { get; set; }
    public string? nama_kategori { get; set; }
    public string? icon { get; set; }
    public decimal total { get; set; }
}

public class LaporanTransaksiItem
{
    public int id_transaksi { get; set; }
    public string? created_at { get; set; }
    public string? no_faktur { get; set; }
    public int id_users { get; set; }
    public string? nama_lengkap { get; set; }
    public int id_rekening { get; set; }
    public string? nama_rekening { get; set; }
    public int id_kategori { get; set; }
    public string? nama_kategori { get; set; }
    public bool tipe { get; set; }
    public string? icon { get; set; }
    public string? keterangan { get; set; }
    public decimal total_transaksi { get; set; }
}