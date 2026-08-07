namespace FinanceApp.Transaksi;

public partial class New_Transaksi : ContentPage
{
    private bool _isPemasukan = false;
    public New_Transaksi()
	{
		InitializeComponent();
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
}