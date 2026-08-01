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
        // 6 Data dummy Dokumen (Horizontal layout)
        var listDokumen = new ObservableCollection<DokumenModel>
        {
            new DokumenModel { IconImage = "receipt.png", IconBgColor = Color.FromArgb("#d0e1fb"), Judul = "Invoice #204", Tanggal = "24 Mei 2024" },
            new DokumenModel { IconImage = "description.png", IconBgColor = Color.FromArgb("#85f8c4"), Judul = "Laporan Pajak", Tanggal = "22 Mei 2024" },
            new DokumenModel { IconImage = "bag.png", IconBgColor = Color.FromArgb("#d0e1fb"), Judul = "Struk Belanja", Tanggal = "20 Mei 2024" },
            new DokumenModel { IconImage = "contract.png", IconBgColor = Color.FromArgb("#d0e1fb"), Judul = "Kontrak Sewa", Tanggal = "15 Mei 2024" },
            new DokumenModel { IconImage = "receipt.png", IconBgColor = Color.FromArgb("#d0e1fb"), Judul = "Invoice #205", Tanggal = "10 Mei 2024" },
            new DokumenModel { IconImage = "description.png", IconBgColor = Color.FromArgb("#85f8c4"), Judul = "Laporan Tahunan", Tanggal = "05 Mei 2024" }
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

public class DokumenModel
{
    public string IconImage { get; set; }
    public Color IconBgColor { get; set; }
    public string Judul { get; set; }
    public string Tanggal { get; set; }
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