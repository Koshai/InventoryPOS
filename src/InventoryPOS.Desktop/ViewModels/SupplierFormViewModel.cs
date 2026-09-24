using InventoryPOS.Application.DTOs.Suppliers;
using InventoryPOS.Application.Interfaces;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class SupplierFormViewModel : INotifyPropertyChanged
{
    private readonly ISupplierService _supplierService;

    private Guid? _supplierId;

    private string _supplierCode = string.Empty;
    private string _name = string.Empty;
    private string? _contactPerson;
    private string? _phone;
    private string? _email;
    private string? _address;
    private string? _taxNumber;
    private bool _isActive = true;
    private bool _isLoading;
    private bool _isSaving;
    private string? _errorMessage;

    public bool IsEditMode =>
        _supplierId.HasValue;

    public string PageTitle =>
        IsEditMode
            ? "Edit Supplier"
            : "Add Supplier";

    public string SupplierCode
    {
        get => _supplierCode;
        set
        {
            if (_supplierCode == value) return;
            _supplierCode = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    public string? ContactPerson
    {
        get => _contactPerson;
        set
        {
            if (_contactPerson == value) return;
            _contactPerson = value;
            OnPropertyChanged();
        }
    }

    public string? Phone
    {
        get => _phone;
        set
        {
            if (_phone == value) return;
            _phone = value;
            OnPropertyChanged();
        }
    }

    public string? Email
    {
        get => _email;
        set
        {
            if (_email == value) return;
            _email = value;
            OnPropertyChanged();
        }
    }

    public string? Address
    {
        get => _address;
        set
        {
            if (_address == value) return;
            _address = value;
            OnPropertyChanged();
        }
    }

    public string? TaxNumber
    {
        get => _taxNumber;
        set
        {
            if (_taxNumber == value) return;
            _taxNumber = value;
            OnPropertyChanged();
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value) return;
            _isActive = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (_isSaving == value) return;
            _isSaving = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value) return;
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public SupplierFormViewModel(
        ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    public async Task LoadAsync(Guid? supplierId)
    {
        _supplierId = supplierId;

        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));

        if (!supplierId.HasValue)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var supplier =
                await _supplierService.GetByIdAsync(
                    supplierId.Value);

            if (supplier is null)
            {
                ErrorMessage =
                    "Supplier could not be found.";

                return;
            }

            SupplierCode = supplier.SupplierCode;
            Name = supplier.Name;
            ContactPerson = supplier.ContactPerson;
            Phone = supplier.Phone;
            Email = supplier.Email;
            Address = supplier.Address;
            TaxNumber = supplier.TaxNumber;
            IsActive = supplier.IsActive;
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

    public async Task<bool> SaveAsync()
    {
        try
        {
            IsSaving = true;
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(SupplierCode))
            {
                ErrorMessage =
                    "Supplier code is required.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage =
                    "Supplier name is required.";

                return false;
            }

            if (_supplierId.HasValue)
            {
                await _supplierService.UpdateAsync(
                    new UpdateSupplierRequest
                    {
                        Id = _supplierId.Value,
                        SupplierCode = SupplierCode,
                        Name = Name,
                        ContactPerson = ContactPerson,
                        Phone = Phone,
                        Email = Email,
                        Address = Address,
                        TaxNumber = TaxNumber,
                        IsActive = IsActive
                    });
            }
            else
            {
                _supplierId =
                    await _supplierService.CreateAsync(
                        new CreateSupplierRequest
                        {
                            SupplierCode = SupplierCode,
                            Name = Name,
                            ContactPerson = ContactPerson,
                            Phone = Phone,
                            Email = Email,
                            Address = Address,
                            TaxNumber = TaxNumber,
                            IsActive = IsActive
                        });
            }

            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    public event PropertyChangedEventHandler?
        PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
}