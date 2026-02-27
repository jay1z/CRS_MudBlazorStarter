using System.ComponentModel.DataAnnotations;

namespace Horizon.Models {
    public class AccessToken : BaseModel {
        [Required]
        public Guid Token { get; set; } = Guid.CreateVersion7();

        [Required]
        public DateTime Expiration { get; set; }

        [Required]
        public Guid RequestId { get; set; }
    }

}
