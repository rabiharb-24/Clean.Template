namespace Application.Common.Models.Responses;

public class GeneralResponse
{
    public bool Success { get; set; }

    public int StatusCode { get; set; }

    public Error? Error { get; set; }
}
