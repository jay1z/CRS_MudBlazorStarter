using Coravel.Events.Interfaces;

using CRS.Models;

namespace CRS.EventsAndListeners {
    public class ReserveStudyCreatedEvent : IEvent {
        public ReserveStudy ReserveStudy { get; set; }

        public ReserveStudyCreatedEvent(ReserveStudy reserveStudy) {
            ReserveStudy = reserveStudy;
        }
    }
}
