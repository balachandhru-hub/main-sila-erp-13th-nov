namespace Buyer.Domain.Dtos
{
    public class SilaRecipePictureFileDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "image/jpeg";
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string Slot { get; set; } = "active";
    }
}
