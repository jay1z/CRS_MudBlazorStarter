using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Data;

namespace CRS.Models {
    public class CalendarEvent : BaseModel {
        public string? Title { get; set; }
        public string? Color { get; set; }
        public string? Location { get; set; }
        [ForeignKey(nameof(ApplicationUser))] public Guid? ApplicationUserId { get; set; }
        [ForeignKey("Specialist")] public Guid? SpecialistUserId { get; set; }
        [DataType(DataType.Text)] public string? Description { get; set; }
        [DataType(DataType.DateTime)] public DateTime? Start { get; set; }
        [DataType(DataType.DateTime)] public DateTime? End { get; set; }
        public bool IsAllDay { get; set; }
        public bool IsRecurring { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsPublic { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsLocked { get; set; }
        public bool IsEditable { get; set; }
        public bool IsVisible { get; set; }
        public bool IsHidden { get; set; }
        public bool IsBusy { get; set; }
        public bool IsFree { get; set; }
        public bool IsTentative { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsAccepted { get; set; }
        public bool IsDeclined { get; set; }
        public bool IsInvited { get; set; }
        public bool IsAttending { get; set; }
        public bool IsNotAttending { get; set; }
        public bool IsMaybeAttending { get; set; }
        public bool IsResponded { get; set; }
        public bool IsNotResponded { get; set; }
        public bool IsRead { get; set; }
        public bool IsFlagged { get; set; }
        public bool IsPinned { get; set; }
        public bool IsArchived { get; set; }
        public bool IsMarked { get; set; }
        public bool IsStarred { get; set; }
        public bool IsLiked { get; set; }
        public bool IsShared { get; set; }
        public bool IsPublished { get; set; }
        public bool IsSubscribed { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsMuted { get; set; }
        public bool IsReported { get; set; }
        public bool IsIgnored { get; set; }

        public enum ClassName {
            Primary,
            Secondary,
            Success,
            Danger,
            Warning,
            Info,
            Light,
            Dark
        }
    }
}
