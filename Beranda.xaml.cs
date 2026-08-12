using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace FinanceApp;

public partial class Beranda : ContentPage
{
	public Beranda()
	{
		InitializeComponent();
		LoadDummyData();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadApiDataAsync();
    }

    private async Task LoadApiDataAsync()
    {
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
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching data: {ex.Message}");
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