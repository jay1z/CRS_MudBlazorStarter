using CRS.Data;

using Microsoft.AspNetCore.Identity;

namespace CRS.Helpers {
    public interface IPasswordHelper {
        string HashPassword(ApplicationUser user, string password);
        bool VerifyPassword(ApplicationUser user, string hashedPassword, string password);
        string GeneratePassword(ApplicationUser user, string password);
    }

    public class PasswordHelper(IPasswordHasher<ApplicationUser> passwordHasher) : IPasswordHelper {
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher = passwordHasher;

        public string GeneratePassword(ApplicationUser user, string password) {
            return _passwordHasher.HashPassword(user, password);
        }

        public string HashPassword(ApplicationUser user, string password) {
            throw new NotImplementedException();
        }

        public bool VerifyPassword(ApplicationUser user, string hashedPassword, string password) {
            var result = _passwordHasher.VerifyHashedPassword(user, hashedPassword, password);
            // if required, you can handle if result is SuccessRehashNeeded
            return result == PasswordVerificationResult.Success;
        }
    }
}
