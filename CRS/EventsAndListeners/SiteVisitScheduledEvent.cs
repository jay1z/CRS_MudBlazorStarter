using Horizon.Models;

using Coravel.Events.Interfaces;

namespace Horizon.EventsAndListeners {
    public class SiteVisitScheduledEvent : IEvent {
        public ReserveStudy? ReserveStudy { get; set; }
        public DateTime SiteVisitDate { get; set; }

        public SiteVisitScheduledEvent(ReserveStudy? reserveStudy, DateTime siteVisitDate) {
            ReserveStudy = reserveStudy;
            SiteVisitDate = siteVisitDate;
        }
    }
}
