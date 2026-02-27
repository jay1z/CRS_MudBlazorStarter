using Horizon.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Horizon.Models {
    public interface IContact {
        [ForeignKey(nameof(ApplicationUser))] public Guid? ApplicationUserId { get; set; }
        public ApplicationUser? User { get; set; }

        string? FirstName { get; set; }
        string? LastName { get; set; }
        string? CompanyName { get; set; }
        string? Email { get; set; }
        string? Phone { get; set; }
        string? Extension { get; set; }
        string FullName { get; }
        string FullNameInverted { get; }
    }
}
