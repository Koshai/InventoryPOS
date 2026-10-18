using System.IO;
using Microsoft.Data.Sqlite;

namespace InventoryPOS.LicenseManager.Data;

public sealed class LicenseManagerDb
{
    private readonly string _connectionString;

    public LicenseManagerDb(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();
        Initialize();
    }

    public static string DefaultDatabasePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "InventoryPOS",
            "LicenseManager",
            "licenses.db");

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Customers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Email TEXT NOT NULL COLLATE NOCASE UNIQUE,
                DisplayName TEXT NOT NULL,
                Notes TEXT NOT NULL DEFAULT '',
                CreatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS MachineLicenses (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CustomerId INTEGER NOT NULL,
                InstallationId TEXT NOT NULL,
                Edition TEXT NOT NULL,
                LicenseId TEXT NOT NULL,
                IssuedAtUtc TEXT NOT NULL,
                ValidFromUtc TEXT NOT NULL,
                ExpiresAtUtc TEXT NOT NULL,
                FeaturesJson TEXT NOT NULL,
                LastIssuedFilePath TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE,
                UNIQUE (CustomerId, InstallationId)
            );

            CREATE TABLE IF NOT EXISTS IssueHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MachineLicenseId INTEGER NOT NULL,
                IssuedAtUtc TEXT NOT NULL,
                ExpiresAtUtc TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                Notes TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (MachineLicenseId) REFERENCES MachineLicenses(Id) ON DELETE CASCADE
            );
            """;
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<CustomerRow> SearchCustomers(string? query)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        if (string.IsNullOrWhiteSpace(query))
        {
            command.CommandText = """
                SELECT Id, Email, DisplayName, Notes, CreatedAtUtc
                FROM Customers
                ORDER BY Email COLLATE NOCASE;
                """;
        }
        else
        {
            command.CommandText = """
                SELECT Id, Email, DisplayName, Notes, CreatedAtUtc
                FROM Customers
                WHERE Email LIKE $q OR DisplayName LIKE $q
                ORDER BY Email COLLATE NOCASE;
                """;
            command.Parameters.AddWithValue("$q", "%" + query.Trim() + "%");
        }

        var list = new List<CustomerRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new CustomerRow(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                DateTimeOffset.Parse(reader.GetString(4))));
        }

        return list;
    }

    public CustomerRow UpsertCustomer(string email, string displayName, string? notes = null)
    {
        email = email.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? email : displayName.Trim();
        notes ??= string.Empty;

        using var connection = Open();
        using (var update = connection.CreateCommand())
        {
            update.CommandText = """
                UPDATE Customers
                SET DisplayName = $name, Notes = $notes
                WHERE Email = $email COLLATE NOCASE;
                """;
            update.Parameters.AddWithValue("$name", displayName);
            update.Parameters.AddWithValue("$notes", notes);
            update.Parameters.AddWithValue("$email", email);
            update.ExecuteNonQuery();
        }

        using (var insert = connection.CreateCommand())
        {
            insert.CommandText = """
                INSERT INTO Customers (Email, DisplayName, Notes, CreatedAtUtc)
                SELECT $email, $name, $notes, $created
                WHERE NOT EXISTS (
                    SELECT 1 FROM Customers WHERE Email = $email COLLATE NOCASE);
                """;
            insert.Parameters.AddWithValue("$email", email);
            insert.Parameters.AddWithValue("$name", displayName);
            insert.Parameters.AddWithValue("$notes", notes);
            insert.Parameters.AddWithValue("$created", DateTimeOffset.UtcNow.ToString("O"));
            insert.ExecuteNonQuery();
        }

        return GetCustomerByEmail(email)
            ?? throw new InvalidOperationException("Customer could not be saved.");
    }

    public CustomerRow? GetCustomerByEmail(string email)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Email, DisplayName, Notes, CreatedAtUtc
            FROM Customers
            WHERE Email = $email COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("$email", email.Trim());
        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        return new CustomerRow(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            DateTimeOffset.Parse(reader.GetString(4)));
    }

    public IReadOnlyList<MachineLicenseRow> GetMachines(long customerId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, CustomerId, InstallationId, Edition, LicenseId,
                   IssuedAtUtc, ValidFromUtc, ExpiresAtUtc, FeaturesJson, LastIssuedFilePath
            FROM MachineLicenses
            WHERE CustomerId = $id
            ORDER BY ExpiresAtUtc DESC;
            """;
        command.Parameters.AddWithValue("$id", customerId);

        var list = new List<MachineLicenseRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(ReadMachine(reader));
        }

        return list;
    }

    public MachineLicenseRow SaveIssuedLicense(
        long customerId,
        string installationId,
        string edition,
        Guid licenseId,
        DateTimeOffset issuedAt,
        DateTimeOffset validFrom,
        DateTimeOffset expiresAt,
        string featuresJson,
        string filePath,
        string historyNotes)
    {
        using var connection = Open();
        using var tx = connection.BeginTransaction();

        long machineId;
        using (var upsert = connection.CreateCommand())
        {
            upsert.Transaction = tx;
            upsert.CommandText = """
                INSERT INTO MachineLicenses (
                    CustomerId, InstallationId, Edition, LicenseId,
                    IssuedAtUtc, ValidFromUtc, ExpiresAtUtc, FeaturesJson, LastIssuedFilePath)
                VALUES (
                    $customerId, $installationId, $edition, $licenseId,
                    $issued, $validFrom, $expires, $features, $path)
                ON CONFLICT(CustomerId, InstallationId) DO UPDATE SET
                    Edition = excluded.Edition,
                    LicenseId = excluded.LicenseId,
                    IssuedAtUtc = excluded.IssuedAtUtc,
                    ValidFromUtc = excluded.ValidFromUtc,
                    ExpiresAtUtc = excluded.ExpiresAtUtc,
                    FeaturesJson = excluded.FeaturesJson,
                    LastIssuedFilePath = excluded.LastIssuedFilePath;
                """;
            upsert.Parameters.AddWithValue("$customerId", customerId);
            upsert.Parameters.AddWithValue("$installationId", installationId);
            upsert.Parameters.AddWithValue("$edition", edition);
            upsert.Parameters.AddWithValue("$licenseId", licenseId.ToString("D"));
            upsert.Parameters.AddWithValue("$issued", issuedAt.ToString("O"));
            upsert.Parameters.AddWithValue("$validFrom", validFrom.ToString("O"));
            upsert.Parameters.AddWithValue("$expires", expiresAt.ToString("O"));
            upsert.Parameters.AddWithValue("$features", featuresJson);
            upsert.Parameters.AddWithValue("$path", filePath);
            upsert.ExecuteNonQuery();
        }

        using (var select = connection.CreateCommand())
        {
            select.Transaction = tx;
            select.CommandText = """
                SELECT Id FROM MachineLicenses
                WHERE CustomerId = $customerId AND InstallationId = $installationId;
                """;
            select.Parameters.AddWithValue("$customerId", customerId);
            select.Parameters.AddWithValue("$installationId", installationId);
            machineId = (long)(select.ExecuteScalar()
                ?? throw new InvalidOperationException("Machine license row missing after save."));
        }

        using (var history = connection.CreateCommand())
        {
            history.Transaction = tx;
            history.CommandText = """
                INSERT INTO IssueHistory (MachineLicenseId, IssuedAtUtc, ExpiresAtUtc, FilePath, Notes)
                VALUES ($machineId, $issued, $expires, $path, $notes);
                """;
            history.Parameters.AddWithValue("$machineId", machineId);
            history.Parameters.AddWithValue("$issued", issuedAt.ToString("O"));
            history.Parameters.AddWithValue("$expires", expiresAt.ToString("O"));
            history.Parameters.AddWithValue("$path", filePath);
            history.Parameters.AddWithValue("$notes", historyNotes);
            history.ExecuteNonQuery();
        }

        tx.Commit();

        return GetMachines(customerId).First(x => x.Id == machineId);
    }

    public IReadOnlyList<IssueHistoryRow> GetHistory(long machineLicenseId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, MachineLicenseId, IssuedAtUtc, ExpiresAtUtc, FilePath, Notes
            FROM IssueHistory
            WHERE MachineLicenseId = $id
            ORDER BY IssuedAtUtc DESC;
            """;
        command.Parameters.AddWithValue("$id", machineLicenseId);

        var list = new List<IssueHistoryRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new IssueHistoryRow(
                reader.GetInt64(0),
                reader.GetInt64(1),
                DateTimeOffset.Parse(reader.GetString(2)),
                DateTimeOffset.Parse(reader.GetString(3)),
                reader.GetString(4),
                reader.GetString(5)));
        }

        return list;
    }

    private static MachineLicenseRow ReadMachine(SqliteDataReader reader) =>
        new(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetString(2),
            reader.GetString(3),
            Guid.Parse(reader.GetString(4)),
            DateTimeOffset.Parse(reader.GetString(5)),
            DateTimeOffset.Parse(reader.GetString(6)),
            DateTimeOffset.Parse(reader.GetString(7)),
            reader.GetString(8),
            reader.GetString(9));
}

public sealed record CustomerRow(
    long Id,
    string Email,
    string DisplayName,
    string Notes,
    DateTimeOffset CreatedAtUtc);

public sealed record MachineLicenseRow(
    long Id,
    long CustomerId,
    string InstallationId,
    string Edition,
    Guid LicenseId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset ExpiresAtUtc,
    string FeaturesJson,
    string LastIssuedFilePath);

public sealed record IssueHistoryRow(
    long Id,
    long MachineLicenseId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string FilePath,
    string Notes);
