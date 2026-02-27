using Coravel.Events.Interfaces;
using Horizon.Models;

namespace Horizon.EventsAndListeners
{
    public class ReserveStudyCompletedEvent : IEvent
    {
        public ReserveStudy ReserveStudy { get; set; }

        public ReserveStudyCompletedEvent(ReserveStudy reserveStudy)
        {
            ReserveStudy = reserveStudy;
        }
    }
}