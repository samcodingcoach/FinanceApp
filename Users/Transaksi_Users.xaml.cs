using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using CommunityToolkit.Maui.Alerts;
#if ANDROID
using Android.Content;
using Android.Provider;
using Android.Graphics.Pdf;
#endif

namespace FinanceApp.Users;

public partial class Transaksi_Users : ContentPage
{
    private List<TransaksiHarianGroup> _pdfData = new();

    public Transaksi_Users()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        BindableLayout.SetItemsSource(GroupedDataCollection, null);

        try 
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;
            int id_users = Preferences.Get("id_user", 0);
            if (id_users <= 0)
            {
                string jsonUser = Preferences.Get("user_data", string.Empty);
                if (!string.IsNullOrEmpty(jsonUser))
                {
                    try
                    {
                        var jObj = Newtonsoft.Json.Linq.JObject.Parse(jsonUser);
                        id_users = (int?)jObj["id_users"] ?? (int?)jObj["user_id"] ?? (int?)jObj["id_user"] ?? (int?)jObj["id"] ?? 0;
                    }
                    catch { }
                }
            }
            if (id_users <= 0) id_users = 1;

            string url = $"{App.API_HOST}/rpc/get_transaksi_detail_harian";

            var payload = new { p_id_users = id_users };
            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");
                
                var response = await client.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    string resJson = await response.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<TransaksiDetailHarianModel>>(resJson);

                    if (list != null && list.Count > 0)
                    {
                        var currentMonth = DateTime.Now.Month;
                        var currentYear = DateTime.Now.Year;

                        var groups = list
                            .Where(x => {
                                if (DateTime.TryParse(x.tanggal, out DateTime dt))
                                    return dt.Month == currentMonth && dt.Year == currentYear;
                                return false;
                            })
                            .GroupBy(x => x.tanggal)
                            .OrderByDescending(g => g.Key)
                            .Select(g => new TransaksiHarianGroup
                            {
                                TanggalFormat = FormatTanggalBulan(g.Key),
                                TotalFormat = $"Rp {g.Sum(x => x.subtotal):N0}",
                                Items = new ObservableCollection<TransaksiDetailHarianModel>(g)
                            }).ToList();

                        _pdfData = groups;
                        BindableLayout.SetItemsSource(GroupedDataCollection, groups);
                    }
                }
                else
                {
                    await DisplayAlert("Error", "Gagal memuat data", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Koneksi bermasalah: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            RefreshData.IsRefreshing = false;
        }
    }
    
    private string FormatTanggalBulan(string tanggal)
    {
        if (DateTime.TryParse(tanggal, out DateTime dt))
        {
            return dt.ToString("MMMM dd, yyyy", new System.Globalization.CultureInfo("en-US"));
        }
        return tanggal;
    }

    private async void Back_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 100);
            await view.ScaleTo(1, 100);
        }
        await Navigation.PopAsync();
    }

    private async void RefreshData_Refreshing(object sender, EventArgs e)
    {
        await LoadDataAsync();
    }

    private async void FAB_ExportPdf_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 50);
            await view.ScaleTo(1, 50);
        }

        if (_pdfData == null || _pdfData.Count == 0)
        {
            await DisplayAlert("Info", "Tidak ada data untuk diexport.", "OK");
            return;
        }

#if ANDROID
        await GenerateAndSharePdfAndroid();
#else
        await DisplayAlert("Info", "Export PDF saat ini hanya didukung di platform Android.", "OK");
#endif
    }

#if ANDROID
    private async Task GenerateAndSharePdfAndroid()
    {
        try
        {
            var pdfDoc = new PdfDocument();
            var pageInfo = new PdfDocument.PageInfo.Builder(595, 842, 1).Create();
            var page = pdfDoc.StartPage(pageInfo);
            var canvas = page.Canvas;

            var paintTitle = new Android.Graphics.Paint { TextSize = 16, Color = Android.Graphics.Color.Black, FakeBoldText = true };
            paintTitle.TextAlign = Android.Graphics.Paint.Align.Center;
            
            var paintNormal = new Android.Graphics.Paint { TextSize = 10, Color = Android.Graphics.Color.Black };
            var paintNormalCenter = new Android.Graphics.Paint { TextSize = 10, Color = Android.Graphics.Color.Black };
            paintNormalCenter.TextAlign = Android.Graphics.Paint.Align.Center;

            var paintBold = new Android.Graphics.Paint { TextSize = 10, Color = Android.Graphics.Color.Black, FakeBoldText = true };
            var paintBoldCenter = new Android.Graphics.Paint { TextSize = 10, Color = Android.Graphics.Color.Black, FakeBoldText = true };
            paintBoldCenter.TextAlign = Android.Graphics.Paint.Align.Center;

            var paintLine = new Android.Graphics.Paint { Color = Android.Graphics.Color.Black, StrokeWidth = 1 };
            paintLine.SetStyle(Android.Graphics.Paint.Style.Stroke);

            int y = 50;
            canvas.DrawText("REKAPITULASI PENGELUARAN ANGGOTA", 595 / 2, y, paintTitle);
            y += 20;
            canvas.DrawText($"Periode: {DateTime.Now.ToString("MMMM yyyy", new System.Globalization.CultureInfo("id-ID"))}", 595 / 2, y, paintNormalCenter);
            y += 30;

            int x0 = 30;  // No
            int x1 = 60;  // Barang/Jasa
            int x2 = 300; // Jumlah
            int x3 = 380; // Harga
            int x4 = 470; // Subtotal
            int x5 = 565; // End

            int rowHeight = 25;
            decimal grandTotal = 0;
            int pageNum = 1;

            void NextPage()
            {
                pdfDoc.FinishPage(page);
                pageNum++;
                pageInfo = new PdfDocument.PageInfo.Builder(595, 842, pageNum).Create();
                page = pdfDoc.StartPage(pageInfo);
                canvas = page.Canvas;
                y = 50;
            }

            foreach (var group in _pdfData)
            {
                if (y > 750) NextPage();

                // Draw Date Header
                canvas.DrawText(group.TanggalFormat, x0, y, paintBold);
                canvas.DrawText(group.TotalFormat, x5 - 70, y, paintBold);
                
                y += 10;
                
                // Draw Table Header
                canvas.DrawLine(x0, y, x5, y, paintLine);
                int textY = y + 17;
                canvas.DrawText("No", x0 + (x1 - x0) / 2, textY, paintBoldCenter);
                canvas.DrawText("Nama Barang/Jasa", x1 + 10, textY, paintBold);
                canvas.DrawText("Jumlah", x2 + (x3 - x2) / 2, textY, paintBoldCenter);
                canvas.DrawText("Harga", x3 + (x4 - x3) / 2, textY, paintBoldCenter);
                canvas.DrawText("Subtotal", x4 + (x5 - x4) / 2, textY, paintBoldCenter);
                
                y += rowHeight;
                canvas.DrawLine(x0, y, x5, y, paintLine);
                
                int index = 1;

                foreach (var item in group.Items)
                {
                    if (y > 780)
                    {
                        NextPage();
                        canvas.DrawLine(x0, y, x5, y, paintLine);
                    }

                    textY = y + 17;
                    canvas.DrawText(index.ToString(), x0 + (x1 - x0) / 2, textY, paintNormalCenter);
                    
                    string itemName = item.nama_barang_jasa ?? "-";
                    if (itemName.Length > 40) itemName = itemName.Substring(0, 37) + "...";
                    canvas.DrawText(itemName, x1 + 10, textY, paintNormal);
                    
                    canvas.DrawText(item.jumlah.ToString("N0"), x2 + (x3 - x2) / 2, textY, paintNormalCenter);
                    canvas.DrawText($"Rp {item.harga:N0}", x3 + (x4 - x3) / 2, textY, paintNormalCenter);
                    canvas.DrawText($"Rp {item.subtotal:N0}", x4 + (x5 - x4) / 2, textY, paintNormalCenter);

                    y += rowHeight;
                    canvas.DrawLine(x0, y, x5, y, paintLine);
                    
                    // Vertical lines
                    canvas.DrawLine(x0, y - rowHeight, x0, y, paintLine);
                    canvas.DrawLine(x1, y - rowHeight, x1, y, paintLine);
                    canvas.DrawLine(x2, y - rowHeight, x2, y, paintLine);
                    canvas.DrawLine(x3, y - rowHeight, x3, y, paintLine);
                    canvas.DrawLine(x4, y - rowHeight, x4, y, paintLine);
                    canvas.DrawLine(x5, y - rowHeight, x5, y, paintLine);
                    
                    grandTotal += item.subtotal;
                    index++;
                }

                y += 25; // Space between dates
            }

            // Grand Total Area
            if (y > 750) NextPage();
            
            y += 10;
            canvas.DrawLine(x0, y, x5, y, paintLine);
            int gtTextY = y + 17;
            canvas.DrawText("TOTAL PENGELUARAN BULAN INI", x1 + 10, gtTextY, paintBold);
            canvas.DrawText($"Rp {grandTotal:N0}", x4 + (x5 - x4) / 2, gtTextY, paintBoldCenter);
            y += rowHeight;
            canvas.DrawLine(x0, y, x5, y, paintLine);

            pdfDoc.FinishPage(page);

            // Output
            string fileName = $"Rekap_Anggota_{DateTime.Now.ToString("yyyyMM")}.pdf";
            var context = Android.App.Application.Context;
            string downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
            string filePath = System.IO.Path.Combine(downloadsPath, fileName);

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

                await Toast.Make("PDF berhasil dibuat di folder Downloads").Show();

                var shareIntent = new Intent(Intent.ActionSend);
                shareIntent.SetType("application/pdf");
                shareIntent.PutExtra(Intent.ExtraStream, uri);
                shareIntent.PutExtra(Intent.ExtraText, "Berikut Laporan Rekapitulasi Pengeluaran Anggota bulan ini.");
                
                var chooserIntent = Intent.CreateChooser(shareIntent, "Bagikan Laporan");
                chooserIntent.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(chooserIntent);
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Gagal export PDF: {ex.Message}").Show();
        }
    }
#endif
}

public class TransaksiDetailHarianModel
{
    public string tanggal { get; set; }
    public int id_users { get; set; }
    public string nama_barang_jasa { get; set; }
    public int jumlah { get; set; }
    public decimal harga { get; set; }
    public decimal subtotal { get; set; }

    [JsonIgnore]
    public string HargaFormat => $"{jumlah} x Rp {harga:N0}";
    [JsonIgnore]
    public string SubtotalFormat => $"Rp {subtotal:N0}";
}

public class TransaksiHarianGroup
{
    public string TanggalFormat { get; set; }
    public string TotalFormat { get; set; }
    public ObservableCollection<TransaksiDetailHarianModel> Items { get; set; }
}