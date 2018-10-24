using System;
using System.Collections.Generic;
using System.Text;

namespace Kaotoby.Openhack.Serverless.Challenge4.Models
{
    public class Rating
    {
        public Guid id { get; set; }
        public Guid userId { get; set; }
        public Guid productId { get; set; }
        public DateTime timestamp { get; set; }
        public string locationName { get; set; }
        public int rating { get; set; }
        public string userNotes { get; set; }
    }
}
