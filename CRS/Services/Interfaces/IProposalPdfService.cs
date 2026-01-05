namespace CRS.Services.Interfaces;

public interface IProposalPdfService
{
    Task<byte[]> GenerateProposalPdfAsync(Guid reserveStudyId);
}
