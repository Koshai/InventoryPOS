using System.Threading.Tasks;

namespace InventoryPOS.Application.Interfaces;

public interface IPdfPrintService
{
    Task PrintAsync(
        byte[] pdfBytes,
        string documentName);
}