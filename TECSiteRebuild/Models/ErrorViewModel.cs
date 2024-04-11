using Newtonsoft.Json;

namespace TECSite.Models
{
    public class ErrorViewModel
    {
        public Exception exception { get; set; }

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
