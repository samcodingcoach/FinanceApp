namespace FinanceApp.Pengaturan;

public partial class ListMenuPengaturan : ContentPage
{
	public ListMenuPengaturan()
	{
		InitializeComponent();
	}

    private async void SettingFinger_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is View view)
        {
            await view.ScaleTo(0.95, 50);
            await view.ScaleTo(1, 50);
            await Navigation.PushAsync(new SettingFinger());
        }
    }
}