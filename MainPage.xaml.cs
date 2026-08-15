namespace FinanceApp
{
    public partial class MainPage : Shell
    {
       

        private bool _isInitialized = false;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            if (!_isInitialized)
            {
                _isInitialized = true;
                string savedUser = Preferences.Get("user_data", string.Empty);
                if (string.IsNullOrEmpty(savedUser))
                {
                    // Tampilkan Login sebagai Modal di atas MainPage
                    await Navigation.PushModalAsync(new NavigationPage(new Login()));
                }
            }
        }
    }
}
