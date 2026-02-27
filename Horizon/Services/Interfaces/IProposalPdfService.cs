namespace Horizon.Services.Interfaces;

public interface IProposalPdfService
{
    Task<byte[]> GenerateProposalPdfAsync(Guid reserveStudyId);
}
