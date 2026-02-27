using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Horizon.Models;
using Horizon.Services.Interfaces;
using Horizon.Controllers.Requests;

namespace Horizon.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class ReserveStudyWorkflowController : ControllerBase {
        private readonly IReserveStudyWorkflowService _workflowService;

        public ReserveStudyWorkflowController(IReserveStudyWorkflowService workflowService) {
            _workflowService = workflowService;
        }

        // Create a new reserve study request (any authenticated user)
        [HttpPost("reserve-study")]
        [Authorize]
        public async Task<ActionResult<ReserveStudy>> CreateReserveStudyRequest([FromBody] CreateReserveStudyRequest req) {
            if (req == null) return BadRequest("request is required");

            var reserveStudy = new ReserveStudy {
                ApplicationUserId = req.ApplicationUserId,
                SpecialistUserId = req.SpecialistUserId,
                CommunityId = req.CommunityId,
                PointOfContactType = (ReserveStudy.PointOfContactTypeEnum)req.PointOfContactType,
                ContactId = req.ContactId,
                PropertyManagerId = req.PropertyManagerId,
                TenantId = req.TenantId,
                IsActive = true
            };

            var created = await _workflowService.CreateReserveStudyRequestAsync(reserveStudy);
            return Ok(created);
        }

        // Specialist or Admin may send proposals
        [HttpPost("{reserveStudyId:guid}/proposal")]
        [Authorize(Roles = "Specialist,Admin")]
        public async Task<IActionResult> SendProposal(Guid reserveStudyId, [FromBody] SendProposalRequest req) {
            if (req == null) return BadRequest("request is required");

            var proposal = new Proposal {
                ProposalScope = req.ProposalScope,
                EstimatedCost = req.EstimatedCost,
                Comments = req.Comments
            };

            var ok = await _workflowService.SendProposalAsync(reserveStudyId, proposal);
            if (!ok) return BadRequest();
            return NoContent();
        }

        // Specialist or Admin may approve proposals
        [HttpPost("proposal/{proposalId:guid}/approve")]
        [Authorize(Roles = "Specialist,Admin")]
        public async Task<IActionResult> ApproveProposal(Guid proposalId) {
            var approvedBy = User?.Identity?.Name ?? "system";
            var ok = await _workflowService.ApproveProposalAsync(proposalId, approvedBy);
            if (!ok) return BadRequest();
            return NoContent();
        }

        // Specialist or Admin may request financial info
        [HttpPost("{reserveStudyId:guid}/request-financial-info")]
        [Authorize(Roles = "Specialist,Admin")]
        public async Task<IActionResult> RequestFinancialInfo(Guid reserveStudyId) {
            var ok = await _workflowService.RequestFinancialInfoAsync(reserveStudyId);
            if (!ok) return BadRequest();
            return NoContent();
        }

        // Any authenticated user may submit financial info
        [HttpPost("{reserveStudyId:guid}/financial-info")]
        [Authorize]
        public async Task<IActionResult> SubmitFinancialInfo(Guid reserveStudyId, [FromBody] SubmitFinancialInfoRequest req) {
            if (req == null) return BadRequest("request is required");

            var financialInfo = new FinancialInfo {
                CurrentReserveFundBalance = req.CurrentReserveFundBalance,
                AnnualContribution = req.AnnualContribution,
                ProjectedAnnualExpenses = req.ProjectedAnnualExpenses,
                FiscalYearStartMonth = req.FiscalYearStartMonth,
                FinancialDocumentUrls = req.FinancialDocumentUrls,
                Comments = req.Comments
            };

            var ok = await _workflowService.SubmitFinancialInfoAsync(reserveStudyId, financialInfo);
            if (!ok) return BadRequest();
            return NoContent();
        }

        // Specialist or Admin may review financial info
        [HttpPost("financial-info/{financialInfoId:guid}/review")]
        [Authorize(Roles = "Specialist,Admin")]
        public async Task<IActionResult> ReviewFinancialInfo(Guid financialInfoId) {
            var reviewedBy = User?.Identity?.Name ?? "system";
            var ok = await _workflowService.ReviewFinancialInfoAsync(financialInfoId, reviewedBy);
            if (!ok) return BadRequest();
            return NoContent();
        }

        // Specialist or Admin may mark reserve study complete
        [HttpPost("{reserveStudyId:guid}/complete")]
        [Authorize(Roles = "Specialist,Admin")]
        public async Task<IActionResult> CompleteReserveStudy(Guid reserveStudyId) {
            var ok = await _workflowService.CompleteReserveStudyAsync(reserveStudyId);
            if (!ok) return BadRequest();
            return NoContent();
        }
    }
}
