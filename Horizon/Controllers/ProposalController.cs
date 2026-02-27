using Horizon.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Horizon.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProposalController : ControllerBase
{
    private readonly IProposalPdfService _pdfService;

    public ProposalController(IProposalPdfService pdfService)
    {
        _pdfService = pdfService;
    }

    [HttpGet("{studyId:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid studyId)
    {
        try
        {
            var pdf = await _pdfService.GenerateProposalPdfAsync(studyId);
            return File(pdf, "application/pdf", $"Proposal_{studyId:N}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
