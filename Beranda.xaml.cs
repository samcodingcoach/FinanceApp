using System.Collections.ObjectModel;

namespace FinanceApp;

public partial class Beranda : ContentPage
{
	public Beranda()
	{
		InitializeComponent();
		LoadDummyData();
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