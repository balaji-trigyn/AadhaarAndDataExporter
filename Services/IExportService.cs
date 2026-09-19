namespace AadhaarAndDataExporter.Services
{
    public interface IExportService
    {
        Task ExportLargeTableAsync(string outputPath);
        Task ExportDecryptedAadhaarAsync(string outputPath);
        Task ExportMissedTableAsync(string outputPath);
        Task ExportTableAsync(string tableName, string outputPath);
    }
}
