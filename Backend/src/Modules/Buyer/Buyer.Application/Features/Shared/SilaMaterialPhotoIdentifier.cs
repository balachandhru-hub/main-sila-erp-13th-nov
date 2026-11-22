using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Photo identification facade over <see cref="SilaMaterialRecognitionEngine"/>.
    /// Never posts inventory and never selects a material on its own — callers must confirm.
    /// </summary>
    public static class SilaMaterialPhotoIdentifier
    {
        /// <summary>Legacy no-input path — returns engine empty-hint guidance without inventing matches.</summary>
        public static SilaStockCountPhotoIdentifyDto Identify()
        {
            return new SilaStockCountPhotoIdentifyDto
            {
                Configured = true,
                Message = "Capture a photo or enter a product hint. Confirm a Material Master match before counting. Inventory was not changed.",
                Candidates = new List<SilaStockCountPhotoCandidateDto>()
            };
        }

        public static Task<SilaStockCountPhotoIdentifyDto> IdentifyAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            string? hintText,
            byte[]? imageBytes,
            string? contentType,
            CancellationToken cancellationToken)
        {
            return SilaMaterialRecognitionEngine.IdentifyAsync(
                repository, buyerId, hintText, imageBytes, contentType, cancellationToken);
        }
    }
}