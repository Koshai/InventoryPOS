
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


if ((args.Length != 2 && args.Length != 3) ||
    args[0] != "--database" ||
    args[1] != "inventorypos_test" ||
    (args.Length == 3 && args[2] != "--apply"))
{
    Console.Error.WriteLine(
        "Usage: --database inventorypos_test [--apply]");

    return 1;
}


var connectionString =
    Environment.GetEnvironmentVariable(
        "INVENTORYPOS_TEST_CONNECTION");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        "INVENTORYPOS_TEST_CONNECTION is missing.");

    return 1;
}

var builder =
    new Npgsql.NpgsqlConnectionStringBuilder(connectionString);

if (!string.Equals(
        builder.Database,
        "inventorypos_test",
        StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine(
        "Refusing to migrate a non-test database.");

    return 1;
}

Console.WriteLine(
    $"Target: {builder.Host}:{builder.Port}/{builder.Database}");

var options =
    new DbContextOptionsBuilder<InventoryPosDbContext>()
        .UseNpgsql(connectionString)
        .Options;

await using var db = new InventoryPosDbContext(options);

Console.WriteLine("Checking database connection...");
if (!await db.Database.CanConnectAsync())
{
    Console.Error.WriteLine("Cannot connect to test database.");
    return 1;
}

var pending = (await db.Database
    .GetPendingMigrationsAsync()).ToList();

Console.WriteLine($"Pending migrations: {pending.Count}");

foreach (var migration in pending)
{
    Console.WriteLine($"  {migration}");
}


if (pending.Count == 0)
{
    Console.WriteLine("Database is up to date.");
    return 0;
}

var applyRequested = args.Length == 3 &&
    args[2] == "--apply";

if (!applyRequested)
{
    Console.WriteLine(
        "Read-only check complete. Use --apply to run migrations.");
    return 0;
}

Console.WriteLine();
Console.WriteLine(
    "WARNING: This will modify the inventorypos_test schema.");
Console.Write("Type APPLY to continue: ");

var confirmation = Console.ReadLine();

if (confirmation != "APPLY")
{
    Console.WriteLine("Cancelled. No migrations applied.");
    return 1;
}

Console.WriteLine("Applying migrations...");

await db.Database.MigrateAsync();

Console.WriteLine("Migrations applied successfully.");

var remaining = await db.Database
    .GetPendingMigrationsAsync();

Console.WriteLine(
    $"Remaining pending migrations: {remaining.Count()}");

return 0;


return 0;
