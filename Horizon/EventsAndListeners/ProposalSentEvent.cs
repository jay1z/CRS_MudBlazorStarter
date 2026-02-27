using Coravel.Events.Interfaces;
using Horizon.Models;

namespace Horizon.EventsAndListeners
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