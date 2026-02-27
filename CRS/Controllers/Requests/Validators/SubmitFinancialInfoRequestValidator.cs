using FluentValidation;
using Horizon.Controllers.Requests;

namespace Horizon.Controllers.Requests.Validators {
    public class SubmitFinancialInfoRequestValidator : AbstractValidator<SubmitFinancialInfoRequest> {
        public SubmitFinancialInfoRequestValidator() {
            RuleFor(x => x.CurrentReserveFundBalance).GreaterThanOrEqualTo(0);
            RuleFor(x => x.AnnualContribution).GreaterThanOrEqualTo(0);
            RuleFor(x => x.FiscalYearStartMonth).InclusiveBetween(1, 12);
            RuleFor(x => x.FinancialDocumentUrls).MaximumLength(4000).When(x => x.FinancialDocumentUrls != null);
            RuleFor(x => x.Comments).MaximumLength(2000).When(x => x.Comments != null);
        }
    }
}