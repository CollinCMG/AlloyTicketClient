using System.Text.Json;

namespace AlloyTicketClient.Models
{
    public class RequestActionKey
    {
        public RequestActionKey(Guid formId, string objectId)
        {
            FormId = formId;
            ObjectId = objectId;
        }
        public RequestActionKey() { }

        public Guid FormId { get; set; } = Guid.Empty;
        public string ObjectId { get; set; }
    }

    public class RequestActionPayload
    {
        public RequestActionKey Key { get; set; } = new RequestActionKey();
        public JsonElement Data { get; set; } 
        public string CategoryId { get; set; }
        public string Route { get; set; }
    }
}
