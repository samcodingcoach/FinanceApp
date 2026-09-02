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
    private int? _selectedIdKategori = null;
    private FavoritImportItemModel? _nearestReminderFav = null;

    public New_Transaksi()
	{
		InitializeComponent();
        _kategoris = new ObservableCollection<KategoriData>();
        KategoriCollectionView.ItemsSource = _kategoris;
        TP_Waktu.Time = DateTime.Now.TimeOfDay;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Hanya load kategori jika belum pernah dimuat sebelumnya agar seleksi tidak hilang
        if (_kategoris.Count == 0)
        {
            LoadKategori();
        }

        // Cek pengingat favorit terdekat (Model 1)
        _ = LoadNearestReminderFavoritAsync();

        // Update ringkasan detail item jika ada (jumlah item & total nominal)
        if (New_Transaksi_Detail.TempDetailItems != null && New_Transaksi_Detail.TempDetailItems.Count > 0)
        {
            int jumlahItem = New_Transaksi_Detail.TempDetailItems.Count;
            decimal grandTotal = New_Transaksi_Detail.TempDetailItems.Sum(x => x.Subtotal);

            LabelDetailCount.Text = $"{jumlahItem} Item Detail Barang / Jasa";
            LabelDetailCount.TextColor = Colors.CornflowerBlue;

            T_Nominal.Text = grandTotal.ToString("N0");
        }
        else
        {
            LabelDetailCount.Text = "Tambah Detail Barang / Jasa";
            LabelDetailCount.TextColor = Colors.Grey;
            T_Nominal.Text = string.Empty;
        }
    }

    private async Task LoadNearestReminderFavoritAsync()
    {
        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                string url = $"{App.API_HOST}/rpc/get_favorit_transaksi";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                var response = await client.PostAsync(url, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.DateTimeOffset };
                    var data = JsonConvert.DeserializeObject<List<FavoritImportItemModel>>(json, settings);

                    if (data != null && data.Count > 0)
                    {
                        int currentDay = DateTime.Now.Day;
                        // Ambil yang belum lewat bulan ini (setiap_tanggal >= currentDay), urutkan yang paling dekat
                        var upcoming = data.Where(x => x.setiap_tanggal >= currentDay).OrderBy(x => x.setiap_tanggal).FirstOrDefault();

                        if (upcoming != null)
                        {
                            _nearestReminderFav = upcoming;
                            int diffDays = upcoming.setiap_tanggal - currentDay;
                            string infoJadwal = diffDays == 0 ? "Jadwal hari ini" : $"{diffDays} hari lagi";

                            L_BannerReminderTitle.Text = $"{upcoming.TitleDisplay}";
                            L_BannerReminderSubtitle.Text = $"Setiap Tanggal {upcoming.setiap_tanggal} ({infoJadwal}) • Rp {upcoming.total_harga:N0}";
                            BannerReminderInfo.IsVisible = true;
                            return;
                        }
                    }
                }
            }
            BannerReminderInfo.IsVisible = false;
        }
        catch
        {
            BannerReminderInfo.IsVisible = false;
        }
    }

    private async void ApplyBannerReminder_Tapped(object sender, TappedEventArgs e)
    {
        if (_nearestReminderFav != null)
        {
            if (sender is View view)
            {
                await view.ScaleTo(0.9, 50);
                await view.ScaleTo(1.0, 50);
            }
            await ApplyFavoritDataToForm(_nearestReminderFav);
            BannerReminderInfo.IsVisible = false;
        }
    }

    private async void BtnImportFavorit_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 50);
            await view.ScaleTo(1.0, 50);
        }

        var sheet = new PilihFavorit_BottomSheet();
        sheet.HasHandle = true;
        sheet.HasBackdrop = true;

        sheet.FavoritSelected += async (s, selectedFav) =>
        {
            await ApplyFavoritDataToForm(selectedFav);
        };

        _ = sheet.ShowAsync(Window);
    }

    private async Task ApplyFavoritDataToForm(FavoritImportItemModel fav)
    {
        // 1. Set Tipe Transaksi (Pemasukan / Pengeluaran)
        if (fav.tipe != _isPemasukan)
        {
            _isPemasukan = fav.tipe;
            if (_isPemasukan)
            {
                BPemasukan.BackgroundColor = Colors.DarkCyan;
                BPemasukan.TextColor = Colors.White;
                BPengeluaran.BackgroundColor = Colors.Transparent;
                BPengeluaran.TextColor = Colors.DarkGrey;
            }
            else
            {
                BPengeluaran.BackgroundColor = Colors.DarkCyan;
                BPengeluaran.TextColor = Colors.White;
                BPemasukan.BackgroundColor = Colors.Transparent;
                BPemasukan.TextColor = Colors.DarkGrey;
            }
            // Muat kategori sesuai tipe baru
            await LoadKategoriAsync();
        }

        // 2. Pilih Kategori yang sesuai
        _selectedIdKategori = fav.id_kategori;
        foreach (var k in _kategoris)
        {
            k.IsSelected = (k.id_kategori == fav.id_kategori);
        }

        // 3. Set Catatan
        T_Catatan.Text = fav.keterangan ?? fav.nama_kategori ?? "";

        // 4. Ambil dan isi Detail Barang / Jasa dari Supabase
        await FetchAndApplyFavoritDetailAsync(fav.id_fav, fav.total_harga, fav.TitleDisplay ?? "");

        await Toast.Make($"Berhasil mengimpor {fav.TitleDisplay}").Show();
    }

    private async Task FetchAndApplyFavoritDetailAsync(int idFav, decimal defaultTotal, string defaultTitle)
    {
        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                string url = $"{App.API_HOST}/favorit_transaksi_detail?id_fav=eq.{idFav}&order=id_fav_detail.asc";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<FavoritDetailResponseModel>>(json);

                    New_Transaksi_Detail.TempDetailItems.Clear();

                    if (list != null && list.Count > 0)
                    {
                        foreach (var item in list)
                        {
                            New_Transaksi_Detail.TempDetailItems.Add(new FormDetailItem
                            {
                                NamaBarang = item.nama_barang_jasa,
                                HargaString = item.harga.ToString("N0"),
                                JumlahString = "1"
                            });
                        }
                    }
                    else if (defaultTotal > 0)
                    {
                        New_Transaksi_Detail.TempDetailItems.Add(new FormDetailItem
                        {
                            NamaBarang = defaultTitle,
                            HargaString = defaultTotal.ToString("N0"),
                            JumlahString = "1"
                        });
                    }

                    int count = New_Transaksi_Detail.TempDetailItems.Count;
                    decimal sumTotal = New_Transaksi_Detail.TempDetailItems.Sum(x => x.Subtotal);

                    LabelDetailCount.Text = $"{count} Item Detail Barang / Jasa";
                    LabelDetailCount.TextColor = Colors.CornflowerBlue;
                    T_Nominal.Text = sumTotal.ToString("N0");
                    return;
                }
            }
        }
        catch { }

        // Fallback jika fetch detail gagal
        T_Nominal.Text = defaultTotal.ToString("N0");
    }

    private Task LoadKategoriAsync()
    {
        var tcs = new TaskCompletionSource<bool>();
        LoadKategori();
        tcs.SetResult(true);
        return tcs.Task;
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
                            if (_selectedIdKategori != null && item.id_kategori == _selectedIdKategori.Value)
                            {
                                item.IsSelected = true;
                            }
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
        _selectedIdKategori = null;
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
        _selectedIdKategori = null;
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
        
        _selectedIdKategori = selectedItem.id_kategori;
        foreach (var item in _kategoris)
        {
            item.IsSelected = (item.id_kategori == selectedItem.id_kategori);
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

            decimal nominalVal = 0;
            if (!string.IsNullOrWhiteSpace(T_Nominal.Text))
            {
                string cleanNominal = new string(T_Nominal.Text.Where(char.IsDigit).ToArray());
                decimal.TryParse(cleanNominal, out nominalVal);
            }

            var page = new Transaksi.PilihRekening_BottomSheet(!_isPemasukan, nominalVal);
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
            _ = view.ScaleToAsync(0.95, 100).ContinueWith(t => view.ScaleToAsync(1, 100));
            _ = view.FadeToAsync(0.5, 100).ContinueWith(t => view.FadeToAsync(1, 100));
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
            _ = view.ScaleToAsync(0.8, 100).ContinueWith(t => view.ScaleToAsync(1, 100));
            _ = view.FadeToAsync(0.5, 100).ContinueWith(t => view.FadeToAsync(1, 100));
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
            await DisplayAlertAsync("Error", $"Gagal membuka kamera: {ex.Message}", "OK");
        }
    }

    private async void Gallery_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            _ = view.ScaleToAsync(0.8, 100).ContinueWith(t => view.ScaleToAsync(1, 100));
            _ = view.FadeToAsync(0.5, 100).ContinueWith(t => view.FadeToAsync(1, 100));
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
            await DisplayAlertAsync("Error", $"Gagal membuka galeri: {ex.Message}", "OK");
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
            await DisplayAlertAsync("Error", $"Gagal mengolah gambar: {ex.Message}", "OK");
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
                    await DisplayAlertAsync("Error Upload", err, "OK");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Gagal mengunggah gambar: {ex.Message}", "OK");
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

            int currentUserId = Preferences.Get("id_user", 0);
            if (currentUserId <= 0)
            {
                string jsonUser = Preferences.Get("user_data", string.Empty);
                if (!string.IsNullOrEmpty(jsonUser))
                {
                    try
                    {
                        var jObj = Newtonsoft.Json.Linq.JObject.Parse(jsonUser);
                        currentUserId = (int?)jObj["id_users"] ?? (int?)jObj["user_id"] ?? (int?)jObj["id_user"] ?? (int?)jObj["id"] ?? 0;
                    }
                    catch { }
                }
            }

            TimeSpan chosenTime = (TP_Waktu?.Time != null && TP_Waktu.Time.Value != TimeSpan.Zero)
                ? TP_Waktu.Time.Value 
                : DateTime.Now.TimeOfDay;

            DateTime selectedDate = DP_Tanggal?.Date ?? DateTime.Today;
            DateTime fullDateTime = selectedDate.Date + chosenTime;

            var trxData = new
            {
                no_faktur = NoFaktur.Text ?? "",
                id_users = currentUserId > 0 ? currentUserId : 1,
                id_rekening = _id_rekening,
                id_kategori = selectedKategori.id_kategori,
                foto_transaksi = _uploadedKey ?? "",
                keterangan = T_Catatan.Text ?? "",
                created_at = fullDateTime.ToString("yyyy-MM-dd HH:mm:ss")
            };

            string trxJson = JsonConvert.SerializeObject(trxData);
            
            //System.Diagnostics.Debug.WriteLine("================ [TRANSAKSI DEBUG] ================");
            //System.Diagnostics.Debug.WriteLine($"[TRANSAKSI PAYLOAD]: {trxJson}");
            //System.Diagnostics.Debug.WriteLine($"[TRANSAKSI ID_USERS DIGUNAKAN]: {trxData.id_users}");
            //System.Diagnostics.Debug.WriteLine("====================================================");
            
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
                        else
                        {
                            // Jika user menginput manual tanpa masuk ke halaman detail
                            // Variabel nominalValue sudah didapatkan di awal fungsi BSimpan_Clicked

                            var listDetail = new List<object>
                            {
                                new {
                                    id_transaksi = id_transaksi,
                                    nama_barang_jasa = selectedKategori?.nama_kategori ?? "Transaksi Umum",
                                    harga = nominalValue,
                                    jumlah = 1,
                                    subtotal = nominalValue
                                }
                            };
                            
                            string detailJson = JsonConvert.SerializeObject(listDetail);
                            var detailContent = new StringContent(detailJson, System.Text.Encoding.UTF8, "application/json");
                            string urlDetail = $"{App.API_HOST}/transaksi_detail";
                            await client.PostAsync(urlDetail, detailContent);
                        }
                    }
                    
                    OverlayText.Text = "Selesai... 100%";
                    await Task.Delay(500);
                }
                else
                {
                    string err = await response.Content.ReadAsStringAsync();
                    await DisplayAlertAsync("Error", $"Gagal menyimpan transaksi: {err}", "OK");
                    OverlayLoading.IsVisible = false;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Terjadi kesalahan: {ex.Message}", "OK");
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
    public string? icon { get; set; }

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

public class FavoritDetailResponseModel
{
    public int id_fav_detail { get; set; }
    public int id_fav { get; set; }
    public string nama_barang_jasa { get; set; } = string.Empty;
    public decimal harga { get; set; }
}