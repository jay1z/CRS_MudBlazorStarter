using System;
using System.Threading.Tasks;
using Horizon.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Horizon.Services
{
    /// <summary>
    /// Service for handling database retries with exponential backoff
    /// for transient errors like deadlocks or connection issues
    /// </summary>
    public class DbRetryService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ILogger<DbRetryService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public DbRetryService(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            ILogger<DbRetryService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
            
            // Define a retry policy with exponential backoff
            _retryPolicy = Policy
                .Handle<DbUpdateException>()
                .Or<DbUpdateConcurrencyException>()
                .Or<Microsoft.Data.SqlClient.SqlException>(ex => ex.Number == 1205) // SQL Server deadlock
                .WaitAndRetryAsync(
                    retryCount: 3, 
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception, 
                            "Retry {RetryCount} after {RetrySeconds}s delay due to: {Message}", 
                            retryCount, 
                            timeSpan.TotalSeconds,
                            exception.Message);
                    });
        }

        /// <summary>
        /// Executes the provided operation within a transaction with retry policy
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">The database operation to execute</param>
        /// <returns>The result of the operation</returns>
        public async Task<T> ExecuteInRetryableTransactionAsync<T>(Func<ApplicationDbContext, Task<T>> operation)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                using var dbContext = await _dbFactory.CreateDbContextAsync();
                using var transaction = await dbContext.Database.BeginTransactionAsync();
                
                try
                {
                    var result = await operation(dbContext);
                    await transaction.CommitAsync();
                    return result;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        /// <summary>
        /// Executes a database operation with retries but without transaction
        /// Useful for read-only operations
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">The database operation to execute</param>
        /// <returns>The result of the operation</returns>
        public async Task<T> ExecuteWithRetryAsync<T>(Func<ApplicationDbContext, Task<T>> operation)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                using var dbContext = await _dbFactory.CreateDbContextAsync();
                return await operation(dbContext);
            });
        }
    }
}
