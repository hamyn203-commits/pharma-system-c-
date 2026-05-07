using System.Collections.ObjectModel;
using AlNeda.Admin.Services;
using AlNeda.Core.Entities;
using AlNeda.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class ProductsViewModel : ObservableObject
{
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private readonly IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<Product> _products = [];
    [ObservableProperty] private ObservableCollection<Category> _categories = [];
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private Product _editProduct = new();
    [ObservableProperty] private string _validationMessage = "";

    public ProductsViewModel(ProductService productService, CategoryService categoryService, IDialogService dialog)
    {
        _productService = productService;
        _categoryService = categoryService;
        _dialog = dialog;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        Products = new ObservableCollection<Product>(await _productService.GetAllAsync(SearchText));
        Categories = new ObservableCollection<Category>(await _categoryService.GetAllAsync());
        IsLoading = false;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private void NewProduct()
    {
        EditProduct = new Product { IsActive = 1, Category = "عام" };
        ShowEditor = true;
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedProduct == null) return;
        EditProduct = new Product
        {
            Id = SelectedProduct.Id,
            Name = SelectedProduct.Name,
            Barcode = SelectedProduct.Barcode,
            CategoryId = SelectedProduct.CategoryId,
            Category = SelectedProduct.Category,
            Company = SelectedProduct.Company,
            Quantity = SelectedProduct.Quantity,
            UnitPrice = SelectedProduct.UnitPrice,
            ExpiryDate = SelectedProduct.ExpiryDate,
            ImagePath = SelectedProduct.ImagePath,
            ImageUrl = SelectedProduct.ImageUrl,
            IsActive = SelectedProduct.IsActive,
            ProductImagesJson = SelectedProduct.ProductImagesJson,
            Description = SelectedProduct.Description,
        };
        ShowEditor = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            ValidationMessage = "";

            if (string.IsNullOrWhiteSpace(EditProduct?.Name))
            {
                ValidationMessage = "اسم المنتج مطلوب";
                return;
            }

            if (EditProduct == null)
            {
                ValidationMessage = "خطأ: لم يتم تحديد المنتج";
                return;
            }

            if (EditProduct.Quantity < 0)
            {
                ValidationMessage = "الكمية لا يمكن أن تكون سالبة";
                return;
            }

            if (EditProduct.UnitPrice < 0)
            {
                ValidationMessage = "السعر لا يمكن أن يكون سالباً";
                return;
            }

            if (EditProduct.CategoryId.HasValue)
            {
                var cat = Categories.FirstOrDefault(c => c.Id == EditProduct.CategoryId);
                if (cat != null) EditProduct.Category = cat.Name;
            }

            if (EditProduct.Id == 0)
            {
                EditProduct.CreatedAt = DateTime.Now;
                EditProduct.UpdatedAt = DateTime.Now;
                await _productService.AddAsync(EditProduct);
            }
            else
            {
                EditProduct.UpdatedAt = DateTime.Now;
                await _productService.UpdateAsync(EditProduct);
            }

            ShowEditor = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ValidationMessage = $"خطأ: {ex.Message}";
            Serilog.Log.Error(ex, "Failed to save product");
        }
    }

    [RelayCommand]
    private void CancelEdit() => ShowEditor = false;

    [RelayCommand]
    private async Task DeleteAsync(Product? product)
    {
        if (product == null) return;
        if (!_dialog.Confirm($"هل أنت متأكد من حذف المنتج '{product.Name}'؟")) return;
        await _productService.DeleteAsync(product.Id);
        await LoadAsync();
    }
}

public partial class CategoriesViewModel : ObservableObject
{
    private readonly CategoryService _categoryService;
    private readonly IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<Category> _categories = [];
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private string _newName = "";
    [ObservableProperty] private string _validationMessage = "";

    public CategoriesViewModel(CategoryService categoryService, IDialogService dialog) { _categoryService = categoryService; _dialog = dialog; }

    [RelayCommand] private async Task LoadAsync() { IsLoading = true; Categories = new ObservableCollection<Category>(await _categoryService.GetAllAsync()); IsLoading = false; }

    [RelayCommand]
    private async Task AddAsync()
    {
        ValidationMessage = "";
        if (string.IsNullOrWhiteSpace(NewName)) { ValidationMessage = "اسم الفئة مطلوب"; return; }
        await _categoryService.AddAsync(new Category { Name = NewName.Trim() });
        NewName = "";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(Category? cat)
    {
        if (cat == null) return;
        if (!_dialog.Confirm($"هل أنت متأكد من حذف الفئة '{cat.Name}'؟")) return;
        await _categoryService.DeleteAsync(cat.Id);
        await LoadAsync();
    }
}

public partial class SuppliersViewModel : ObservableObject
{
    private readonly SupplierService _supplierService;
    private readonly IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<Supplier> _suppliers = [];
    [ObservableProperty] private Supplier? _selectedSupplier;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private Supplier _editSupplier = new();
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _validationMessage = "";

    public SuppliersViewModel(SupplierService supplierService, IDialogService dialog) { _supplierService = supplierService; _dialog = dialog; }

    [RelayCommand] private async Task LoadAsync() { IsLoading = true; Suppliers = new ObservableCollection<Supplier>(await _supplierService.GetAllAsync(SearchText)); IsLoading = false; }
    [ObservableProperty] private bool _isLoading;

    [RelayCommand] private void NewSupplier() { EditSupplier = new Supplier(); ShowEditor = true; }

    [RelayCommand] private void EditSelected() { if (SelectedSupplier == null) return; EditSupplier = new Supplier { Id = SelectedSupplier.Id, Name = SelectedSupplier.Name, Phone = SelectedSupplier.Phone, Address = SelectedSupplier.Address, Company = SelectedSupplier.Company, Balance = SelectedSupplier.Balance, Notes = SelectedSupplier.Notes }; ShowEditor = true; }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = "";
        if (string.IsNullOrWhiteSpace(EditSupplier.Name)) { ValidationMessage = "اسم المورد مطلوب"; return; }
        if (EditSupplier.Balance < 0) { ValidationMessage = "الرصيد لا يمكن أن يكون سالباً"; return; }
        if (EditSupplier.Id == 0) await _supplierService.AddAsync(EditSupplier);
        else await _supplierService.UpdateAsync(EditSupplier);
        ShowEditor = false; await LoadAsync();
    }

    [RelayCommand] private void CancelEdit() => ShowEditor = false;

    [RelayCommand]
    private async Task DeleteAsync(Supplier? s)
    {
        if (s == null) return;
        if (!_dialog.Confirm($"هل أنت متأكد من حذف المورد '{s.Name}'؟")) return;
        await _supplierService.DeleteAsync(s.Id);
        await LoadAsync();
    }
}

public partial class PurchasesViewModel : ObservableObject
{
    private readonly PurchaseService _purchaseService;
    private readonly SupplierService _supplierService;
    private readonly ProductService _productService;

    [ObservableProperty] private ObservableCollection<Purchase> _purchases = [];
    [ObservableProperty] private ObservableCollection<Supplier> _suppliers = [];
    [ObservableProperty] private ObservableCollection<Product> _products = [];
    [ObservableProperty] private ObservableCollection<PurchaseItem> _newItems = [];
    [ObservableProperty] private Purchase? _selectedPurchase;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private Supplier? _selectedSupplier;
    [ObservableProperty] private string _invoiceNumber = "";
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private string _validationMessage = "";

    public PurchasesViewModel(PurchaseService ps, SupplierService ss, ProductService prs) { _purchaseService = ps; _supplierService = ss; _productService = prs; }

    [RelayCommand] private async Task LoadAsync() { IsLoading = true; Purchases = new ObservableCollection<Purchase>(await _purchaseService.GetAllAsync()); Suppliers = new ObservableCollection<Supplier>(await _supplierService.GetAllAsync()); Products = new ObservableCollection<Product>(await _productService.GetAllAsync()); IsLoading = false; }

    [RelayCommand]
    private void NewPurchase()
    {
        NewItems = []; TotalAmount = 0; InvoiceNumber = ""; SelectedSupplier = null; ShowEditor = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = "";
        if (SelectedSupplier == null) { ValidationMessage = "اختر المورد"; return; }
        if (string.IsNullOrWhiteSpace(InvoiceNumber)) { ValidationMessage = "رقم الفاتورة مطلوب"; return; }
        var p = new Purchase { InvoiceNumber = InvoiceNumber, SupplierId = SelectedSupplier.Id, TotalAmount = TotalAmount, Status = "unpaid", CreatedAt = DateTime.Now };
        await _purchaseService.AddAsync(p);
        ShowEditor = false; await LoadAsync();
    }

    [RelayCommand] private void CancelEdit() => ShowEditor = false;
}

public partial class PharmaciesViewModel : ObservableObject
{
    private readonly PharmacyService _pharmacyService;
    private readonly IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<Pharmacy> _pharmacies = [];
    [ObservableProperty] private Pharmacy? _selectedPharmacy;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private Pharmacy _editPharmacy = new();
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _validationMessage = "";

    public PharmaciesViewModel(PharmacyService pharmacyService, IDialogService dialog) { _pharmacyService = pharmacyService; _dialog = dialog; }

    [RelayCommand] private async Task LoadAsync() { IsLoading = true; Pharmacies = new ObservableCollection<Pharmacy>(await _pharmacyService.GetAllAsync(SearchText)); IsLoading = false; }

    [RelayCommand] private void NewPharmacy() { EditPharmacy = new Pharmacy { AccountStatus = "pending" }; ShowEditor = true; }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedPharmacy == null) return;
        EditPharmacy = new Pharmacy { Id = SelectedPharmacy.Id, Name = SelectedPharmacy.Name, Address = SelectedPharmacy.Address, Phone = SelectedPharmacy.Phone, Balance = SelectedPharmacy.Balance, AccountStatus = SelectedPharmacy.AccountStatus };
        ShowEditor = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = "";
        if (string.IsNullOrWhiteSpace(EditPharmacy.Name)) { ValidationMessage = "اسم الصيدلية مطلوب"; return; }
        if (EditPharmacy.Balance < 0) { ValidationMessage = "الرصيد لا يمكن أن يكون سالباً"; return; }
        if (EditPharmacy.Id == 0) await _pharmacyService.AddAsync(EditPharmacy);
        else await _pharmacyService.UpdateAsync(EditPharmacy);
        ShowEditor = false; await LoadAsync();
    }

    [RelayCommand] private void CancelEdit() => ShowEditor = false;

    [RelayCommand]
    private async Task SetStatusAsync(string? status)
    {
        if (SelectedPharmacy == null || status == null) return;
        if (status == "blocked" && !_dialog.Confirm($"هل تريد حظر الصيدلية '{SelectedPharmacy.Name}'؟")) return;
        if (status == "active" && !_dialog.Confirm($"هل تريد تفعيل الصيدلية '{SelectedPharmacy.Name}'؟")) return;
        await _pharmacyService.SetStatusAsync(SelectedPharmacy.Id, status);
        await LoadAsync();
    }
}
