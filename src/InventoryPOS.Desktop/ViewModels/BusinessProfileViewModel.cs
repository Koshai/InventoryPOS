using InventoryPOS.Application.DTOs.Settings;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Desktop.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;

namespace InventoryPOS.Desktop.ViewModels;

public sealed class BusinessProfileViewModel : INotifyPropertyChanged
{
    private readonly IBusinessProfileService _businessProfileService;
    private readonly IBusinessLogoService _businessLogoService;

    private Guid? _id;

    private string _businessName = string.Empty;
    private string _businessNameBn = string.Empty;

    private string _address = string.Empty;
    private string _addressBn = string.Empty;

    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _website = string.Empty;

    private string _binNumber = string.Empty;
    private string _tinNumber = string.Empty;
    private string _tradeLicenseNumber = string.Empty;

    private string _currencyCode = "BDT";
    private string _currencySymbol = "৳";

    private string _logoPath = string.Empty;
    private string _invoiceFooter = string.Empty;

    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;

    private bool _isBusy;
    private bool _isLoaded;

    public BusinessProfileViewModel(
        IBusinessProfileService businessProfileService,
        IBusinessLogoService businessLogoService)
    {
        _businessProfileService = businessProfileService;
        _businessLogoService = businessLogoService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid? Id
    {
        get => _id;
        private set => SetProperty(ref _id, value);
    }

    public string BusinessName
    {
        get => _businessName;
        set => SetProperty(ref _businessName, value);
    }

    public string BusinessNameBn
    {
        get => _businessNameBn;
        set => SetProperty(ref _businessNameBn, value);
    }

    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string AddressBn
    {
        get => _addressBn;
        set => SetProperty(ref _addressBn, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Website
    {
        get => _website;
        set => SetProperty(ref _website, value);
    }

    public string BinNumber
    {
        get => _binNumber;
        set => SetProperty(ref _binNumber, value);
    }

    public string TinNumber
    {
        get => _tinNumber;
        set => SetProperty(ref _tinNumber, value);
    }

    public string TradeLicenseNumber
    {
        get => _tradeLicenseNumber;
        set => SetProperty(ref _tradeLicenseNumber, value);
    }

    public string CurrencyCode
    {
        get => _currencyCode;
        set => SetProperty(ref _currencyCode, value);
    }

    public string CurrencySymbol
    {
        get => _currencySymbol;
        set => SetProperty(ref _currencySymbol, value);
    }

    public string LogoPath
    {
        get => _logoPath;
        set
        {
            if (SetProperty(ref _logoPath, value))
            {
                OnPropertyChanged(nameof(HasLogo));
                OnPropertyChanged(nameof(LogoButtonText));
                OnPropertyChanged(nameof(LogoAbsolutePath));
            }
        }
    }

    public string InvoiceFooter
    {
        get => _invoiceFooter;
        set => SetProperty(ref _invoiceFooter, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public string SuccessMessage
    {
        get => _successMessage;
        private set
        {
            if (SetProperty(ref _successMessage, value))
            {
                OnPropertyChanged(nameof(HasSuccess));
            }
        }
    }

    public bool HasError =>
        !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasSuccess =>
        !string.IsNullOrWhiteSpace(SuccessMessage);

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool IsLoaded
    {
        get => _isLoaded;
        private set => SetProperty(ref _isLoaded, value);
    }

    public bool HasLogo =>
    !string.IsNullOrWhiteSpace(LogoPath);

    public string LogoButtonText =>
        HasLogo
            ? "Replace Logo"
            : "Choose Logo";

    public string? LogoAbsolutePath =>
        _businessLogoService.GetAbsolutePath(LogoPath);

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;

        ClearMessages();

        try
        {
            var profile =
                await _businessProfileService.GetAsync();

            if (profile is null)
            {
                ResetToDefaults();
            }
            else
            {
                Apply(profile);
            }

            IsLoaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Unable to load business profile. {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<bool> SaveAsync()
    {
        if (IsBusy)
            return false;

        ClearMessages();

        if (!Validate())
            return false;

        IsBusy = true;

        try
        {
            var request = new BusinessProfileDto
            {
                Id = Id,

                BusinessName = BusinessName,
                BusinessNameBn = BusinessNameBn,

                Address = Address,
                AddressBn = AddressBn,

                Phone = Phone,
                Email = Email,
                Website = Website,

                BinNumber = BinNumber,
                TinNumber = TinNumber,
                TradeLicenseNumber = TradeLicenseNumber,

                CurrencyCode = CurrencyCode,
                CurrencySymbol = CurrencySymbol,

                LogoPath = LogoPath,

                InvoiceFooter = InvoiceFooter
            };

            var saved =
                await _businessProfileService.SaveAsync(request);

            Apply(saved);

            SuccessMessage =
                "Business profile saved successfully.";

            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Unable to save business profile. {ex.Message}";

            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SetLogoAsync(StorageFile file)
    {
        ClearMessages();

        try
        {
            LogoPath =
                await _businessLogoService.SaveAsync(file);
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Unable to save business logo. {ex.Message}";
        }
    }

    public async Task RemoveLogoAsync()
    {
        ClearMessages();

        try
        {
            await _businessLogoService.RemoveAsync(LogoPath);

            LogoPath = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Unable to remove business logo. {ex.Message}";
        }
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(BusinessName))
        {
            ErrorMessage =
                "Business name is required.";

            return false;
        }

        if (string.IsNullOrWhiteSpace(CurrencyCode))
        {
            ErrorMessage =
                "Currency code is required.";

            return false;
        }

        if (string.IsNullOrWhiteSpace(CurrencySymbol))
        {
            ErrorMessage =
                "Currency symbol is required.";

            return false;
        }

        return true;
    }

    private void Apply(BusinessProfileDto profile)
    {
        Id = profile.Id;

        BusinessName = profile.BusinessName;
        BusinessNameBn = profile.BusinessNameBn ?? string.Empty;

        Address = profile.Address ?? string.Empty;
        AddressBn = profile.AddressBn ?? string.Empty;

        Phone = profile.Phone ?? string.Empty;
        Email = profile.Email ?? string.Empty;
        Website = profile.Website ?? string.Empty;

        BinNumber = profile.BinNumber ?? string.Empty;
        TinNumber = profile.TinNumber ?? string.Empty;
        TradeLicenseNumber =
            profile.TradeLicenseNumber ?? string.Empty;

        CurrencyCode =
            string.IsNullOrWhiteSpace(profile.CurrencyCode)
                ? "BDT"
                : profile.CurrencyCode;

        CurrencySymbol =
            string.IsNullOrWhiteSpace(profile.CurrencySymbol)
                ? "৳"
                : profile.CurrencySymbol;

        LogoPath = profile.LogoPath ?? string.Empty;

        InvoiceFooter =
            profile.InvoiceFooter ?? string.Empty;
    }

    private void ResetToDefaults()
    {
        Id = null;

        BusinessName = string.Empty;
        BusinessNameBn = string.Empty;

        Address = string.Empty;
        AddressBn = string.Empty;

        Phone = string.Empty;
        Email = string.Empty;
        Website = string.Empty;

        BinNumber = string.Empty;
        TinNumber = string.Empty;
        TradeLicenseNumber = string.Empty;

        CurrencyCode = "BDT";
        CurrencySymbol = "৳";

        LogoPath = string.Empty;
        InvoiceFooter = string.Empty;
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    private bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);

        return true;
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}