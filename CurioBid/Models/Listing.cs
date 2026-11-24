namespace CurioBid.Models
{
    public enum ListingStatus
    {
        Pending,
        NeedsProof,
        Approved,
        Rejected
    }

    public class Listing
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal StartingPrice { get; set; }
        public string EvidenceType { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        public ListingStatus Status { get; set; } = ListingStatus.Pending;

        // Who posted it
        public string SellerId { get; set; } = string.Empty;
        public ApplicationUser? Seller { get; set; }

        // Which reviewer it was sent to
        public string ReviewerId { get; set; } = string.Empty;
        public ApplicationUser? Reviewer { get; set; }

        public string? ReviewerNote { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public DateTime? AuctionEndsAt { get; set; }   // set on approval (UTC)
    }
}