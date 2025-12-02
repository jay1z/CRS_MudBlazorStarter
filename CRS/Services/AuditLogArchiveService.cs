using CRS.Data;
using CRS.Models;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services {
    /// <summary>
    /// Background service that archives old audit logs to prevent unbounded table growth.
    /// Runs monthly to move logs older than retention period to archive table.
    /// </summary>
    public class AuditLogArchiveService : BackgroundService {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditLogArchiveService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromDays(1); // Check daily
        private const int RetentionDays = 90; // Keep audit logs for 90 days before archiving

        public AuditLogArchiveService(IServiceScopeFactory scopeFactory, ILogger<AuditLogArchiveService> logger) {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.LogInformation("Audit Log Archive Service starting...");

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    await ArchiveOldLogsAsync(stoppingToken);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Error occurred during audit log archiving");
                }

                // Wait for next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Audit Log Archive Service stopping...");
        }

        private async Task ArchiveOldLogsAsync(CancellationToken cancellationToken) {
            using var scope = _scopeFactory.CreateScope();
            await using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(-RetentionDays);

            try {
                // Find audit logs older than retention period
                var oldLogs = await context.AuditLogs
                    .Where(log => log.CreatedAt < cutoffDate)
                    .Take(1000) // Process in batches to avoid memory issues
                    .ToListAsync(cancellationToken);

                if (!oldLogs.Any()) {
                    _logger.LogDebug("No audit logs to archive");
                    return;
                }

                _logger.LogInformation("Archiving {Count} audit logs older than {CutoffDate}", oldLogs.Count, cutoffDate);

                // Create archive records
                var archiveRecords = oldLogs.Select(log => new AuditLogArchive {
                    ApplicationUserId = log.ApplicationUserId,
                    Action = log.Action,
                    TableName = log.TableName,
                    RecordId = log.RecordId,
                    ColumnName = log.ColumnName,
                    OldValue = log.OldValue,
                    NewValue = log.NewValue,
                    ActorId = log.ActorId,
                    ActorName = log.ActorName,
                    CorrelationId = log.CorrelationId,
                    Method = log.Method,
                    Path = log.Path,
                    RemoteIp = log.RemoteIp,
                    CreatedAt = log.CreatedAt,
                    Payload = log.Payload,
                    ArchivedAt = DateTime.UtcNow,
                    ArchiveReason = $"Retention policy ({RetentionDays} days)"
                }).ToList();

                // Add to archive
                await context.AuditLogArchives.AddRangeAsync(archiveRecords, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                // Remove from main table
                context.AuditLogs.RemoveRange(oldLogs);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully archived {Count} audit logs", oldLogs.Count);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to archive audit logs");
                throw;
            }
        }
    }
}
