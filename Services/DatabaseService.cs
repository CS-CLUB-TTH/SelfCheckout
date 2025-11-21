using Microsoft.Data.SqlClient;
using Dapper;
using SelfCheckoutKiosk.Models;

namespace SelfCheckoutKiosk.Services;

public interface IDatabaseService
{
    Task<int?> GetCustomerKeyByCardNo(string cardNo);
    Task<List<BillDetail>> GetBillDetailsByCustomerKey(int cusKey, int workstationId = 0);
}

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Database connection string not configured");
        _logger = logger;
    }

    public async Task<int?> GetCustomerKeyByCardNo(string cardNo)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT TOP 1 cus_key FROM mst_customer_supplier WHERE card_no = @CardNo";
            var cusKey = await connection.QueryFirstOrDefaultAsync<int?>(query, new { CardNo = cardNo });

            _logger.LogInformation("Retrieved customer key {CusKey} for card {CardNo}", cusKey, cardNo);
            return cusKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer key for card {CardNo}", cardNo);
            throw;
        }
    }

    public async Task<List<BillDetail>> GetBillDetailsByCustomerKey(int cusKey, int workstationId = 0)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new
            {
                cus_key = cusKey,
                ws = workstationId
            };

            var billDetails = await connection.QueryAsync<BillDetail>(
                "dbo.retrieve_bill_details2",
                parameters,
                commandType: System.Data.CommandType.StoredProcedure
            );

            var result = billDetails.ToList();
            _logger.LogInformation("Retrieved {Count} bill details for customer key {CusKey}", result.Count, cusKey);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bill details for customer key {CusKey}", cusKey);
            throw;
        }
    }
}
