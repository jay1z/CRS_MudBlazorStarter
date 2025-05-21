using Coravel.Events.Interfaces;
using CRS.Models;

namespace CRS.EventsAndListeners
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