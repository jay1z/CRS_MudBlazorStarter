using System.ComponentModel.DataAnnotations;

namespace CRS.Models {
    public class Address : BaseModel {
        public string? Street { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        [DataType(DataType.PostalCode)] public string? Zip { get; set; }

        /// <summary>
        /// Indicates if this address is the mailing/correspondence address.
        /// Only ONE address per community should have this set to true.
        /// Physical addresses should have this set to false.
        /// </summary>
        public bool IsMailingAddress { get; set; }

        /// <summary>
        /// Type of address: Physical (homes/buildings), Mailing (office/correspondence), etc.
        /// </summary>
        public AddressType AddressType { get; set; } = AddressType.Physical;

        public string FullAddress {
            get {
                return $"{Street}, {City}, {State} {Zip}";
            }
        }
    }

    /// <summary>
    /// Defines the type/purpose of an address.
    /// A Community can have multiple Physical addresses (homes/buildings),
    /// but only ONE Mailing address (main office).
    /// </summary>
    public enum AddressType
    {
        /// <summary>
        /// Physical location address (home/building). Can have multiple per community.
        /// </summary>
        Physical = 0,

        /// <summary>
        /// Mailing/correspondence address (main office/PO Box). Only ONE per community.
        /// </summary>
        Mailing = 1,

        /// <summary>
        /// Billing address (future use if different from mailing).
        /// </summary>
        Billing = 2
    }
}
