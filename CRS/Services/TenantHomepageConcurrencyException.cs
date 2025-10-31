using System;

namespace CRS.Services {
 public class TenantHomepageConcurrencyException : Exception {
 public TenantHomepageConcurrencyException() { }
 public TenantHomepageConcurrencyException(string message) : base(message) { }
 public TenantHomepageConcurrencyException(string message, Exception inner) : base(message, inner) { }
 }
}
