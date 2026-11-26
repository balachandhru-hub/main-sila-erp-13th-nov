using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Entities;
using SharedKernel.Integration.Services;
using Xunit;

namespace Buyer.Tests.Shared
{
    public class SilaCulinaryAiConfigTests
    {
        private sealed class FakeProtector : IIntegrationCredentialProtector
        {
            public string? Protect(string? value) => string.IsNullOrWhiteSpace(value) ? null : $"ENC::{value}";
            public string? Unprotect(string? value) => value != null && value.StartsWith("ENC::", StringComparison.Ordinal) ? value[5..] : value;
            public string State(ApiIntegrationConfiguration configuration) => "Configured";
        }

        private sealed class FakeHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new();
        }

        [Fact]
        public void MaskSecret_NeverExposesRawKey()
        {
            FakeProtector protector = new();
            string? encrypted = protector.Protect("AIzaSySecretKey987654321");
            string? masked = SilaCulinaryAiSettings.MaskSecret(encrypted, protector);

            Assert.Equal("••••4321", masked);
            Assert.DoesNotContain("AIzaSy", masked);
            Assert.True(SilaCulinaryAiSettings.IsMaskedPlaceholder(masked));
            Assert.False(SilaCulinaryAiSettings.IsMaskedPlaceholder("AIzaSyNewSecret1234"));
        }

        [Fact]
        public void ToDto_MapsMaskedSecretsAndDefaultFlags()
        {
            Guid buyerId = Guid.NewGuid();
            FakeProtector protector = new();
            SilaCulinaryAiConfiguration entity = SilaCulinaryAiSettings.CreateDefault(buyerId);
            entity.Id = Guid.NewGuid();
            entity.ProtectedGeminiApiKey = protector.Protect("gemini-real-secret-abcd");

            SilaCulinaryAiConfigDto dto = SilaCulinaryAiSettings.ToDto(entity, buyerId, true, protector);

            Assert.True(dto.Saved);
            Assert.True(dto.CanEdit);
            Assert.True(dto.GeminiKeyConfigured);
            Assert.Equal("••••abcd", dto.GeminiKeyMasked);
            Assert.False(dto.ClaudeKeyConfigured);
            Assert.True(dto.FeatureFlags.CulinarySuiteEnabled);
            Assert.Equal(11, dto.TaskRouting.Count);
        }

        [Fact]
        public void ValidateWrite_RejectsInvalidOperatingModeAndBounds()
        {
            Assert.Throws<BadRequestCustomException>(() =>
                SilaCulinaryAiSettings.ValidateWrite(new SilaCulinaryAiConfigWriteDto { OperatingMode = "UNKNOWN" }));

            Assert.Throws<BadRequestCustomException>(() =>
                SilaCulinaryAiSettings.ValidateWrite(new SilaCulinaryAiConfigWriteDto { OperatingMode = "LOCAL", RequestTimeoutSeconds = 2 }));
        }

        [Fact]
        public async Task TestConnection_BlocksExternalAiWhenTenantConsentDisabled()
        {
            SilaCulinaryAiProviderGateway gateway = new(new FakeHttpClientFactory(), new FakeProtector());
            SilaCulinaryAiConfiguration config = SilaCulinaryAiSettings.CreateDefault(Guid.NewGuid());
            config.GeminiEnabled = true;
            config.AllowExternalAi = false;
            config.ProtectedGeminiApiKey = "ENC::secret-key-1234";

            SilaCulinaryAiConnectionTestResultDto res = await gateway.TestConnectionAsync(
                config,
                new SilaCulinaryAiConnectionTestRequestDto { Provider = "GEMINI" },
                CancellationToken.None);

            Assert.False(res.Success);
            Assert.Equal("EXTERNAL_AI_DISABLED", res.StatusCode);
        }
    }
}
