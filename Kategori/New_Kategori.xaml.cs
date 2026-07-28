using Microsoft.Maui.Graphics.Text;
using static System.Net.Mime.MediaTypeNames;

namespace FinanceApp.Kategori;

public partial class New_Kategori : ContentPage
{
    bool tipe_kategori = false;
	public New_Kategori()
	{
		InitializeComponent();
	}

    private string _selectedImagePath;

    private async void SelectIcon_Tapped(object sender, TappedEventArgs e)
    {
        // Animation feedback
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
                _selectedImagePath = result.FullPath;
                var stream = await result.OpenReadAsync();
                ImgIcon.Source = ImageSource.FromStream(() => stream);
                L_IconStatus.Text = "Icon Dipilih";
                L_IconStatus.TextColor = Colors.DarkCyan;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Info", "Pemilihan gambar dibatalkan atau terjadi kesalahan.", "OK");
        }
    }

    private void BSimpan_Clicked(object sender, EventArgs e)
    {

    }

    private void BPemasukan_Clicked(object sender, EventArgs e)
    {
        
        BPemasukan.BackgroundColor = Colors.DarkCyan;
        BPengeluaran.BackgroundColor = Colors.Transparent;
        tipe_kategori = true;


    }

    private void BPengeluaran_Clicked(object sender, EventArgs e)
    {
        BPemasukan.BackgroundColor = Colors.Transparent;
        BPengeluaran.BackgroundColor = Colors.DarkCyan;
        tipe_kategori = false;
    }
}