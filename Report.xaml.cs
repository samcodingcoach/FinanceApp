namespace FinanceApp;

public partial class Report : ContentPage
{
	public Report()
	{
		InitializeComponent();
	}

    private void Tab_Clicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        if (btn == null) return;

        // Reset semua tombol ke style inactive
        ResetTabStyle(BtnMingguan);
        ResetTabStyle(BtnBulanan);
        ResetTabStyle(BtnTahunan);

        // Set style active untuk tombol yang ditekan
        btn.BackgroundColor = Color.FromArgb("#006948");
        btn.TextColor = Colors.White;

        // Atur ketersediaan filter (Tahun/Bulan) dan update label grafik berdasarkan mode
        if (btn == BtnMingguan)
        {
            PickerTahun.IsVisible = true;
            PickerBulan.IsVisible = true;
            LabelTahunIni.IsVisible = false;
            
            LblBar1.Text = "W1";
            LblBar2.Text = "W2";
            LblBar3.Text = "W3";
            LblBar4.Text = "W4";
            LblBar5.Text = "W5";
        }
        else if (btn == BtnBulanan)
        {
            PickerTahun.IsVisible = true;
            PickerBulan.IsVisible = false;
            LabelTahunIni.IsVisible = false;
            
            LblBar1.Text = "Jan";
            LblBar2.Text = "Feb";
            LblBar3.Text = "Mar";
            LblBar4.Text = "Apr";
            LblBar5.Text = "Mei";
        }
        else if (btn == BtnTahunan)
        {
            PickerTahun.IsVisible = false;
            PickerBulan.IsVisible = false;
            LabelTahunIni.IsVisible = true;
            
            LblBar1.Text = "2022";
            LblBar2.Text = "2023";
            LblBar3.Text = "2024";
            LblBar4.Text = "2025";
            LblBar5.Text = "2026";
        }
    }

    private void ResetTabStyle(Button btn)
    {
        btn.BackgroundColor = Colors.Transparent;
        btn.TextColor = Color.FromArgb("#3d4a42");
    }
}