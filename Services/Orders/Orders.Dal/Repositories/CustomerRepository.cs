using System.Data;
using Microsoft.Data.SqlClient;
using Orders.Dal.Errors;
using Orders.Dal.Interfaces;
using Orders.Domain.Models;

namespace Orders.Dal.Repositories;

public sealed class CustomerRepository : RepositoryBase, ICustomerRepository
{
    public CustomerRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction) { }

    public async Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql =
            "SELECT Id, Email, FullName, Phone FROM dbo.Customers WHERE Id = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql =
            "SELECT Id, Email, FullName, Phone FROM dbo.Customers WHERE Email = @Email AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = email;

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql =
            "SELECT Id, Email, FullName, Phone FROM dbo.Customers WHERE IsDeleted = 0 ORDER BY Id";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);

        var result = new List<Customer>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            result.Add(Map(reader));
        return result;
    }

    public async Task<long> AddAsync(Customer customer, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.Customers (Email, FullName, Phone)
            OUTPUT INSERTED.Id
            VALUES (@Email, @FullName, @Phone)";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = customer.Email;
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 200).Value = customer.FullName;
        command.Parameters.Add("@Phone", SqlDbType.NVarChar, 30).Value = (object?)customer.Phone ?? DBNull.Value;

        try
        {
            var id = await command.ExecuteScalarAsync(ct);
            return Convert.ToInt64(id);
        }
        catch (SqlException ex) when (SqlErrorTranslator.Translate(ex) is not null)
        {
            throw SqlErrorTranslator.Translate(ex)!;
        }
    }

    public async Task<bool> UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.Customers
            SET Email = @Email, FullName = @FullName, Phone = @Phone, UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = customer.Id;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = customer.Email;
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 200).Value = customer.FullName;
        command.Parameters.Add("@Phone", SqlDbType.NVarChar, 30).Value = (object?)customer.Phone ?? DBNull.Value;

        try
        {
            return await command.ExecuteNonQueryAsync(ct) > 0;
        }
        catch (SqlException ex) when (SqlErrorTranslator.Translate(ex) is not null)
        {
            throw SqlErrorTranslator.Translate(ex)!;
        }
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.Customers
            SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    private static Customer Map(SqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Email = reader.GetString(1),
        FullName = reader.GetString(2),
        Phone = reader.IsDBNull(3) ? null : reader.GetString(3)
    };
}