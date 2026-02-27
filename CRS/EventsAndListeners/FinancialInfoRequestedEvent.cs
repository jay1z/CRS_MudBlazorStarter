using Coravel.Events.Interfaces;
using Horizon.Models;

namespace Horizon.EventsAndListeners
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