using Npgsql;

namespace ChatRumi.Infrastructure;

public static class DbInitializer
{
    public static void Initialize(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var targetDb = builder.Database;
 
        builder.Database = "postgres";

        using var conn = new NpgsqlConnection(builder.ConnectionString);
        conn.Open();
 
        using var cmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", conn);
        cmd.Parameters.AddWithValue("name", targetDb!);
        var exists = cmd.ExecuteScalar();

        if (exists != null) return;
        using var create = new NpgsqlCommand($"CREATE DATABASE \"{targetDb}\"", conn);
        create.ExecuteNonQuery();
    }
}