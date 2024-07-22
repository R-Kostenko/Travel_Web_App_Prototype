namespace Models
{
    public class UploadImage
    {
        public byte[] Data { get; set; }
        public long Size { get; set; }
        public string Name { get; set; }
        public string ContentType { get; set; }

        public UploadImage() { }

        public DateTimeOffset LastModified => DateTimeOffset.Now;

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            return new MemoryStream(Data);
        }

        public IFormFile ToFormFile()
        {
            var stream = OpenReadStream();
            return new FormFile(stream, 0, stream.Length, Name, Name)
            {
                Headers = new HeaderDictionary(),
                ContentType = ContentType
            };
        }
    }
}
