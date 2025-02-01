namespace CRS.Models {
    public interface IContact {
        string FirstName { get; set; }
        string LastName { get; set; }
        string? CompanyName { get; set; }
        string Email { get; set; }
        string Phone { get; set; }
        string? Extension { get; set; }
        string FullName { get; }
        string FullNameInverted { get; }
    }
}
