using CommunityToolkit.Maui.Alerts;

namespace FinanceApp.Kategori;

public partial class Edit_Kategori : ContentPage
{
    private string _selectedImagePath;
    private bool _isPemasukan = false;
    private KategoriModel _kategori;

    public Edit_Kategori(KategoriModel kategori)
    {
        InitializeComponent();
        _kategori = kategori;

        // Initialize UI with passed model
        e_nama_kategori.Text = _kategori.nama_kategori;
        c_isactive.IsToggled = _kategori.is_active;
        
        // Initialize Tipe
        _isPemasukan = _kategori.tipe;
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

        // Initialize Icon
        ImgIcon.Source = _kategori.FullIconUrl;
        L_IconStatus.Text = "Icon Saat Ini";
        L_IconStatus.TextColor = Colors.DarkCyan;
    }

    private async void SelectIcon_Tapped(object sender, TappedEventArgs e)
    {
        await IconBorder.ScaleTo(0.95, 100);
        await IconBorder.ScaleTo(1.0, 100);

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
                L_IconStatus.Text = "Icon Baru Dipilih";
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

    private async void BUpdate_Clicked(object sender, EventArgs e)
    {
        // Akan ditambahkan fungsi update API sesuai dengan petunjuk selanjutnya
        await Toast.Make("Fungsi perbarui akan diimplementasikan nanti!").Show();
    }

    private async void Cancel_Clicked(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}