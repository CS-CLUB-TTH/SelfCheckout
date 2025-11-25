using Microsoft.Data.SqlClient;
using Dapper;
using SelfCheckoutKiosk.Models;

namespace SelfCheckoutKiosk.Services;

public interface IDatabaseService
{
    Task<int?> GetCustomerKeyByCardNo(string cardNo);
    Task<List<BillDetail>> GetBillDetailsByCustomerKey(int cusKey, int workstationId = 0);
    Task<int?> GetBillHdrKeyByCustomerKey(int cusKey);
    Task<PaymentCompletionResult> CompletePaymentAsync(
        int billHdrKey,
        int paymentTypeId = 2,
        int? creditCardTypeId = null,
        string? authorizationCode = null,
        string? referenceNumber = null);
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

        DefaultTypeMap.MatchNamesWithUnderscores = true;
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

    public async Task<int?> GetBillHdrKeyByCustomerKey(int cusKey)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get the most recent open bill for this customer
            var query = @"
                SELECT TOP 1 bh.bill_hdr_key 
                FROM trn_bill_header bh
                WHERE bh.customer_key = @CusKey 
                  AND bh.bill_end_datetime IS NULL
                ORDER BY bh.bill_start_datetime DESC";
            
            var billHdrKey = await connection.QueryFirstOrDefaultAsync<int?>(query, new { CusKey = cusKey });

            _logger.LogInformation("Retrieved bill header key {BillHdrKey} for customer key {CusKey}", billHdrKey, cusKey);
            return billHdrKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bill header key for customer key {CusKey}", cusKey);
            throw;
        }
    }

    public async Task<PaymentCompletionResult> CompletePaymentAsync(
        int billHdrKey,
        int paymentTypeId = 2,
        int? creditCardTypeId = null,
        string? authorizationCode = null,
        string? referenceNumber = null)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new
            {
                bill_hdr_key = billHdrKey,
                payment_type_id = paymentTypeId,
                credit_card_type_id = creditCardTypeId,
                authorization_code = authorizationCode,
                reference_number = referenceNumber
            };

            var result = await connection.QueryFirstOrDefaultAsync<PaymentCompletionResult>(
                "dbo.p_complete_selfcheckout_payment",
                parameters,
                commandType: System.Data.CommandType.StoredProcedure
            );

            if (result == null)
            {
                _logger.LogWarning("No result returned from payment completion for bill {BillHdrKey}", billHdrKey);
                return new PaymentCompletionResult
                {
                    BillHdrKey = billHdrKey,
                    Status = "ERROR",
                    Message = "No result returned from stored procedure"
                };
            }

            _logger.LogInformation(
                "Payment completion {Status} for bill {BillHdrKey}: {Message}", 
                result.Status, billHdrKey, result.Message);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing payment for bill {BillHdrKey}", billHdrKey);
            return new PaymentCompletionResult
            {
                BillHdrKey = billHdrKey,
                Status = "ERROR",
                Message = ex.Message
            };
        }
    }
}
