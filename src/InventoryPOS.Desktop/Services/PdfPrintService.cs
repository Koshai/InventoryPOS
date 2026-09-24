using InventoryPOS.Application.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace InventoryPOS.Desktop.Services;

public sealed class PdfPrintService : IPdfPrintService
{
    public async Task PrintAsync(
        byte[] pdfBytes,
        string documentName)
    {
        if (pdfBytes is null ||
            pdfBytes.Length == 0)
        {
            throw new ArgumentException(
                "The PDF document is empty.",
                nameof(pdfBytes));
        }

        var safeDocumentName =
            MakeSafeFileName(documentName);

        var file =
            await ApplicationData.Current
                .TemporaryFolder
                .CreateFileAsync(
                    $"{safeDocumentName}.pdf",
                    CreationCollisionOption.ReplaceExisting);

        await FileIO.WriteBytesAsync(
            file,
            pdfBytes);

        var options =
            new LauncherOptions
            {
                DisplayApplicationPicker = false
            };

        var launched =
            await Launcher.LaunchFileAsync(
                file,
                options);

        if (!launched)
        {
            throw new InvalidOperationException(
                "Windows could not open the invoice PDF.");
        }
    }

    private static string MakeSafeFileName(
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "Invoice";

        foreach (var invalidCharacter
                 in Path.GetInvalidFileNameChars())
        {
            fileName =
                fileName.Replace(
                    invalidCharacter,
                    '-');
        }

        return fileName;
    }
}