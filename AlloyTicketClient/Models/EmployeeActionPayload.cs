using System.Text.Json;

namespace AlloyTicketClient.Models
{
    public class RequestActionPayload
    {
        public Guid FormId { get; set; }
        public string ObjectId { get; set; }
        public JsonElement Data { get; set; }
        public string CategoryId { get; set; }
        public string Route { get; set; }
    }
}