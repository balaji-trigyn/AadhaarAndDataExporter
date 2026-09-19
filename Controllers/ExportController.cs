using AadhaarAndDataExporter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace AadhaarAndDataExporter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExportController : ControllerBase
    {
        private readonly IExportService _exportService;

        public ExportController(IExportService exportService)
        {
            _exportService = exportService;
        }

        /// <summary>
        /// Export Large Dataset with dynamic filename parameter
        /// </summary>
        [HttpGet("large-dataset")]
        public async Task<IActionResult> DownloadLargeDataset([FromQuery] string? customFileName)
        {
            var fileName = !string.IsNullOrWhiteSpace(customFileName) ? customFileName : $"LargeTable_{Guid.NewGuid()}.csv";
            var tempPath = Path.Combine(Path.GetTempPath(), fileName);

            await _exportService.ExportLargeTableAsync(tempPath);

            var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
            return File(stream, "text/csv", fileName);
        }

        /// <summary>
        /// Export Decrypted Data with query parameter
        /// </summary>
        [HttpGet("decrypted-identity")]
        public async Task<IActionResult> DownloadDecryptedIdentity([FromQuery] string? outputName)
        {
            var fileName = !string.IsNullOrWhiteSpace(outputName) ? outputName : $"Decrypted_Export_{Guid.NewGuid()}.csv";
            var tempPath = Path.Combine(Path.GetTempPath(), fileName);

            await _exportService.ExportDecryptedAadhaarAsync(tempPath);

            var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
            return File(stream, "text/csv", fileName);
        }

        /// <summary>
        /// Dynamically export any requested table into a CSV file named <tableName>.csv
        /// </summary>
        [HttpGet("table-direct")]
        public async Task<IActionResult> ExportTableDirect([FromQuery] string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return BadRequest("Table name is required.");
            }

            var fileName = $"{tableName}.csv";
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{fileName}");

            try
            {
                await _exportService.ExportTableAsync(tableName, tempPath);

                var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
                return File(stream, "text/csv", fileName);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

