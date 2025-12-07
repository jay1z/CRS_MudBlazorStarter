using System.ComponentModel.DataAnnotations;

namespace CRS.Models {
    /// <summary>
    /// Represents a physical address. Addresses are stored in a normalized table
    /// and referenced via FK pointers from entities like Community.
    /// </summary>
    public class Address : BaseModel {
        public string? Street { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        [DataType(DataType.PostalCode)] 
        public string? Zip { get; set; }

        public string FullAddress => $"{Street}, {City}, {State} {Zip}";
    }
}
