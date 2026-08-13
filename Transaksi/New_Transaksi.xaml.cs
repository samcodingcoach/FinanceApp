using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;
using System.Linq;

namespace FinanceApp.Transaksi;

public partial class New_Transaksi : ContentPage
{
    private bool _isPemasukan = false;
    private ObservableCollection<KategoriData> _kategoris;

    public New_Transaksi()
	{
		InitializeComponent();
        _kategoris = new ObservableCollection<KategoriData>();
        KategoriCollectionView.ItemsSource = _kategoris;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Load data pengeluaran (karena _isPemasukan = false secara default)
        LoadKategori();

        // Update ringkasan detail item jika ada (jumlah item & total nominal)
        if (New_Transaksi_Detail.TempDetailItems != null && New_Transaksi_Detail.TempDetailItems.Count > 0)
        {
            int jumlahItem = New_Transaksi_Detail.TempDetailItems.Count;
            decimal grandTotal = New_Transaksi_Detail.TempDetailItems.Sum(x => x.Subtotal);

            LabelDetailCount.Text = $"{jumlahItem} Item Detail Barang / Jasa";
            LabelDetailCount.TextColor = Colors.CornflowerBlue;

            if (grandTotal > 0)
            {
                T_Nominal.Text = grandTotal.ToString("N0");
            }
        }
        else
        {
            LabelDetailCount.Text = "Tambah Detail Barang / Jasa";
            LabelDetailCount.TextColor = Colors.Grey;
        }
    }

    private async void LoadKategori()
    {
        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                // api endpoint: kategori?is_active=eq.true&tipe=eq.{_isPemasukan}
                string url = $"{App.API_HOST}/kategori?is_active=eq.true&tipe=eq.{_isPemasukan.ToString().ToLower()}";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<KategoriData>>(json);
                    
                    _kategoris.Clear();
                    if (data != null)
                    {
                        foreach (var item in data)
                        {
                            _kategoris.Add(item);
                        }
                    }
                }
                else
                {
                    await Toast.Make("Gagal memuat kategori").Show();
                }
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Error memuat kategori: {ex.Message}").Show();
        }
    }

    private void BPemasukan_Clicked(object sender, EventArgs e)
    {
        if (_isPemasukan) return;
        _isPemasukan = true;
        BPemasukan.BackgroundColor = Colors.DarkCyan;
        BPemasukan.TextColor = Colors.White;
        BPengeluaran.BackgroundColor = Colors.Transparent;
        BPengeluaran.TextColor = Colors.DarkGrey;
        LoadKategori();
    }

    private void BPengeluaran_Clicked(object sender, EventArgs e)
    {
        if (!_isPemasukan) return;
        _isPemasukan = false;
        BPengeluaran.BackgroundColor = Colors.DarkCyan;
        BPengeluaran.TextColor = Colors.White;
        BPemasukan.BackgroundColor = Colors.Transparent;
        BPemasukan.TextColor = Colors.DarkGrey;
        LoadKategori();
    }

    private async void Kategori_Tapped(object sender, TappedEventArgs e)
    {
        var selectedItem = e.Parameter as KategoriData;
        if (selectedItem == null) return;
        
        foreach (var item in _kategoris)
        {
            item.IsSelected = (item == selectedItem);
        }

        await Toast.Make($"Memilih {selectedItem.nama_kategori} (ID: {selectedItem.id_kategori})").Show();
    }

    private int? _id_rekening = null;

    private async void TapRekening_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is StackLayout stackLayout)
        {
            await stackLayout.FadeToAsync(0.3, 100); // Turunkan opacity ke 0.3 dalam 100ms
            await stackLayout.FadeToAsync(1, 200);   // Kembalikan opacity ke 1 dalam 200ms

            var page = new Transaksi.PilihRekening_BottomSheet(!_isPemasukan);
            page.HasHandle = true;
            page.HasBackdrop = true;
            
            page.RekeningSelected += async (s, rekening) => 
            {
                _id_rekening = rekening.id_rekening;
                LabelPilihRekening.Text = rekening.nama_rekening;
                
                await Toast.Make($"Rekening terpilih: {rekening.nama_rekening} (ID: {rekening.id_rekening})").Show();
            };

            _ = page.ShowAsync(Window);
        }
    }

    private async void Back_Tapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void DetailItem_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            _ = view.ScaleTo(0.95, 100).ContinueWith(t => view.ScaleTo(1, 100));
            _ = view.FadeTo(0.5, 100).ContinueWith(t => view.FadeTo(1, 100));
        }

        await Task.Delay(150); // Menunggu sejenak agar animasi klik terlihat sebelum berpindah halaman
        await Navigation.PushAsync(new New_Transaksi_Detail());
    }

    private byte[] _strukBytes = null;
    private string _strukFilename = null;
    private string _uploadedKey = null;

    private async void Camera_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            _ = view.ScaleTo(0.8, 100).ContinueWith(t => view.ScaleTo(1, 100));
            _ = view.FadeTo(0.5, 100).ContinueWith(t => view.FadeTo(1, 100));
        }

        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                FileResult photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    await ProcessPhoto(photo);
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Gagal membuka kamera: {ex.Message}", "OK");
        }
    }

    private async void Gallery_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            _ = view.ScaleTo(0.8, 100).ContinueWith(t => view.ScaleTo(1, 100));
            _ = view.FadeTo(0.5, 100).ContinueWith(t => view.FadeTo(1, 100));
        }

        try
        {
            FileResult photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo != null)
            {
                await ProcessPhoto(photo);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Gagal membuka galeri: {ex.Message}", "OK");
        }
    }

    private async Task ProcessPhoto(FileResult photo)
    {
        try
        {
            LabelUploadStatus.Text = "Memproses gambar...";
            
            // Generate filename based on timestamp
            string ext = Path.GetExtension(photo.FileName);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
            _strukFilename = $"struk_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";

            using var stream = await photo.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            
            // Compress using SkiaSharp
            using (var originalBitmap = SkiaSharp.SKBitmap.Decode(stream))
            {
                int maxDim = 1080;
                int newWidth = originalBitmap.Width;
                int newHeight = originalBitmap.Height;

                if (originalBitmap.Width > maxDim || originalBitmap.Height > maxDim)
                {
                    double ratio = Math.Min((double)maxDim / originalBitmap.Width, (double)maxDim / originalBitmap.Height);
                    newWidth = (int)(originalBitmap.Width * ratio);
                    newHeight = (int)(originalBitmap.Height * ratio);
                }
                
                using (var resizedBitmap = originalBitmap.Resize(new SkiaSharp.SKImageInfo(newWidth, newHeight), SkiaSharp.SKSamplingOptions.Default))
                {
                    using (var image = SkiaSharp.SKImage.FromBitmap(resizedBitmap))
                    {
                        using (var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 75))
                        {
                            data.SaveTo(memoryStream);
                        }
                    }
                }
            }
            
            _strukBytes = memoryStream.ToArray();
            LabelUploadStatus.Text = "Gambar siap diunggah";
        }
        catch (Exception ex)
        {
            LabelUploadStatus.Text = "Upload Min 500kb";
            await DisplayAlert("Error", $"Gagal mengolah gambar: {ex.Message}", "OK");
        }
    }

    private async Task<bool> UploadPhotoToSupabase()
    {
        if (_strukBytes == null || string.IsNullOrEmpty(_strukFilename))
            return true; // Tidak ada foto yang perlu diunggah, anggap sukses

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;
            
            // Endpoint for storage API: /storage/v1/object/transaksi/{filename}
            string baseUrl = App.API_HOST.Replace("/rest/v1/", "/storage/v1/object/");
            string uploadUrl = $"{baseUrl}transaksi/{_strukFilename}";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);
                
                var content = new ByteArrayContent(_strukBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                
                var response = await client.PostAsync(uploadUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var resultObj = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                    string fullKey = resultObj.Key;
                    
                    if (!string.IsNullOrEmpty(fullKey) && fullKey.StartsWith("transaksi/"))
                    {
                        _uploadedKey = fullKey.Substring("transaksi/".Length);
                    }
                    else
                    {
                        _uploadedKey = fullKey;
                    }
                    
                    return true;
                }
                else
                {
                    string err = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error Upload", err, "OK");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Gagal mengunggah gambar: {ex.Message}", "OK");
            return false;
        }
    }

    private async void BSimpan_Clicked(object sender, EventArgs e)
    {
        // Validasi form
        if (string.IsNullOrWhiteSpace(T_Nominal.Text) || T_Nominal.Text == "0")
        {
            await Toast.Make("Nominal transaksi harus diisi!").Show();
            return;
        }

        var selectedKategori = _kategoris.FirstOrDefault(k => k.IsSelected);
        if (selectedKategori == null)
        {
            await Toast.Make("Pilih kategori terlebih dahulu!").Show();
            return;
        }

        if (_id_rekening == null)
        {
            await Toast.Make("Pilih rekening terlebih dahulu!").Show();
            return;
        }
        
        string cleanNominal = new string(T_Nominal.Text.Where(char.IsDigit).ToArray());
        if (!decimal.TryParse(cleanNominal, out decimal nominalValue) || nominalValue <= 0)
        {
            await Toast.Make("Nominal transaksi tidak valid!").Show();
            return;
        }

        OverlayLoading.IsVisible = true;
        
        OverlayText.Text = "Menyiapkan Data... 10%";
        await Task.Delay(500); // Sebagian dari 3 detik delay

        // 1. Upload photo first if it exists
        if (_strukBytes != null)
        {
            OverlayText.Text = "Upload Image... 30%";
            await Task.Delay(500); // Simulasi delay
            
            bool uploadSuccess = await UploadPhotoToSupabase();
            if (!uploadSuccess)
            {
                OverlayLoading.IsVisible = false;
                return; // Batalkan simpan transaksi jika upload gambar gagal
            }
        }
        
        OverlayText.Text = "Simpan Transaksi... 60%";
        await Task.Delay(1500); // Sebagian dari 3 detik delay
        
        // 2. Simpan data transaksi API_HOST + transaksi method post
        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            var trxData = new
            {
                no_faktur = NoFaktur.Text ?? "",
                id_users = Preferences.Get("id_user", 3), // Default 3 sesuai agy.txt
                id_rekening = _id_rekening,
                id_kategori = selectedKategori.id_kategori,
                foto_transaksi = _uploadedKey ?? "",
                keterangan = T_Catatan.Text ?? "",
                created_at = string.Format("{0:yyyy-MM-dd}", DP_Tanggal.Date)
            };

            string trxJson = JsonConvert.SerializeObject(trxData);
            
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);
                // Minta return row untuk mendapatkan id_transaksi (Supabase PostgREST)
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");
                
                var content = new StringContent(trxJson, System.Text.Encoding.UTF8, "application/json");
                string urlTrx = $"{App.API_HOST}/transaksi";
                
                var response = await client.PostAsync(urlTrx, content);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    OverlayText.Text = "Simpan Detail Transaksi... 90%";
                    await Task.Delay(500); // Tambahan smooth delay

                    string resJson = await response.Content.ReadAsStringAsync();
                    var insertedTrx = JsonConvert.DeserializeObject<List<dynamic>>(resJson);
                    
                    if (insertedTrx != null && insertedTrx.Count > 0)
                    {
                        int id_transaksi = insertedTrx[0].id_transaksi;
                        
                        // 3. Simpan detail transaksi API_HOST + transaksi_detail method post
                        if (New_Transaksi_Detail.TempDetailItems != null && New_Transaksi_Detail.TempDetailItems.Count > 0)
                        {
                            var listDetail = new List<object>();
                            foreach(var item in New_Transaksi_Detail.TempDetailItems)
                            {
                                listDetail.Add(new {
                                    id_transaksi = id_transaksi,
                                    nama_barang_jasa = item.NamaBarang ?? "",
                                    harga = item.HargaNumeric ?? 0,
                                    jumlah = item.JumlahNumeric ?? 0,
                                    subtotal = item.Subtotal
                                });
                            }
                            
                            string detailJson = JsonConvert.SerializeObject(listDetail);
                            var detailContent = new StringContent(detailJson, System.Text.Encoding.UTF8, "application/json");
                            
                            // Endpoint yang benar adalah transaksi_detail
                            string urlDetail = $"{App.API_HOST}/transaksi_detail";
                            
                            // Send batch insert for details
                            await client.PostAsync(urlDetail, detailContent);
                            
                            // Bersihkan temporary detail setelah sukses
                            New_Transaksi_Detail.TempDetailItems.Clear();
                        }
                    }
                    
                    OverlayText.Text = "Selesai... 100%";
                    await Task.Delay(500);
                }
                else
                {
                    string err = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Gagal menyimpan transaksi: {err}", "OK");
                    OverlayLoading.IsVisible = false;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Terjadi kesalahan: {ex.Message}", "OK");
            OverlayLoading.IsVisible = false;
            return;
        }
        
        OverlayLoading.IsVisible = false;
        
        await Toast.Make("Transaksi berhasil disimpan!").Show();
        
        await Navigation.PopAsync();
    }
}

public class KategoriData : INotifyPropertyChanged
{
    public int id_kategori { get; set; }
    public string? nama_kategori { get; set; }
    public bool tipe { get; set; }
    public bool is_active { get; set; }
    public string icon { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IconBackgroundColor));
            }
        }
    }

    [JsonIgnore]
    public string DisplayIcon 
    {
        get
        {
            if (string.IsNullOrEmpty(icon)) return "sampelicon1.png";
            var app = Application.Current as App;
            string bucket = app?.BUCKET_URL ?? "";
            if (!bucket.EndsWith("/")) bucket += "/";
            if (icon.StartsWith("/")) icon = icon.Substring(1);
            if (!icon.StartsWith("icon/")) icon = "icon/" + icon;
            return bucket + icon;
        }
    }

    [JsonIgnore]
    public Color IconBackgroundColor => IsSelected ? Colors.CornflowerBlue : Colors.LightGray;

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}