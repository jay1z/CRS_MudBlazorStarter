using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using Serilog;
using Horizon.Data;
using Horizon.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Horizon.Middleware {
 public class RequestAuditMiddleware {
 private readonly RequestDelegate _next;

 public RequestAuditMiddleware(RequestDelegate next) {
 _next = next;
 }

 public async Task InvokeAsync(HttpContext context, IDbContextFactory<ApplicationDbContext> dbFactory) {
 // Ensure correlation id
 var correlationId = context.Request.Headers.ContainsKey("X-Correlation-Id")
 ? context.Request.Headers["X-Correlation-Id"].ToString()
 : Guid.NewGuid().ToString();

 context.Response.Headers["X-Correlation-Id"] = correlationId;

 var user = context.User;
 var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 var userName = user?.Identity?.Name;
 var remoteIp = context.Connection.RemoteIpAddress?.ToString();

 using (LogContext.PushProperty("CorrelationId", correlationId))
 using (LogContext.PushProperty("UserId", userId ?? "anonymous"))
 using (LogContext.PushProperty("UserName", userName ?? string.Empty))
 using (LogContext.PushProperty("RemoteIp", remoteIp ?? string.Empty)) {
 try {
 // For non-GET methods, capture a lightweight audit record asynchronously
 if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method)) {
 // Read request body (safely)
 string body = string.Empty;
 context.Request.EnableBuffering();
 try {
 using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
 body = await reader.ReadToEndAsync();
 context.Request.Body.Position =0;
 } catch { /* ignore body read errors */ }

 // Create audit log entry (best-effort)
 try {
 await using var db = await dbFactory.CreateDbContextAsync();
 var audit = new AuditLog {
 ActorId = userId,
 ActorName = userName,
 CorrelationId = correlationId,
 Method = context.Request.Method,
 Path = context.Request.Path,
 RemoteIp = remoteIp,
 CreatedAt = DateTime.UtcNow,
 Payload = string.IsNullOrWhiteSpace(body) ? null : (body.Length >4000 ? body.Substring(0,4000) : body)
 };
 db.AuditLogs.Add(audit);
 await db.SaveChangesAsync();
 } catch (Exception ex) {
 // Swallow DB audit errors but log them to Serilog
 Log.Warning(ex, "Failed to save audit log");
 }
 }

 await _next(context);
 } catch (Exception ex) {
 Log.Error(ex, "Unhandled exception in request pipeline");
 throw;
 }
 }
 }
 }
}
