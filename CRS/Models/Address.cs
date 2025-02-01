using System.ComponentModel.DataAnnotations;

namespace CRS.Models {
    public class Address : BaseModel {
        public required string Street { get; set; }
        public required string City { get; set; }
        public required string State { get; set; }
        [DataType(DataType.PostalCode)] public required string Zip { get; set; }

        public string FullAddress {
            get {
                return $"{Street}, {City}, {State} {Zip}";
            }
        }
    }
}
