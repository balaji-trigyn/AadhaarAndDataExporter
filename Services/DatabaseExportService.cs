using AadhaarAndDataExporter.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace AadhaarAndDataExporter.Services
{
    public class DatabaseExportService : IExportService
    {
        private readonly IConfiguration _config;
        private readonly IAadhaarDecryptor _decryptor;
        private readonly ILogger<DatabaseExportService> _logger;

        public DatabaseExportService(IConfiguration config, IAadhaarDecryptor decryptor, ILogger<DatabaseExportService> logger)
        {
            _config = config;
            _decryptor = decryptor;
            _logger = logger;
        }
        public async Task ExportLargeTableAsync(string outputPath)
        {
            _logger.LogInformation("Starting streaming export for 1.3M dataset...");

            var connStr = _config.GetConnectionString("SourceDb");
            var query = "SELECT * FROM LargeTable WITH (NOLOCK)";

            using var connection = new SqlConnection(connStr);
            using var command = new SqlCommand(query, connection) { CommandTimeout = 600 };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            await CsvStreamWriter.StreamDataReaderToCsvAsync(reader, outputPath);
            _logger.LogInformation("Large table export finished: {Path}", outputPath);
        }
        public async Task ExportDecryptedAadhaarAsync(string outputPath)
        {
            _logger.LogInformation("Starting batch decryption export...");

            var connStr = _config.GetConnectionString("IdentityDb");
            var query = "SELECT REQUISITION_ID, CITIZEN_UID FROM REQUISITION_DETAILS WITH (NOLOCK)";

            using var connection = new SqlConnection(connStr);
            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            using var writer = new StreamWriter(outputPath, false, Encoding.UTF8, bufferSize: 65536);
            await writer.WriteLineAsync("REQUISITION_ID,DecryptedNumber");

            const int chunkSize = 1000;
            var idBatch = new List<string>();
            var encryptedBatch = new List<string>();

            while (await reader.ReadAsync())
            {
                idBatch.Add(reader["REQUISITION_ID"]?.ToString() ?? string.Empty);
                encryptedBatch.Add(reader["CITIZEN_UID"]?.ToString() ?? string.Empty);

                if (encryptedBatch.Count >= chunkSize)
                {
                    await ProcessAndWriteBatchAsync(writer, idBatch, encryptedBatch);
                    idBatch.Clear();
                    encryptedBatch.Clear();
                }
            }

            if (encryptedBatch.Count > 0)
            {
                await ProcessAndWriteBatchAsync(writer, idBatch, encryptedBatch);
            }

            await writer.FlushAsync();
            _logger.LogInformation("Decryption export finished: {Path}", outputPath);
        }

        private async Task ProcessAndWriteBatchAsync(StreamWriter writer, List<string> ids, List<string> encryptedList)
        {
            List<string> decryptedList = _decryptor.DecryptBatch(encryptedList);

            for (int i = 0; i < decryptedList.Count; i++)
            {
                await writer.WriteLineAsync($"\"{ids[i]}\",\"{decryptedList[i]}\"");
            }
        }
        public async Task ExportMissedTableAsync(string outputPath)
        {
            _logger.LogInformation("Starting export for missed table...");

            var connStr = _config.GetConnectionString("SourceDb");
            var query = "SELECT * FROM Pension_Telugu_Names WITH (NOLOCK)";

            using var connection = new SqlConnection(connStr);
            using var command = new SqlCommand(query, connection) { CommandTimeout = 300 };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            await CsvStreamWriter.StreamDataReaderToCsvAsync(reader, outputPath);
            _logger.LogInformation("Missed table export finished: {Path}", outputPath);
        }

        public async Task ExportTableAsync(string tableName, string outputPath)
        {
            string sanitizedTableName = SanitizeTableName(tableName);
            _logger.LogInformation("Starting dynamic export for table {TableName}...", sanitizedTableName);

            var connStr = _config.GetConnectionString("SourceDb");
            var query = $"SELECT * FROM {sanitizedTableName} WITH (NOLOCK)";

            using var connection = new SqlConnection(connStr);
            using var command = new SqlCommand(query, connection) { CommandTimeout = 600 };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            await CsvStreamWriter.StreamDataReaderToCsvAsync(reader, outputPath);
            _logger.LogInformation("Dynamic export for table {TableName} finished: {Path}", sanitizedTableName, outputPath);
        }

        private static string SanitizeTableName(string tableName)
        {
            var cleanName = Regex.Replace(tableName, @"[^\w\.]", "");
            if (string.IsNullOrWhiteSpace(cleanName))
            {
                throw new ArgumentException("Invalid table name provided.");
            }
            return cleanName;
        }
    }
}
