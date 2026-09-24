using InventoryPOS.Application.DTOs.Customers;
using InventoryPOS.Application.Interfaces;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class CustomerFormViewModel : INotifyPropertyChanged
{
    private readonly ICustomerService _customerService;

    private Guid? _customerId;

    private string _customerCode = string.Empty;
    private string _name = string.Empty;
    private string? _phone;
    private string? _email;
    private string? _address;
    private double _creditLimit;
    private bool _isActive = true;
    private bool _isLoading;
    private bool _isSaving;
    private string? _errorMessage;

    public bool IsEditMode =>
        _customerId.HasValue;

    public string PageTitle =>
        IsEditMode
            ? "Edit Customer"
            : "Add Customer";

    public string SaveButtonText =>
        IsEditMode
            ? "Save Changes"
            : "Add Customer";

    public string CustomerCode
    {
        get => _customerCode;
        set
        {
            _customerCode = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public string? Phone
    {
        get => _phone;
        set
        {
            _phone = value;
            OnPropertyChanged();
        }
    }

    public string? Email
    {
        get => _email;
        set
        {
            _email = value;
            OnPropertyChanged();
        }
    }

    public string? Address
    {
        get => _address;
        set
        {
            _address = value;
            OnPropertyChanged();
        }
    }

    public double CreditLimit
    {
        get => _creditLimit;
        set
        {
            _creditLimit = value;
            OnPropertyChanged();
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            _isSaving = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public CustomerFormViewModel(
        ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task LoadAsync(Guid? customerId)
    {
        _customerId = customerId;

        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(SaveButtonText));

        if (!customerId.HasValue)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var customer =
                await _customerService
                    .GetByIdAsync(customerId.Value);

            if (customer is null)
            {
                ErrorMessage =
                    "Customer could not be found.";

                return;
            }

            CustomerCode =
                customer.CustomerCode;

            Name =
                customer.Name;

            Phone =
                customer.Phone;

            Email =
                customer.Email;

            Address =
                customer.Address;

            CreditLimit =
                Convert.ToDouble(
                    customer.CreditLimit);

            IsActive =
                customer.IsActive;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<Guid?> SaveAsync()
    {
        try
        {
            IsSaving = true;
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(CustomerCode))
            {
                ErrorMessage =
                    "Customer code is required.";

                return null;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage =
                    "Customer name is required.";

                return null;
            }

            if (CreditLimit < 0)
            {
                ErrorMessage =
                    "Credit limit cannot be negative.";

                return null;
            }

            if (_customerId.HasValue)
            {
                await _customerService.UpdateAsync(
                    new UpdateCustomerRequest
                    {
                        Id = _customerId.Value,

                        CustomerCode =
                            CustomerCode,

                        Name =
                            Name,

                        Phone =
                            Phone,

                        Email =
                            Email,

                        Address =
                            Address,

                        CreditLimit =
                            Convert.ToDecimal(
                                CreditLimit),

                        IsActive =
                            IsActive
                    });

                return _customerId.Value;
            }

            return await _customerService.CreateAsync(
                new CreateCustomerRequest
                {
                    CustomerCode =
                        CustomerCode,

                    Name =
                        Name,

                    Phone =
                        Phone,

                    Email =
                        Email,

                    Address =
                        Address,

                    CreditLimit =
                        Convert.ToDecimal(
                            CreditLimit),

                    IsActive =
                        IsActive
                });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;

            return null;
        }
        finally
        {
            IsSaving = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}