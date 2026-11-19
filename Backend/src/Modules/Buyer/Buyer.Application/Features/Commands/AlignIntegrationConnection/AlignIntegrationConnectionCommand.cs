using MediatR;
using SharedKernel.Integration.Dtos;

namespace Buyer.Application.Features.Commands.AlignIntegrationConnection
{
    /// <summary>
    /// Copies connection parameters and protected credentials from a working integration onto another
    /// of the same buyer. Process type, name and posting body stay on the target.
    /// </summary>
    public class AlignIntegrationConnectionCommand : IRequest<IntegrationConfigurationResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
        public Guid SourceConfigurationId { get; set; }
    }
}
