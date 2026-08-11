using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using The49.Maui.BottomSheet;

#if ANDROID
using Android.Content;
using Android.Graphics.Pdf;
using Android.Provider;
#endif

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
                        foreach (var item in data)
                        {
                            _allData.Add(item);
                            _filteredData.Add(item);
                        }
                        UpdateTotal();
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

    private void UpdateTotal()
    {
        decimal sumTotal = 0;
        foreach (var item in _filteredData)
        {
            sumTotal += item.subtotal;
        }
        MainThread.BeginInvokeOnMainThread(() =>
        {
            string formattedTotal = sumTotal == 0 ? "0" : sumTotal.ToString("N0");
            L_TotalSubtotal.Text = "Rp " + formattedTotal;
        });
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
        UpdateTotal();
    }

    private async void Close_Tapped(object sender, TappedEventArgs e)
    {
        await this.DismissAsync();
    }

    private async void B_Export_Clicked(object sender, EventArgs e)
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

                string url = $"{App.API_HOST}/rpc/get_transaksi?id_transaksi=eq.{_id_transaksi}";
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var headerList = JsonConvert.DeserializeObject<List<TransaksiRowModel>>(json);
                    var header = headerList?.FirstOrDefault();

                    if (header != null)
                    {
#if ANDROID
                        await GenerateAndSharePdfAndroid(header);
#else
                        await Toast.Make("Export PDF saat ini hanya didukung di Android.").Show();
#endif
                    }
                    else
                    {
                        await Toast.Make("Data transaksi tidak ditemukan.").Show();
                    }
                }
                else
                {
                    await Toast.Make("Gagal memuat data transaksi.").Show();
                }
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Gagal export: {ex.Message}").Show();
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

#if ANDROID
    private async Task GenerateAndSharePdfAndroid(TransaksiRowModel header)
    {
        try
        {
            var pdfDoc = new PdfDocument();
            var pageInfo = new PdfDocument.PageInfo.Builder(595, 842, 1).Create();
            var page = pdfDoc.StartPage(pageInfo);
            var canvas = page.Canvas;

            var paintTitle = new Android.Graphics.Paint { TextSize = 16, Color = Android.Graphics.Color.Black, FakeBoldText = true };
            paintTitle.TextAlign = Android.Graphics.Paint.Align.Center;
            
            var paintNormal = new Android.Graphics.Paint { TextSize = 12, Color = Android.Graphics.Color.Black };
            var paintNormalCenter = new Android.Graphics.Paint { TextSize = 12, Color = Android.Graphics.Color.Black };
            paintNormalCenter.TextAlign = Android.Graphics.Paint.Align.Center;

            var paintBold = new Android.Graphics.Paint { TextSize = 12, Color = Android.Graphics.Color.Black, FakeBoldText = true };
            var paintBoldCenter = new Android.Graphics.Paint { TextSize = 12, Color = Android.Graphics.Color.Black, FakeBoldText = true };
            paintBoldCenter.TextAlign = Android.Graphics.Paint.Align.Center;
            
            var paintBoldItalic = new Android.Graphics.Paint { TextSize = 12, Color = Android.Graphics.Color.Black, FakeBoldText = true, TextSkewX = -0.25f };

            var paintLine = new Android.Graphics.Paint { Color = Android.Graphics.Color.Black, StrokeWidth = 1 };
            paintLine.SetStyle(Android.Graphics.Paint.Style.Stroke);

            int marginX = 20;
            int y = 50;

            // Title
            canvas.DrawText("LAPORAN TRANSAKSI", 595 / 2, y, paintTitle);
            y += 40;

            // Header 2 Columns
            int col1_X = marginX;
            int col1_colon = marginX + 80;
            
            int col2_X = 330;
            int col2_colon = 330 + 110;

            // Row 1
            canvas.DrawText("No Faktur", col1_X, y, paintNormal);
            canvas.DrawText($": {header.no_faktur ?? "-"}", col1_colon, y, paintNormal);
            canvas.DrawText("Kategori", col2_X, y, paintNormal);
            canvas.DrawText($": {header.nama_kategori ?? "-"}", col2_colon, y, paintNormal);
            y += 20;

            // Row 2
            canvas.DrawText("User", col1_X, y, paintNormal);
            canvas.DrawText($": {header.nama_lengkap ?? "-"}", col1_colon, y, paintNormal);
            canvas.DrawText("Tanggal Transaksi", col2_X, y, paintNormal);
            canvas.DrawText($": {header.created_at:dd MMM yyyy HH:mm}", col2_colon, y, paintNormal);
            y += 20;

            // Row 3
            canvas.DrawText("Sumber Dana", col1_X, y, paintNormal);
            canvas.DrawText($": {header.nama_rekening ?? "-"}", col1_colon, y, paintNormal);
            canvas.DrawText("Keterangan", col2_X, y, paintNormal);
            canvas.DrawText($": {header.keterangan ?? "-"}", col2_colon, y, paintNormal);
            y += 40;

            // Subtitle
            canvas.DrawText("Detail Item Barang / Jasa", marginX, y, paintBold);
            y += 15;

            // Table Columns
            int x0 = 20;
            int x1 = 60;
            int x2 = 275;
            int x3 = 355;
            int x4 = 465;
            int x5 = 575;

            // Header Row
            int rowHeight = 30;
            canvas.DrawLine(x0, y, x5, y, paintLine); // Top line
            
            int textY = y + 20;
            canvas.DrawText("No", x0 + (x1 - x0) / 2, textY, paintBoldCenter);
            canvas.DrawText("Nama Barang/Jasa", x1 + (x2 - x1) / 2, textY, paintBoldCenter);
            canvas.DrawText("Jumlah", x2 + (x3 - x2) / 2, textY, paintBoldCenter);
            canvas.DrawText("Harga", x3 + (x4 - x3) / 2, textY, paintBoldCenter);
            canvas.DrawText("Subtotal", x4 + (x5 - x4) / 2, textY, paintBoldCenter);

            y += rowHeight;
            canvas.DrawLine(x0, y, x5, y, paintLine); // Bottom header line

            decimal grandTotal = 0;
            int totalQty = 0;
            int index = 1;

            // Table Vertical Lines (Header)
            canvas.DrawLine(x0, y - rowHeight, x0, y, paintLine);
            canvas.DrawLine(x1, y - rowHeight, x1, y, paintLine);
            canvas.DrawLine(x2, y - rowHeight, x2, y, paintLine);
            canvas.DrawLine(x3, y - rowHeight, x3, y, paintLine);
            canvas.DrawLine(x4, y - rowHeight, x4, y, paintLine);
            canvas.DrawLine(x5, y - rowHeight, x5, y, paintLine);

            foreach (var item in _allData)
            {
                if (y > 650)
                {
                    pdfDoc.FinishPage(page);
                    pageInfo = new PdfDocument.PageInfo.Builder(595, 842, 2).Create();
                    page = pdfDoc.StartPage(pageInfo);
                    canvas = page.Canvas;
                    y = 50;
                    canvas.DrawLine(x0, y, x5, y, paintLine);
                }

                textY = y + 20;
                canvas.DrawText(index.ToString(), x0 + (x1 - x0) / 2, textY, paintNormalCenter);
                canvas.DrawText(item.nama_barang_jasa ?? "-", x1 + 10, textY, paintNormal);
                canvas.DrawText(item.jumlah.ToString("N0"), x2 + (x3 - x2) / 2, textY, paintNormalCenter);
                canvas.DrawText($"Rp {item.harga:N0}", x3 + (x4 - x3) / 2, textY, paintNormalCenter);
                canvas.DrawText($"Rp {item.subtotal:N0}", x4 + (x5 - x4) / 2, textY, paintNormalCenter);

                totalQty += item.jumlah;
                grandTotal += item.subtotal;
                index++;

                y += rowHeight;
                canvas.DrawLine(x0, y, x5, y, paintLine);

                // Vertical lines for row
                canvas.DrawLine(x0, y - rowHeight, x0, y, paintLine);
                canvas.DrawLine(x1, y - rowHeight, x1, y, paintLine);
                canvas.DrawLine(x2, y - rowHeight, x2, y, paintLine);
                canvas.DrawLine(x3, y - rowHeight, x3, y, paintLine);
                canvas.DrawLine(x4, y - rowHeight, x4, y, paintLine);
                canvas.DrawLine(x5, y - rowHeight, x5, y, paintLine);
            }

            // Grand Total Row
            textY = y + 20;
            canvas.DrawText("Grand Total", x0 + (x2 - x0) / 2, textY, paintBoldCenter);
            canvas.DrawText(totalQty.ToString("N0"), x2 + (x3 - x2) / 2, textY, paintBoldCenter);
            canvas.DrawText($"Rp {grandTotal:N0}", x3 + (x5 - x3) / 2, textY, paintBoldCenter);

            y += rowHeight;
            canvas.DrawLine(x0, y, x5, y, paintLine);

            // Vertical lines for Grand Total
            canvas.DrawLine(x0, y - rowHeight, x0, y, paintLine);
            canvas.DrawLine(x2, y - rowHeight, x2, y, paintLine);
            canvas.DrawLine(x3, y - rowHeight, x3, y, paintLine);
            canvas.DrawLine(x5, y - rowHeight, x5, y, paintLine);

            // Bottom Area
            y += 40;
            int boxSize = 100;
            canvas.DrawRect(x0, y, x0 + boxSize, y + boxSize, paintLine);

            if (!string.IsNullOrEmpty(header.foto_transaksi))
            {
                try {
                    string imageUrl = ((App)Application.Current).BUCKET_URL + "/transaksi/" + header.foto_transaksi;
                    using var hc = new HttpClient();
                    var imgBytes = await hc.GetByteArrayAsync(imageUrl);
                    var bitmap = Android.Graphics.BitmapFactory.DecodeByteArray(imgBytes, 0, imgBytes.Length);
                    if (bitmap != null) {
                        canvas.DrawBitmap(bitmap, null, new Android.Graphics.Rect(x0, y, x0 + boxSize, y + boxSize), null);
                    } else {
                        canvas.DrawText("Foto Transaksi", x0 + boxSize / 2, y + boxSize / 2 + 5, paintNormalCenter);
                    }
                } catch {
                    canvas.DrawText("Foto Transaksi", x0 + boxSize / 2, y + boxSize / 2 + 5, paintNormalCenter);
                }
            }
            else
            {
                canvas.DrawText("Foto Transaksi", x0 + boxSize / 2, y + boxSize / 2 + 5, paintNormalCenter);
            }

            canvas.DrawText("Export Date", x0 + boxSize + 20, y + 15, paintBoldItalic);
            canvas.DrawText(DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"), x0 + boxSize + 90, y + 15, paintNormal);

            pdfDoc.FinishPage(page);

            string fileName = $"{header.no_faktur ?? "ID" + header.id_transaksi}.pdf";
            
            // Hapus karakter yang tidak diizinkan di nama file
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            
            var context = Android.App.Application.Context;
            
            string downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
            string filePath = System.IO.Path.Combine(downloadsPath, fileName);

            if (System.IO.File.Exists(filePath))
            {
                bool replace = await Application.Current.MainPage.DisplayAlert("File Sudah Ada", 
                    $"Laporan {fileName} sudah ada di folder Downloads.\n\nApakah Anda ingin menimpanya (replace)?", 
                    "Ya, Timpa", "Batal");
                
                if (!replace)
                {
                    pdfDoc.Close();
                    return;
                }
                
                try {
                    System.IO.File.Delete(filePath);
                } catch { }
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

                await Toast.Make("PDF berhasil dibuat dan disimpan di folder Downloads").Show();

                var shareIntent = new Intent(Intent.ActionSend);
                shareIntent.SetType("application/pdf");
                shareIntent.PutExtra(Intent.ExtraStream, uri);
                shareIntent.PutExtra(Intent.ExtraText, $"Berikut adalah laporan transaksi {header.no_faktur}");
                
                var chooserIntent = Intent.CreateChooser(shareIntent, "Bagikan Laporan Transaksi");
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