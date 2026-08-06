namespace FinanceApp.Transaksi;

public partial class List_Transaksi : ContentPage
{
	public List_Transaksi()
	{
		InitializeComponent();
	}

    private void T_Search_TextChanged(object sender, TextChangedEventArgs e)
    {

    }

    private async void BtnAdd_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new New_Transaksi());
    }
}