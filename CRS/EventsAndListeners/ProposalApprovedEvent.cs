using Coravel.Events.Interfaces;
using Horizon.Models;

namespace Horizon.EventsAndListeners
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