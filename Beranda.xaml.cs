using CommunityToolkit.Maui.Alerts;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

#if ANDROID
using Android.Content;
using Android.Provider;
#endif

namespace FinanceApp;

public partial class Beranda : ContentPage
{
    private static DateTime _lastFetchTime = DateTime.MinValue;
    private IDispatcherTimer? _countdownTimer;
    private int _remainingSeconds;

    public static void ResetCache()
    {
        _lastFetchTime = DateTime.MinValue;
    }

	public Beranda()
	{
		InitializeComponent();
		LoadDummyData();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        StartCountdownTimer();
        await LoadApiDataAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopCountdownTimer();
    }

    private void StartCountdownTimer()
    {
        StopCountdownTimer();

        int intervalMinutes = Preferences.Get("refresh_interval_minutes", 30);
        if (intervalMinutes < 1) intervalMinutes = 1;
        if (intervalMinutes > 30) intervalMinutes = 30;

        if (_lastFetchTime == DateTime.MinValue)
        {
            _remainingSeconds = intervalMinutes * 60;
        }
        else
        {
            var elapsed = (DateTime.Now - _lastFetchTime).TotalSeconds;
            _remainingSeconds = (int)Math.Max(0, (intervalMinutes * 60) - elapsed);
        }

        UpdateCountdownText();

        _countdownTimer = Dispatcher.CreateTimer();
        _countdownTimer.Interval = TimeSpan.FromSeconds(1);
        _countdownTimer.Tick += async (s, e) =>
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                UpdateCountdownText();
            }
            else
            {
                int mins = Preferences.Get("refresh_interval_minutes", 30);
                if (mins < 1) mins = 1;
                if (mins > 30) mins = 30;
                _remainingSeconds = mins * 60;

                L_Countdown.Text = "Refresh...";
                if (ImgRefreshIcon != null)
                {
                    _ = ImgRefreshIcon.RelRotateTo(360, 600);
                }
                await LoadApiDataAsync(force: true);
                UpdateCountdownText();
            }
        };
        _countdownTimer.Start();
    }

    private void StopCountdownTimer()
    {
        if (_countdownTimer != null)
        {
            _countdownTimer.Stop();
            _countdownTimer = null;
        }
    }

    private void UpdateCountdownText()
    {
        int mins = _remainingSeconds / 60;
        int secs = _remainingSeconds % 60;
        L_Countdown.Text = $"{mins:D2}:{secs:D2}";
    }

    public async Task LoadApiDataAsync(bool force = false)
    {
        int intervalMinutes = Preferences.Get("refresh_interval_minutes", 30);
        if (intervalMinutes < 1) intervalMinutes = 1;
        if (intervalMinutes > 30) intervalMinutes = 30;

        // Hanya memanggil API jika sudah berlalu intervalMinutes dari fetch terakhir atau jika dipaksa (force)
        if (!force && (DateTime.Now - _lastFetchTime).TotalMinutes < intervalMinutes)
        {
            return;
        }

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);

                // 1. Saldo Akhir
                string urlSaldo = $"{App.API_HOST}/total_saldo_akhir";
                var resSaldo = await client.GetAsync(urlSaldo);
                if (resSaldo.IsSuccessStatusCode)
                {
                    string json = await resSaldo.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<TotalSaldoResponse>>(json);
                    if (list != null && list.Count > 0)
                    {
                        L_TotalSaldo.Text = $"Rp {list[0].total:N0}";
                    }
                }

                // 2. Pengeluaran dan Pemasukan
                string urlInOut = $"{App.API_HOST}/dashboard_pengeluaran_pemasukan";
                var resInOut = await client.GetAsync(urlInOut);
                if (resInOut.IsSuccessStatusCode)
                {
                    string json = await resInOut.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<PengeluaranPemasukanResponse>>(json);
                    if (list != null)
                    {
                        var pemasukan = list.FirstOrDefault(x => x.tipe == "Pemasukan");
                        var pengeluaran = list.FirstOrDefault(x => x.tipe == "Pengeluaran");
                        
                        L_Pemasukan.Text = pemasukan != null ? $"Rp {pemasukan.nominal:N0}" : "Rp 0";
                        L_Pengeluaran.Text = pengeluaran != null ? $"Rp {pengeluaran.nominal:N0}" : "Rp 0";
                    }
                }

                // 3. Anggaran Bulanan
                string urlAnggaran = $"{App.API_HOST}/dashboard_anggaran";
                var resAnggaran = await client.GetAsync(urlAnggaran);
                if (resAnggaran.IsSuccessStatusCode)
                {
                    string json = await resAnggaran.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<DashboardAnggaranResponse>>(json);
                    if (list != null && list.Count > 0)
                    {
                        var data = list[0];
                        decimal rencana = data.total_rencana;
                        decimal pakai = data.total_pemakaian;
                        decimal sisa = rencana - pakai;
                        
                        double persentase = 0;
                        if (rencana > 0)
                        {
                            persentase = (double)(pakai / rencana);
                        }

                        if (persentase > 1) persentase = 1;
                        if (persentase < 0) persentase = 0;

                        PB_Anggaran.Progress = persentase;
                        L_AnggaranTerpakaiPersen.Text = $"Terpakai {Math.Round(persentase * 100)}%";
                        L_AnggaranTersisa.Text = $"Tersisa Rp {sisa:N0} untuk bulan ini.";
                    }
                }
                // 4. Pengeluaran Anggota
                string urlAnggota = $"{App.API_HOST}/dashboard_pengeluaran_anggota";
                var resAnggota = await client.GetAsync(urlAnggota);
                if (resAnggota.IsSuccessStatusCode)
                {
                    string json = await resAnggota.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<DashboardPengeluaranAnggotaResponse>>(json);
                    if (list != null)
                    {
                        var mappedList = new ObservableCollection<PengeluaranModel>();
                        decimal grandTotal = list.Sum(x => x.total_nominal);
                        
                        // Urutkan dari pengeluaran terbesar
                        var sorted = list.OrderByDescending(x => x.total_nominal).ToList();
                        
                        for (int i = 0; i < sorted.Count; i++)
                        {
                            var item = sorted[i];
                            double progress = grandTotal > 0 ? (double)(item.total_nominal / grandTotal) : 0;
                            
                            // Warna default (ranking bawah)
                            Color nominalColor = Color.FromArgb("#171d19");
                            Color avatarBg = Color.FromArgb("#e9efe9");
                            Color avatarText = Color.FromArgb("#3d4a42");
                            Color progressColor = Color.FromArgb("#66a58f");
                            
                            if (i == 0) // Ranking 1 (Tertinggi)
                            {
                                nominalColor = Color.FromArgb("#006948");
                                avatarBg = Color.FromArgb("#d0e1fb");
                                avatarText = Color.FromArgb("#006948");
                                progressColor = Color.FromArgb("#006948");
                            }
                            else if (i == 2) // Ranking 3
                            {
                                progressColor = Color.FromArgb("#99c6b6");
                            }
                            
                            string bucketUrl = app?.BUCKET_URL ?? "";
                            string photoUrl = string.IsNullOrEmpty(item.photo) ? "nopic100.png" : $"{bucketUrl}/photo_user/{item.photo}";
                            
                            string displayNama = (string.IsNullOrEmpty(item.role) || item.role == "Lainnya") 
                                ? (item.nama_lengkap?.ToUpper() ?? "") 
                                : item.role.ToUpper();

                            mappedList.Add(new PengeluaranModel {
                                Urutan = (i + 1).ToString("D2"),
                                Nama = displayNama,
                                Nominal = $"Rp {item.total_nominal:N0}",
                                NominalColor = nominalColor,
                                AvatarBgColor = avatarBg,
                                AvatarTextColor = avatarText,
                                ProgressValue = progress,
                                ProgressColor = progressColor,
                                PhotoUrl = photoUrl
                            });
                        }
                        
                        BindableLayout.SetItemsSource(VS_PengeluaranAnggota, mappedList);
                    }
                }

                // 5. Transaksi Terakhir
                string urlTransaksi = $"{App.API_HOST}/dashboard_transaksi_akhir";
                var resTransaksi = await client.GetAsync(urlTransaksi);
                if (resTransaksi.IsSuccessStatusCode)
                {
                    string json = await resTransaksi.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<DashboardTransaksiAkhirResponse>>(json);
                    if (list != null)
                    {
                        var mappedList = new ObservableCollection<TransaksiModel>();
                        
                        foreach (var item in list)
                        {
                            // Berdasarkan sample data JSON, "tipe": false untuk "Ambeven" (obat) yang berarti pengeluaran.
                            // Jadi jika tipe == false maka itu adalah Pengeluaran, jika true maka Pemasukan.
                            bool isPengeluaran = !item.tipe;
                            
                            string bucketUrl = app?.BUCKET_URL ?? "";
                            string iconUrl = string.IsNullOrEmpty(item.icon) ? "cart.png" : $"{bucketUrl}/icon/{item.icon}";
                            
                            // Karena icon berwarna putih, gunakan warna latar (background) yang lebih gelap muda (solid)
                            Color bgColor = isPengeluaran ? Color.FromArgb("#ba5551") : Color.FromArgb("#006948");
                            if (string.IsNullOrEmpty(item.icon)) bgColor = Color.FromArgb("#d0e1fb");

                            string prefix = isPengeluaran ? "- Rp" : "+ Rp";
                            Color textColor = isPengeluaran ? Color.FromArgb("#ba5551") : Color.FromArgb("#006948");
                            
                            mappedList.Add(new TransaksiModel {
                                IconImage = iconUrl,
                                IconBgColor = bgColor,
                                Judul = item.nama_barang_jasa ?? "Transaksi",
                                Tanggal = item.created_at.ToString("dd MMM yyyy"),
                                Nominal = $"{prefix} {item.subtotal:N0}",
                                NominalColor = textColor
                            });
                        }
                        
                        BindableLayout.SetItemsSource(VS_TransaksiTerakhir, mappedList);
                    }
                }

                // 6. Dokumen Terakhir
                string urlDokumen = $"{App.API_HOST}/transaksi?select=no_faktur,keterangan,foto_transaksi&foto_transaksi=neq.&order=id_transaksi.desc&limit=4";
                var resDokumen = await client.GetAsync(urlDokumen);
                if (resDokumen.IsSuccessStatusCode)
                {
                    string json = await resDokumen.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<DashboardDokumenTerakhirResponse>>(json);
                    if (list != null)
                    {
                        var mappedList = new ObservableCollection<DokumenModel>();
                        
                        foreach (var item in list)
                        {
                            string bucketUrl = app?.BUCKET_URL ?? "";
                            string imageUrl = "nopic100.png";
                            bool isDownloadVisible = false;

                            if (!string.IsNullOrEmpty(item.foto_transaksi))
                            {
                                imageUrl = $"{bucketUrl}/transaksi/{item.foto_transaksi}";
                                isDownloadVisible = true;
                            }
                            
                            mappedList.Add(new DokumenModel {
                                ImageUrl = imageUrl,
                                Judul = item.no_faktur ?? "Dokumen",
                                Subtitle = item.keterangan ?? "-",
                                IsDownloadVisible = isDownloadVisible
                            });
                        }
                        
                        BindableLayout.SetItemsSource(HS_DokumenTerakhir, mappedList);
                    }
                }

                // 7. Pengingat Transaksi Favorit / Berulang (Maks 5 item yang belum lewat dari setiap_tanggal)
                string urlFavorit = $"{App.API_HOST}/rpc/get_favorit_transaksi";
                var resFavorit = await client.PostAsync(urlFavorit, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
                if (resFavorit.IsSuccessStatusCode)
                {
                    string json = await resFavorit.Content.ReadAsStringAsync();
                    var settings = new JsonSerializerSettings
                    {
                        DateParseHandling = DateParseHandling.DateTimeOffset
                    };
                    var listFav = JsonConvert.DeserializeObject<List<ReminderFavoritModel>>(json, settings);
                    if (listFav != null && listFav.Count > 0)
                    {
                        int currentDay = DateTime.Now.Day;

                        // Filter hanya yang belum lewat dari tanggal jadwal bulan ini (setiap_tanggal >= hari ini), lalu urutkan terdekat dan ambil maks 5
                        var upcomingFavs = listFav
                            .Where(x => x.setiap_tanggal >= currentDay)
                            .OrderBy(x => x.setiap_tanggal)
                            .Take(5)
                            .ToList();

                        if (upcomingFavs.Count > 0)
                        {
                            BindableLayout.SetItemsSource(HS_ReminderFavorit, upcomingFavs);
                            SectionReminder.IsVisible = true;
                        }
                        else
                        {
                            SectionReminder.IsVisible = false;
                        }
                    }
                    else
                    {
                        SectionReminder.IsVisible = false;
                    }
                }
                else
                {
                    SectionReminder.IsVisible = false;
                }
                
                // Jika semua API dieksekusi tanpa error, catat waktu terakhirnya
                _lastFetchTime = DateTime.Now;
                int mins = Preferences.Get("refresh_interval_minutes", 30);
                if (mins < 1) mins = 1;
                if (mins > 30) mins = 30;
                _remainingSeconds = mins * 60;
                UpdateCountdownText();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching data: {ex.Message}");
        }
    }

    private async void Download_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await Task.WhenAll(
                view.ScaleTo(0.8, 100),
                view.FadeTo(0.2, 100)
            );
            await Task.WhenAll(
                view.ScaleTo(1.0, 100),
                view.FadeTo(0.6, 100)
            );
        }

        if (e.Parameter is string url && !string.IsNullOrEmpty(url) && url.StartsWith("http"))
        {
            try
            {
#if ANDROID
                string fileName = System.IO.Path.GetFileName(new Uri(url).AbsolutePath);
                if (string.IsNullOrEmpty(fileName)) fileName = "foto_transaksi.jpg";
                
                string downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
                string filePath = System.IO.Path.Combine(downloadsPath, fileName);

                // Jika sudah ada tidak usah ditanyakan, hapus saja yang lama agar tertimpa dengan yang baru
                if (System.IO.File.Exists(filePath))
                {
                    try { System.IO.File.Delete(filePath); } catch { }
                }

                using var hc = new HttpClient();
                var imgBytes = await hc.GetByteArrayAsync(url);
                
                var context = Android.App.Application.Context;
                var values = new ContentValues();
                values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
                values.Put(MediaStore.IMediaColumns.MimeType, "image/jpeg");
                values.Put(MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryDownloads);

                var uri = context.ContentResolver.Insert(MediaStore.Downloads.ExternalContentUri, values);
                if (uri != null)
                {
                    using (var stream = context.ContentResolver.OpenOutputStream(uri))
                    {
                        await stream.WriteAsync(imgBytes, 0, imgBytes.Length);
                    }
                    await Toast.Make($"Foto {fileName} berhasil diunduh ke folder Downloads").Show();
                }
#else
                await Launcher.OpenAsync(new Uri(url));
#endif
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Gagal mengunduh foto: " + ex.Message, "OK");
            }
        }
    }

#pragma warning disable CS0618
    private async void MenuRekening_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 50);
            await view.ScaleTo(1, 50);
            await Navigation.PushAsync(new FinanceApp.Rekening.List_Akun());
        }
    }

    private async void MenuKategori_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 50);
            await view.ScaleTo(1, 50);
            await Navigation.PushAsync(new FinanceApp.Kategori.List_Kategori());
        }
    }

    private async void MenuAnggaran_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 50);
            await view.ScaleTo(1, 50);
            await Navigation.PushAsync(new FinanceApp.Budget.List_Budget());
        }
    }

    private async void MenuPengguna_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 50);
            await view.ScaleTo(1, 50);
            await Navigation.PushAsync(new FinanceApp.Users.List_Users());
        }
    }

    private async void MenuPengaturan_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 50);
            await view.ScaleTo(1, 50);
            await Navigation.PushAsync(new FinanceApp.Pengaturan.ListMenuPengaturan());
        }
    }

    private async void MenuFav_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleToAsync(0.9, 50);
            await view.ScaleToAsync(1, 50);
            await Navigation.PushAsync(new Favorit.ListFav());
        }
    }

    private async void ReminderItem_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border)
        {
            await border.ScaleToAsync(0.95, 80);
            await border.ScaleToAsync(1.0, 80);

            if (e.Parameter is int id_fav)
            {
                var page = new Favorit.List_FavDetail(id_fav, () =>
                {
                    ResetCache();
                    _ = LoadApiDataAsync(force: true);
                });
                page.HasHandle = true;
                page.HasBackdrop = true;
                _ = page.ShowAsync(Window);
            }
        }
    }
#pragma warning restore CS0618

    private async void LihatSemuaPengeluaran_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.9, 50);
            await view.ScaleTo(1, 50);
            await Navigation.PushAsync(new FinanceApp.Users.Transaksi_Users());
        }
    }

    private void LoadDummyData()
    {
        // Data dummy Pengeluaran Anggota
        var listPengeluaran = new ObservableCollection<PengeluaranModel>
        {
            new PengeluaranModel { Urutan = "01", Nama = "IBU", Nominal = "Rp 30.000.000", NominalColor = Color.FromArgb("#006948"), AvatarBgColor = Color.FromArgb("#d0e1fb"), AvatarTextColor = Color.FromArgb("#006948"), ProgressValue = 0.70, ProgressColor = Color.FromArgb("#006948") },
            new PengeluaranModel { Urutan = "02", Nama = "ANAK", Nominal = "Rp 10.000.000", NominalColor = Color.FromArgb("#171d19"), AvatarBgColor = Color.FromArgb("#e9efe9"), AvatarTextColor = Color.FromArgb("#3d4a42"), ProgressValue = 0.23, ProgressColor = Color.FromArgb("#66a58f") },
            new PengeluaranModel { Urutan = "03", Nama = "AYAH", Nominal = "Rp 3.000.000", NominalColor = Color.FromArgb("#171d19"), AvatarBgColor = Color.FromArgb("#e9efe9"), AvatarTextColor = Color.FromArgb("#3d4a42"), ProgressValue = 0.07, ProgressColor = Color.FromArgb("#99c6b6") }
        };
        BindableLayout.SetItemsSource(VS_PengeluaranAnggota, listPengeluaran);

        // Data dummy Dokumen (Horizontal layout dengan Image)
        var listDokumen = new ObservableCollection<DokumenModel>
        {
            new DokumenModel { ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuD2K8a9Bt_DziYwwMuxMzZatodKeXKalcz-qTu-Q3DkNA3SprpaXFPuZgw8a_vGgL2HKdd1b_2YUSkRbsB8ceiUGO6z9hJwG2BlMflEYnAxu1hlrleNnrSrqP4JjGidYULMnrTbgqa4lRYs95AmdTcrlTC0VPeiAKmus_K7wRUlzGwskTNSyIq4Kj0ehWxXDT_T_6g1YHGHVyOepx66bPKBd3sWDHkl4wH_biNjbJkNmVvfhdeKDXx03g", Judul = "Invoice #204", Subtitle = "Cloud Hosting Service", StatusText = "Paid", StatusBgColor = Color.FromArgb("#d0e1fb"), StatusTextColor = Color.FromArgb("#0b1c30"), ActionIcon = "file_download" },
            new DokumenModel { ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCnbRQsD-Jq2Hz0SRSY6yTN3jggCCwMtMb2nm7km9OwHkggdyRMnl5qwyvRLA3Kvvdy-YV00YB1Y35gUiJp0Ud1CPYTxEWqNgftWE_BzJM_Thp_9w7F4FlqMy2GNxCjy05J8wNTFjMzU4Rru3CCMK39VZuQtuKFoHKrE8NxtmkEt3rsmNFcr_5N89vY8CLNvktcXZzeJ7h8dA3YbErHv2y3-AyOkNwRpSbg8vcaHN4he3zphDFDOiHWPw", Judul = "Tax Report", Subtitle = "Q3 Corporate Tax 2023", StatusText = "Draft", StatusBgColor = Color.FromArgb("#ffdad6"), StatusTextColor = Color.FromArgb("#93000a"), ActionIcon = "visibility" },
            new DokumenModel { ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuD2K8a9Bt_DziYwwMuxMzZatodKeXKalcz-qTu-Q3DkNA3SprpaXFPuZgw8a_vGgL2HKdd1b_2YUSkRbsB8ceiUGO6z9hJwG2BlMflEYnAxu1hlrleNnrSrqP4JjGidYULMnrTbgqa4lRYs95AmdTcrlTC0VPeiAKmus_K7wRUlzGwskTNSyIq4Kj0ehWxXDT_T_6g1YHGHVyOepx66bPKBd3sWDHkl4wH_biNjbJkNmVvfhdeKDXx03g", Judul = "Grocery Receipt", Subtitle = "May Supermarket", StatusText = "Paid", StatusBgColor = Color.FromArgb("#d0e1fb"), StatusTextColor = Color.FromArgb("#0b1c30"), ActionIcon = "file_download" }
        };

        BindableLayout.SetItemsSource(HS_DokumenTerakhir, listDokumen);

        // Data dummy Transaksi (Vertical layout)
        var listTransaksi = new ObservableCollection<TransaksiModel>
        {
            new TransaksiModel { IconImage = "cart.png", IconBgColor = Color.FromArgb("#d0e1fb"), Judul = "Belanja", Tanggal = "24 Mei 2024", Nominal = "- Rp 150.000", NominalColor = Color.FromArgb("#ba5551") },
            new TransaksiModel { IconImage = "payments.png", IconBgColor = Color.FromArgb("#85f8c4"), Judul = "Gaji", Tanggal = "25 Mei 2024", Nominal = "+ Rp 10.000.000", NominalColor = Color.FromArgb("#006948") },
            new TransaksiModel { IconImage = "car.png", IconBgColor = Color.FromArgb("#d0e1fb"), Judul = "Transportasi", Tanggal = "26 Mei 2024", Nominal = "- Rp 20.000", NominalColor = Color.FromArgb("#ba5551") },
            new TransaksiModel { IconImage = "restaurant.png", IconBgColor = Color.FromArgb("#d0e1fb"), Judul = "Makan di luar", Tanggal = "26 Mei 2024", Nominal = "- Rp 215.000", NominalColor = Color.FromArgb("#ba5551") }
        };

        BindableLayout.SetItemsSource(VS_TransaksiTerakhir, listTransaksi);
    }
}

public class PengeluaranModel
{
    public string Urutan { get; set; }
    public string Nama { get; set; }
    public string Nominal { get; set; }
    public Color NominalColor { get; set; }
    public Color AvatarBgColor { get; set; }
    public Color AvatarTextColor { get; set; }
    public double ProgressValue { get; set; }
    public Color ProgressColor { get; set; }
    public string PhotoUrl { get; set; }
}

public class DokumenModel
{
    public string ImageUrl { get; set; }
    public string Judul { get; set; }
    public string Subtitle { get; set; }
    public string StatusText { get; set; }
    public Color StatusBgColor { get; set; }
    public Color StatusTextColor { get; set; }
    public string ActionIcon { get; set; }
    public bool IsDownloadVisible { get; set; }
}

public class TransaksiModel
{
    public string IconImage { get; set; }
    public Color IconBgColor { get; set; }
    public string Judul { get; set; }
    public string Tanggal { get; set; }
    public string Nominal { get; set; }
    public Color NominalColor { get; set; }
}

public class TotalSaldoResponse
{
    public decimal total { get; set; }
}

public class PengeluaranPemasukanResponse
{
    public string tipe { get; set; }
    public decimal nominal { get; set; }
}

public class DashboardAnggaranResponse
{
    public decimal total_rencana { get; set; }
    public decimal total_pemakaian { get; set; }
}

public class DashboardPengeluaranAnggotaResponse
{
    public int id_users { get; set; }
    public string nama_lengkap { get; set; }
    public string photo { get; set; }
    public string role { get; set; }
    public decimal total_nominal { get; set; }
}

public class DashboardTransaksiAkhirResponse
{
    public int id_transaksi { get; set; }
    public DateTime created_at { get; set; }
    public bool tipe { get; set; }
    public string icon { get; set; }
    public decimal subtotal { get; set; }
    public string nama_barang_jasa { get; set; }
}

public class DashboardDokumenTerakhirResponse
{
    public string no_faktur { get; set; }
    public string keterangan { get; set; }
    public string foto_transaksi { get; set; }
}

public class ReminderFavoritModel
{
    public int id_fav { get; set; }
    public DateTimeOffset created_at { get; set; }
    public int id_kategori { get; set; }
    public string? keterangan { get; set; }
    public int setiap_tanggal { get; set; }
    public string? nama_kategori { get; set; }
    public bool tipe { get; set; }
    public string? icon { get; set; }
    public decimal total_harga { get; set; }

    [JsonIgnore]
    public Color BgColor => tipe ? Color.FromArgb("#16841E") : Color.FromArgb("#FA5252");

    [JsonIgnore]
    public string FullIconUrl
    {
        get
        {
            if (string.IsNullOrEmpty(icon)) return "nopic100.png";
            if (icon.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return icon;
            var app = Application.Current as App;
            string? cleanBucket = app?.BUCKET_URL;
            if (!string.IsNullOrEmpty(cleanBucket) && !cleanBucket.EndsWith("/")) cleanBucket += "/";
            string cleanIcon = icon.StartsWith("/") ? icon.Substring(1) : icon;
            return $"{cleanBucket}icon/{cleanIcon}";
        }
    }

    [JsonIgnore]
    public string NominalDisplay => $"{(tipe ? "+" : "-")} Rp {total_harga:N0}";

    [JsonIgnore]
    public Color NominalColor => tipe ? Colors.Green : Colors.OrangeRed;

    [JsonIgnore]
    public string? TitleDisplay => string.IsNullOrEmpty(keterangan) ? nama_kategori : keterangan;

    [JsonIgnore]
    public string ScheduleDisplay => $"Tanggal {setiap_tanggal}";
}