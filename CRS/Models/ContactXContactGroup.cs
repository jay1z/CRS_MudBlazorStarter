namespace CRS.Models {
    public class ContactXContactGroup : BaseModel {
        public Contact Contact { get; set; }
        public ContactGroup ContactGroup { get; set; }
    }
}
