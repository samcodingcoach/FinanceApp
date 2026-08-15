using System.Net.Http.Headers;
using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;

namespace FinanceApp.Kategori;

public partial class New_Kategori : ContentPage
{
    private string? _selectedImagePath;
    private bool _isPemasukan = false;

    public New_Kategori()
    {
        InitializeComponent();
    }

    [Obsolete]
    private async void SelectIcon_Tapped(object sender, TappedEventArgs e)
    {
        await IconBorder.ScaleToAsync(0.95, 100);
        await IconBorder.ScaleToAsync(1.0, 100);

        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Pilih Icon Kategori"
            });

            if (result != null)
            {
                var fileInfo = new FileInfo(result.FullPath);
                if (fileInfo.Length > 500 * 1024)
                {
                    await Toast.Make("Ukuran gambar maksimal 500KB").Show();
                    return;
                }

                _selectedImagePath = result.FullPath;
                var stream = await result.OpenReadAsync();
                ImgIcon.Source = ImageSource.FromStream(() => stream);
                L_IconStatus.Text = "Icon Dipilih";
                L_IconStatus.TextColor = Colors.DarkCyan;
            }
        }
        catch (Exception ex)
        {
            await Toast.Make("Pemilihan gambar dibatalkan.").Show();
        }
    }

    private void BPemasukan_Clicked(object sender, EventArgs e)
    {
        _isPemasukan = true;
        BPemasukan.BackgroundColor = Colors.DarkCyan;
        BPemasukan.TextColor = Colors.White;
        BPengeluaran.BackgroundColor = Colors.Transparent;
        BPengeluaran.TextColor = Colors.DarkGrey;
    }

    private void BPengeluaran_Clicked(object sender, EventArgs e)
    {
        _isPemasukan = false;
        BPengeluaran.BackgroundColor = Colors.DarkCyan;
        BPengeluaran.TextColor = Colors.White;
        BPemasukan.BackgroundColor = Colors.Transparent;
        BPemasukan.TextColor = Colors.DarkGrey;
    }

    private async void BSimpan_Clicked(object sender, EventArgs e)
    {
        string? namaKategori = e_nama_kategori.Text?.Trim();

        if (string.IsNullOrEmpty(namaKategori))
        {
            await Toast.Make("Nama kategori tidak boleh kosong").Show();
            return;
        }

        if (string.IsNullOrEmpty(_selectedImagePath))
        {
            await Toast.Make("Silakan pilih ikon kategori terlebih dahulu").Show();
            return;
        }

        string originalFileName = Path.GetFileName(_selectedImagePath);
        string safeFileName = originalFileName.Replace(" ", "");

        OverlayLoading.IsVisible = true;

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                // TAHAP 1: Upload Gambar
                string uploadUrl = $"https://oiotjlunbaohzypbslla.supabase.co/storage/v1/object/icon/{safeFileName}";
                
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);
                client.DefaultRequestHeaders.Add("x-upsert", "true");

                var content = new ByteArrayContent(File.ReadAllBytes(_selectedImagePath));
                content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

                var uploadResponse = await client.PostAsync(uploadUrl, content);

                if (!uploadResponse.IsSuccessStatusCode)
                {
                    string err = await uploadResponse.Content.ReadAsStringAsync();
                    await Toast.Make($"Gagal upload gambar: {uploadResponse.StatusCode}").Show();
                    OverlayLoading.IsVisible = false;
                    return;
                }

                // TAHAP 2: Simpan Kategori
                client.DefaultRequestHeaders.Remove("x-upsert");
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");

                string dbUrl = App.API_HOST + "kategori";
                var payload = new
                {
                    nama_kategori = namaKategori,
                    tipe = _isPemasukan,
                    icon = safeFileName,
                    is_active = c_isactive.IsToggled
                };

                var jsonContent = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
                var dbResponse = await client.PostAsync(dbUrl, jsonContent);

                if (dbResponse.IsSuccessStatusCode)
                {
                    await Toast.Make("Berhasil menyimpan kategori").Show();
                    
                    // Kembali ke halaman sebelumnya
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Navigation.PopAsync();
                    });
                }
                else
                {
                    string errDb = await dbResponse.Content.ReadAsStringAsync();
                    await Toast.Make($"Gagal simpan kategori: {dbResponse.StatusCode}").Show();
                }
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Terjadi kesalahan: {ex.Message}").Show();
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OverlayLoading.IsVisible = false;
            });
        }
    }

    private async void Cancel_Clicked(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}