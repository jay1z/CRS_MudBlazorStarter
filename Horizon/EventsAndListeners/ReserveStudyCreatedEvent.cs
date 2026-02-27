using Coravel.Events.Interfaces;

using Horizon.Models;

namespace Horizon.EventsAndListeners {
    public class ReserveStudyCreatedEvent : IEvent {
        public ReserveStudy ReserveStudy { get; set; }

        public ReserveStudyCreatedEvent(ReserveStudy reserveStudy) {
            ReserveStudy = reserveStudy;
        }
    }
}
