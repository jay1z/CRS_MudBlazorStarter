using Coravel.Events.Interfaces;
using CRS.Models;

namespace CRS.EventsAndListeners
{
    public class ProposalApprovedEvent : IEvent
    {
        public ReserveStudy ReserveStudy { get; set; }
        public Proposal Proposal { get; set; }

        public ProposalApprovedEvent(ReserveStudy reserveStudy, Proposal proposal)
        {
            ReserveStudy = reserveStudy;
            Proposal = proposal;
        }
    }
}