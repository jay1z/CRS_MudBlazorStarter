using Coravel.Events.Interfaces;
using Horizon.Models;

namespace Horizon.EventsAndListeners
{
    public class ProposalDeclinedEvent : IEvent
    {
        public ReserveStudy ReserveStudy { get; }
        public Proposal Proposal { get; }
        public bool RevisionRequested { get; }

        public ProposalDeclinedEvent(ReserveStudy reserveStudy, Proposal proposal, bool revisionRequested)
        {
            ReserveStudy = reserveStudy;
            Proposal = proposal;
            RevisionRequested = revisionRequested;
        }
    }
}
