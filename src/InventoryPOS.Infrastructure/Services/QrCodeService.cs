using InventoryPOS.Application.Interfaces;
using QRCoder;

namespace InventoryPOS.Infrastructure.Services;

public sealed class QrCodeService : IQrCodeService
{
    public byte[] GeneratePng(
        string payload,
        int pixelsPerModule = 10)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException(
                "QR payload is required.",
                nameof(payload));
        }

        if (pixelsPerModule < 1 ||
            pixelsPerModule > 50)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pixelsPerModule),
                "Pixels per module must be between 1 and 50.");
        }

        using var generator =
            new QRCodeGenerator();

        using var qrData =
            generator.CreateQrCode(
                payload.Trim(),
                QRCodeGenerator.ECCLevel.Q);

        var qrCode =
            new PngByteQRCode(qrData);

        return qrCode.GetGraphic(
            pixelsPerModule);
    }
}