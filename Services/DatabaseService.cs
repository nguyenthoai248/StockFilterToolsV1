using System;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

namespace StockFilterToolsV1.Services
{
    public class DatabaseService
    {
        private string connectionString = string.Empty;
        public DatabaseService()
        {
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            connectionString = GetConnectionString();
            using var conn = new SqliteConnection(connectionString);

            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Organizations (Symbol TEXT PRIMARY KEY, ResponseJson TEXT, FetchedAt TEXT);
                CREATE TABLE IF NOT EXISTS BalanceSheets (Symbol TEXT PRIMARY KEY, ResponseJson TEXT, FetchedAt TEXT);
                CREATE TABLE IF NOT EXISTS IncomeStatements (Symbol TEXT PRIMARY KEY, ResponseJson TEXT, FetchedAt TEXT);
            ";
            cmd.ExecuteNonQuery();
        }

        private string GetConnectionString() {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string solutionDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));

            string dbPath = Path.Combine(solutionDir, "Data", "financial_data.db");
            return $"Data Source={dbPath}";
        }

        public async Task SaveJson(string table, string symbol, string json)
        {
            await using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO " + table + @" (Symbol, ResponseJson, FetchedAt)
                VALUES (@symbol, @json, @time)
                ON CONFLICT(Symbol) DO UPDATE SET 
                ResponseJson = excluded.ResponseJson,
                FetchedAt = excluded.FetchedAt;";
            cmd.Parameters.AddWithValue("@symbol", symbol);
            cmd.Parameters.AddWithValue("@json", json);
            cmd.Parameters.AddWithValue("@time", DateTime.UtcNow.ToString("o"));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<DataTable> GetOrganizationTable()
        {
            return await Task.Run(() =>
            {
                var dt = new DataTable();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Symbol, ResponseJson FROM Organizations ORDER BY Symbol";
                using var reader = cmd.ExecuteReader();
                dt.Load(reader);
                return dt;
            });
        }

        public async Task<DataTable> GetBalanceSheetBySymbol(string symbol)
        {
            return await Task.Run(() =>
            {
                var dt = new DataTable();
                using var conn = new SqliteConnection(connectionString);
                conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Symbol, ResponseJson FROM BalanceSheets WHERE Symbol = $symbol ORDER BY Symbol";
                cmd.Parameters.AddWithValue("$symbol", symbol);

                using var reader = cmd.ExecuteReader();
                dt.Load(reader);

                return dt;
            });
        }

        public async Task<DataTable> GetIncomeStatementsBySymbol(string symbol)
        {
            return await Task.Run(() =>
            {
                var dt = new DataTable();
                using var conn = new SqliteConnection(connectionString);
                conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Symbol, ResponseJson FROM IncomeStatements WHERE Symbol = $symbol ORDER BY Symbol";
                cmd.Parameters.AddWithValue("$symbol", symbol);
                using var reader = cmd.ExecuteReader();
                dt.Load(reader);
                return dt;
            });
        }

        public bool IsSymbolRecentlyUpdated(string symbol)
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT FetchedAt FROM BalanceSheets WHERE Symbol = $symbol ORDER BY FetchedAt DESC LIMIT 1";
            cmd.Parameters.AddWithValue("$symbol", symbol);

            var result = cmd.ExecuteScalar();
            if (result == null) return false;

            if (DateTime.TryParse(result.ToString(), out var lastUpdated))
            {
                return (DateTime.UtcNow - lastUpdated).TotalMinutes < 300;
            }

            return false;
        }
    }
}
