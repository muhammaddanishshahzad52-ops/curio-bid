namespace CurioBid.Models
{
    public class Auction
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal CurrentBid { get; set; }
        public DateTime EndTime { get; set; }   // stored in UTC
        public string ImageUrl { get; set; } = string.Empty;
    }
}