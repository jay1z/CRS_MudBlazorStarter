namespace CRS.Models {
    public class ContactXContactGroup : BaseModel {
        public required Contact Contact { get; set; }
        public required ContactGroup ContactGroup { get; set; }
    }
}
