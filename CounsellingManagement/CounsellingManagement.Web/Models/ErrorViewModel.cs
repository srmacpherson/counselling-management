namespace CounsellingManagement.Web.Models
{
    /// <summary>Carries request diagnostics displayed by the error page.</summary>
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
