using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;

namespace Buyer.Tests.Shared
{
    public class SilaMaterialRecognitionEngineTests
    {
        [Fact]
        public void PhotoIdentifier_EmptyPath_DoesNotInventMaterials()
        {
            SilaStockCountPhotoIdentifyDto result = SilaMaterialPhotoIdentifier.Identify();
            Assert.True(result.Configured);
            Assert.Empty(result.Candidates);
            Assert.Contains("Inventory was not changed", result.Message);
        }

        [Fact]
        public async Task RecognitionEngine_EmptyHint_DoesNotInventMaterials()
        {
            SilaStockCountPhotoIdentifyDto result = await SilaMaterialRecognitionEngine.IdentifyAsync(
                null!,
                Guid.NewGuid(),
                "   ",
                null,
                null,
                CancellationToken.None);

            Assert.True(result.Configured);
            Assert.Empty(result.Candidates);
            Assert.False(result.PreferBarcodeFallback);
            Assert.Contains("Inventory was not changed", result.Message);
        }
    }
}
