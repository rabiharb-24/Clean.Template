namespace Application.Common.Dto;

public sealed class FileDto
{
    public int Id { get; set; }

    public byte[] FileBytes { get; set; } = [];

    public string OriginalFileName { get; set; } = string.Empty;
}
