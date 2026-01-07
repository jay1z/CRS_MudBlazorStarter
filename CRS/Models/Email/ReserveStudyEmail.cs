namespace CRS.Models.Emails {
    public class ReserveStudyEmail {
        public required ReserveStudy ReserveStudy { get; set; }
        public string? AdditionalMessage { get; set; }
        public required string BaseUrl { get; set; } = string.Empty;
        public DateTime? SiteVisitDate { get; set; }
    }
}
