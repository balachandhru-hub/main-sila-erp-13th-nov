namespace Buyer.Domain.Dtos
{
    public class SilaRecipePictureUploadDto
    {
        public string FileName { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
