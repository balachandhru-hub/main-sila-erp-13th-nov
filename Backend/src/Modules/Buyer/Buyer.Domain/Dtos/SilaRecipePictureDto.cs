namespace Buyer.Domain.Dtos
{
    public class SilaRecipePictureDto
    {
        public bool HasPicture { get; set; }
        /// <summary>active | draft when a file exists.</summary>
        public string? Slot { get; set; }
    }
}
