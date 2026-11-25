using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Integration.Dtos
{
    /// <summary>Body for copying connection settings from a working integration onto another.</summary>
    public class AlignIntegrationConnectionInputDto
    {
        [Required]
        public Guid SourceConfigurationId { get; set; }
    }
}
