namespace RentManagement.Models
{
    public class LeaseDocument
    {
        public int Id { get; set; }
        public int LeaseId { get; set; }
        public string FileName { get; set; }
        public string UniqueFileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; }

        // Navigation property
        public virtual Lease Lease { get; set; }
    }
}
