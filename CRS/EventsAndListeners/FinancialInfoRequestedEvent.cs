using Coravel.Events.Interfaces;
using CRS.Models;

namespace CRS.EventsAndListeners
{
    public class FinancialInfoRequestedEvent : IEvent
    {
        public ReserveStudy ReserveStudy { get; set; }

        public FinancialInfoRequestedEvent(ReserveStudy reserveStudy)
        {
            ReserveStudy = reserveStudy;
        }
    }
}