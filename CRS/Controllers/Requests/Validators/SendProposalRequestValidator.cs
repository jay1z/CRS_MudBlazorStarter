using FluentValidation;
using Horizon.Controllers.Requests;

namespace Horizon.Controllers.Requests.Validators {
    public class SendProposalRequestValidator : AbstractValidator<SendProposalRequest> {
        public SendProposalRequestValidator() {
            RuleFor(x => x.ProposalScope).NotEmpty().WithMessage("Proposal scope is required.");
            RuleFor(x => x.EstimatedCost).GreaterThanOrEqualTo(0).WithMessage("Estimated cost must be >=0.");
            RuleFor(x => x.Comments).MaximumLength(2000).When(x => x.Comments != null);
        }
    }
}