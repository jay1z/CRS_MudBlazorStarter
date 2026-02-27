using Coravel.Events.Interfaces;
using Horizon.Models;

namespace Horizon.EventsAndListeners
{
    public class FinancialInfoSubmittedEvent : IEvent
    {
        public ReserveStudy ReserveStudy { get; set; }
        public FinancialInfo FinancialInfo { get; set; }

        public FinancialInfoSubmittedEvent(ReserveStudy reserveStudy, FinancialInfo financialInfo)
        {
            ReserveStudy = reserveStudy;
            FinancialInfo = financialInfo;
        }
    }
}