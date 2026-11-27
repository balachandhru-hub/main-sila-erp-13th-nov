using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryMaterialIndexState : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        /// <summary>EMPTY, BUILDING, READY, FAILED.</summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "EMPTY";

        public int IndexedMaterialCount { get; set; }

        public int ActiveMaterialCount { get; set; }

        public int PendingUpdatesCount { get; set; }

        [MaxLength(160)]
        public string EmbeddingModelVersion { get; set; } = "BAAI/bge-small-en-v1.5";

        [MaxLength(160)]
        public string RerankingModelVersion { get; set; } = "Xenova/ms-marco-MiniLM-L-6-v2";

        [MaxLength(120)]
        public string SemanticBackend { get; set; } = "FASTEMBED_ONNX";

        public double BuildDurationMs { get; set; }

        public DateTime? LastBuildStartedUtc { get; set; }

        public DateTime? LastBuildCompletedUtc { get; set; }

        public DateTime? LastIncrementalSyncUtc { get; set; }

        [MaxLength(1000)]
        public string? LastError { get; set; }
    }
}
