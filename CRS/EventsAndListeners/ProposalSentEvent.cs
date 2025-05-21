using Coravel.Events.Interfaces;
using CRS.Models;

namespace CRS.EventsAndListeners
{
    public class ProposalSentEvent : IEvent
    {
        public ReserveStudy ReserveStudy { get; set; }
        public Proposal Proposal { get; set; }

        public ProposalSentEvent(ReserveStudy reserveStudy, Proposal proposal)
        {
            ReserveStudy = reserveStudy;
            Proposal = proposal;
        }
    }
}