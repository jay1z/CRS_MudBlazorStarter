using System.ComponentModel.DataAnnotations;

namespace CRS.Models {
    public class Address : BaseModel {
        public  string? Street { get; set; }

        public  string? City { get; set; }

        public  string? State { get; set; }

        [DataType(DataType.PostalCode)] public string? Zip { get; set; }

        public string FullAddress {
            get {
                return $"{Street}, {City}, {State} {Zip}";
            }
        }
    }
}
