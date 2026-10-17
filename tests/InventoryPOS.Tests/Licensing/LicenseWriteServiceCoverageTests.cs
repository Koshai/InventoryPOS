using InventoryPOS.Application.Licensing;
using InventoryPOS.Infrastructure.Services;

namespace InventoryPOS.Tests.Licensing;

/// <summary>
/// Prevent newly introduced constructors from silently dropping the licensing dependency.
/// Functional tests must still exercise each write operation against an expired license.
/// </summary>
public sealed class LicenseWriteServiceCoverageTests
{
    public static IEnumerable<object[]> ProtectedServices()
    {
        yield return new object[] { typeof(BusinessProfileService) };
        yield return new object[] { typeof(CustomerService) };
        yield return new object[] { typeof(SupplierService) };
        yield return new object[] { typeof(ProductService) };
        yield return new object[] { typeof(ProductImageService) };
        yield return new object[] { typeof(ProductBarcodeService) };
        yield return new object[] { typeof(PurchaseReturnService) };
        yield return new object[] { typeof(SalesReturnService) };
        yield return new object[] { typeof(SalesQuoteService) };
        yield return new object[] { typeof(WarrantyClaimService) };
        yield return new object[] { typeof(WarrantyClaimManagementService) };
        yield return new object[] { typeof(WarrantyReplacementService) };
        yield return new object[] { typeof(DocumentIdentifierService) };
    }

    [Theory]
    [MemberData(nameof(ProtectedServices))]
    public void MutatingService_RequiresLicenseAccessGuard(Type serviceType)
    {
        Assert.Contains(serviceType.GetConstructors(), constructor =>
            constructor.GetParameters().Any(parameter =>
                parameter.ParameterType == typeof(ILicenseAccessGuard)));
    }
}
