using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace AadhaarAndDataExporter.Helpers
{
    public static class CsvStreamWriter
    {
        public static async Task StreamDataReaderToCsvAsync(IDataReader reader, string destinationPath)
        {
            using var streamWriter = new StreamWriter(destinationPath, false, Encoding.UTF8, bufferSize: 65536);

            // Write CSV Headers
            var columnCount = reader.FieldCount;
            for (int i = 0; i < columnCount; i++)
            {
                await streamWriter.WriteAsync(EscapeCsvField(reader.GetName(i)));
                if (i < columnCount - 1) await streamWriter.WriteAsync(",");
            }
            await streamWriter.WriteLineAsync();

            // Stream Data Rows Line-by-Line
            while (reader.Read())
            {
                for (int i = 0; i < columnCount; i++)
                {
                    var val = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i).ToString();
                    await streamWriter.WriteAsync(EscapeCsvField(val));
                    if (i < columnCount - 1) await streamWriter.WriteAsync(",");
                }
                await streamWriter.WriteLineAsync();
            }

            await streamWriter.FlushAsync();
        }

        private static string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }
}
