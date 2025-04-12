namespace CRS.Models {
    public class Community : BaseModel {
        public Community() {
            Addresses = new List<Address>();
            IsActive = true;
        }
        public string? Name { get; set; }

        public DateTime? AnnualMeetingDate { get; set; }

        public List<Address>? Addresses { get; set; } = [];

        public bool IsActive { get; set; }
    }
}
