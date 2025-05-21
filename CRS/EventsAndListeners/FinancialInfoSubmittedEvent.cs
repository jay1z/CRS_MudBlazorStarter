using Coravel.Events.Interfaces;
using CRS.Models;

namespace CRS.EventsAndListeners
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