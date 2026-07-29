namespace FinanceApp.Users;

public partial class List_Users : ContentPage
{
	public List_Users()
	{
		InitializeComponent();
	}

    private void T_Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        //RefreshLocalFilter();
    }

    private async void BtnMore_Tapped(object sender, TappedEventArgs e)
    {
        var img = sender as Image;
        if (img == null) return;

        // Animation feedback
        await img.ScaleToAsync(0.8, 100);
        await img.ScaleToAsync(1, 100);

        if (img.Source.ToString().Contains("close100.png"))
        {
            T_Search.Text = string.Empty;
            StackLayoutSearch.IsVisible = false;
            StackLayoutTitle.IsVisible = true;
            img.Source = "more50gray.png";
            img.Rotation = 90;
        }
        else
        {
            string action = await DisplayActionSheetAsync("Opsi", "Batal", null, "Search");
            if (action == "Search")
            {
                StackLayoutTitle.IsVisible = false;
                StackLayoutSearch.IsVisible = true;
                img.Source = "close100.png";
                img.Rotation = 0;
            }
        }
    }
}