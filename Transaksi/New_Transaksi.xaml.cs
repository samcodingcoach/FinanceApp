using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;

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

            var page = new Transaksi.PilihRekening_BottomSheet();
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
        OverlayLoading.IsVisible = true;
        
        // 1. Upload photo first if it exists
        if (_strukBytes != null)
        {
            bool uploadSuccess = await UploadPhotoToSupabase();
            if (!uploadSuccess)
            {
                OverlayLoading.IsVisible = false;
                return; // Batalkan simpan transaksi jika upload gambar gagal
            }
        }
        
        // 2. Simulasikan simpan API (tunggu data sampai tersimpan status 201)
        await Task.Delay(3000); // Dummy delay sesuai instruksi "selama 3 detik"
        
        OverlayLoading.IsVisible = false;
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