using The49.Maui.BottomSheet;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using FinanceApp.Rekening; // To access AkunRekening

namespace FinanceApp.Transaksi;

public partial class PilihRekening_BottomSheet : BottomSheet
{
    private ObservableCollection<AkunRekening> _allAkun;
    private int _offset = 0;
    private const int _limit = 50;
    private bool _isLoadingMore = false;
    private bool _hasMoreData = true;

    // Optional event if we want to return data to parent
    public event EventHandler<AkunRekening> RekeningSelected;

    private bool _isPengeluaran;
    private decimal _nominalRequired;

    public PilihRekening_BottomSheet(bool isPengeluaran = false, decimal nominalRequired = 0)
    {
        _isPengeluaran = isPengeluaran;
        _nominalRequired = nominalRequired;
        InitializeComponent();
        _allAkun = new ObservableCollection<AkunRekening>();
        ListAkunCollection.ItemsSource = _allAkun;
        
        _ = LoadDataAsync(true);
    }

    private async void AkunRefresh_Refreshing(object sender, EventArgs e)
    {
        _offset = 0;
        _hasMoreData = true;
        _allAkun.Clear();
        await LoadDataAsync(false);
    }

    private async void ListAkunCollection_RemainingItemsThresholdReached(object sender, EventArgs e)
    {
        if (_isLoadingMore || !_hasMoreData) return;
        
        _offset += _limit;
        await LoadDataAsync(false);
    }

    private async Task LoadDataAsync(bool showOverlay = true)
    {
        if (_isLoadingMore) return;
        _isLoadingMore = true;

        if (showOverlay) OverlayLoading.IsVisible = true;

        try
        {
            var app = Application.Current as App;
            string tokenKey = app?.TOKEN_KEY ?? string.Empty;
            string apiUrl = App.API_HOST + $"akun_rekening?limit={_limit}&offset={_offset}";
            
            // Jika transaksi pengeluaran, jangan tampilkan rekening bersaldo 0 atau minus
            if (_isPengeluaran)
            {
                apiUrl += "&saldo_akhir=gt.0";
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenKey);
                client.DefaultRequestHeaders.Add("apikey", tokenKey);
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");

                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<AkunRekening>>(responseContent);

                    if (result != null && result.Count > 0)
                    {
                        var sorted = result.OrderByDescending(x => x.last_update ?? x.created_at).ToList();
                        foreach (var item in sorted)
                        {
                            _allAkun.Add(item);
                        }

                        if (result.Count < _limit)
                        {
                            _hasMoreData = false;
                        }
                    }
                    else
                    {
                        _hasMoreData = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (Application.Current.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
                }
            });
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (showOverlay) OverlayLoading.IsVisible = false;
                AkunRefresh.IsRefreshing = false;
                _isLoadingMore = false;
            });
        }
    }

    private AkunRekening _selectedRekening;
    
    private Border _lastSelectedBorder;

    private async void Rekening_Tapped(object sender, TappedEventArgs e)
    {
        var selectedItem = e.Parameter as AkunRekening;
        if (selectedItem != null && sender is Border currentBorder)
        {
            if (_isPengeluaran && (decimal)selectedItem.saldo_akhir < _nominalRequired)
            {
                if (Application.Current.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Saldo Tidak Cukup", $"Saldo rekening {selectedItem.nama_rekening} (Rp {selectedItem.saldo_akhir:N0}) tidak mencukupi untuk transaksi senilai Rp {_nominalRequired:N0}.", "OK");
                }
                return;
            }

            if (_selectedRekening == selectedItem)
            {
                // Tap ke-2 pada item yang sama: Konfirmasi pilihan dan kembali ke New_Transaksi
                RekeningSelected?.Invoke(this, _selectedRekening);
                await this.DismissAsync();
            }
            else
            {
                // Tap ke-1 pada item ini: Tandai sebagai terpilih (Ubah Warna)
                _selectedRekening = selectedItem;
                
                // Kembalikan warna item sebelumnya ke putih
                if (_lastSelectedBorder != null)
                {
                    _lastSelectedBorder.BackgroundColor = Colors.White;
                }
                
                // Ubah warna item yang baru di-tap menjadi biru muda sebagai indikasi terpilih
                currentBorder.BackgroundColor = Color.FromArgb("#e6f2ff"); 
                _lastSelectedBorder = currentBorder;
            }
        }
    }

    private async void BtnPilih_Clicked(object sender, EventArgs e)
    {
        if (_selectedRekening != null)
        {
            RekeningSelected?.Invoke(this, _selectedRekening);
            await this.DismissAsync();
        }
        else
        {
            if (Application.Current.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Pilih Rekening", "Silakan pilih salah satu rekening terlebih dahulu.", "OK");
            }
        }
    }
}