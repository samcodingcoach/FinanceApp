using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Alerts;

namespace FinanceApp.Transaksi;

public partial class New_Transaksi_Detail : ContentPage, INotifyPropertyChanged
{
    public static ObservableCollection<FormDetailItem> TempDetailItems { get; set; } = new ObservableCollection<FormDetailItem>();

    public ObservableCollection<FormDetailItem> DetailItems { get; set; } = new ObservableCollection<FormDetailItem>();
    
    private decimal _grandTotal;
    public decimal GrandTotal
    {
        get => _grandTotal;
        set
        {
            if (_grandTotal != value)
            {
                _grandTotal = value;
                OnPropertyChanged();
            }
        }
    }

    public New_Transaksi_Detail()
    {
        InitializeComponent();
        
        // Load dari TempDetailItems kalau ada, atau kasih form kosong
        if (TempDetailItems.Count > 0)
        {
            foreach (var item in TempDetailItems)
            {
                var copyItem = new FormDetailItem
                {
                    NamaBarang = item.NamaBarang,
                    HargaString = item.HargaString,
                    JumlahString = item.JumlahString
                };
                copyItem.PropertyChanged += FormDetailItem_PropertyChanged;
                DetailItems.Add(copyItem);
            }
            CalculateGrandTotal();
        }
        else
        {
            AddNewFormDetail();
        }

        DetailItems.CollectionChanged += (s, e) => CalculateGrandTotal();
        BindingContext = this;
    }

    private void AddNewFormDetail()
    {
        var newItem = new FormDetailItem();
        newItem.PropertyChanged += FormDetailItem_PropertyChanged;
        DetailItems.Add(newItem);
    }

    private void FormDetailItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FormDetailItem.Subtotal))
        {
            CalculateGrandTotal();
        }
    }

    private void CalculateGrandTotal()
    {
        decimal total = 0;
        foreach (var item in DetailItems)
        {
            total += item.Subtotal;
        }
        GrandTotal = total;
    }

    private void BtnAddDetail_Clicked(object sender, EventArgs e)
    {
        AddNewFormDetail();
    }

    private async void DeleteItem_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is FormDetailItem item)
        {
            bool isEmpty = string.IsNullOrWhiteSpace(item.NamaBarang) && (item.HargaNumeric == null || item.HargaNumeric == 0) && (item.JumlahNumeric == null || item.JumlahNumeric == 0);
            
            if (isEmpty)
            {
                item.PropertyChanged -= FormDetailItem_PropertyChanged;
                DetailItems.Remove(item);
            }
            else
            {
                bool answer = await DisplayAlert("Konfirmasi", "Apakah Anda yakin ingin menghapus detail ini?", "Ya", "Tidak");
                if (answer)
                {
                    item.PropertyChanged -= FormDetailItem_PropertyChanged;
                    DetailItems.Remove(item);
                    await Toast.Make("Form detail berhasil dihapus").Show();
                }
            }
        }
    }

    private async void BSimpan_Clicked(object sender, EventArgs e)
    {
        TempDetailItems.Clear();
        foreach (var item in DetailItems)
        {
            if (!string.IsNullOrWhiteSpace(item.NamaBarang) || item.HargaNumeric > 0 || item.JumlahNumeric > 0)
            {
                TempDetailItems.Add(item);
            }
        }
        await Toast.Make("Detail transaksi disimpan secara temporary").Show();
        await Navigation.PopAsync();
    }

    private bool _isFormatting = false;

    private void Harga_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isFormatting) return;

        if (sender is Entry entry)
        {
            string raw = (e.NewTextValue ?? "").Replace(".", "").Replace(",", "").Trim();
            if (string.IsNullOrEmpty(raw)) return;

            if (decimal.TryParse(raw, out decimal nominal))
            {
                if (nominal < 0) nominal = 0;
                string formatted = nominal.ToString("N0");

                if (formatted != (e.NewTextValue ?? ""))
                {
                    Dispatcher.Dispatch(() =>
                    {
                        _isFormatting = true;
                        entry.Text = formatted;
                        entry.CursorPosition = formatted.Length;
                        _isFormatting = false;
                    });
                }
            }
        }
    }

    private void Jumlah_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isFormatting) return;

        if (sender is Entry entry)
        {
            string raw = (e.NewTextValue ?? "").Replace(".", "").Replace(",", "").Trim();
            if (string.IsNullOrEmpty(raw)) return;

            if (int.TryParse(raw, out int nominal))
            {
                if (nominal < 0) nominal = 0;
                string formatted = nominal.ToString("N0");

                if (formatted != (e.NewTextValue ?? ""))
                {
                    Dispatcher.Dispatch(() =>
                    {
                        _isFormatting = true;
                        entry.Text = formatted;
                        entry.CursorPosition = formatted.Length;
                        _isFormatting = false;
                    });
                }
            }
        }
    }
}

public class FormDetailItem : INotifyPropertyChanged
{
    private string _namaBarang;
    public string NamaBarang
    {
        get => _namaBarang;
        set { _namaBarang = value; OnPropertyChanged(); }
    }

    public decimal? HargaNumeric { get; private set; }
    
    private string _hargaString;
    public string HargaString
    {
        get => _hargaString;
        set 
        {
            if (_hargaString == value) return;
            _hargaString = value;

            if (string.IsNullOrWhiteSpace(value))
            {
                HargaNumeric = null;
            }
            else
            {
                string cleanStr = new string(value.Where(char.IsDigit).ToArray());
                if (decimal.TryParse(cleanStr, out decimal parsedValue))
                {
                    if (parsedValue < 0) parsedValue = 0;
                    HargaNumeric = parsedValue;
                }
                else
                {
                    HargaNumeric = null;
                }
            }

            OnPropertyChanged(); // Beritahu UI
            OnPropertyChanged(nameof(Subtotal));
        }
    }

    public int? JumlahNumeric { get; private set; }

    private string _jumlahString;
    public string JumlahString
    {
        get => _jumlahString;
        set 
        { 
            if (_jumlahString == value) return;
            _jumlahString = value;

            if (string.IsNullOrWhiteSpace(value))
            {
                JumlahNumeric = null;
            }
            else
            {
                string cleanStr = new string(value.Where(char.IsDigit).ToArray());
                if (int.TryParse(cleanStr, out int parsedValue))
                {
                    if (parsedValue < 0) parsedValue = 0;
                    JumlahNumeric = parsedValue;
                }
                else
                {
                    JumlahNumeric = null;
                }
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(Subtotal));
        }
    }

    public decimal Subtotal => (HargaNumeric ?? 0) * (JumlahNumeric ?? 0);

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}