namespace Ringo.Api.Services
{
    public class StationServiceResult
    {
        public int Status { get; internal set; }
        public string Message { get; internal set; }
        public bool Success { get; set; }
    }
}