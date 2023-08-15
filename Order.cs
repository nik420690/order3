namespace OrderAPI
{
    // Razred Order predstavlja entiteto naročila v kontekstu aplikacije.
    public class Order
    {
        // Lastnost _id hranji identifikator naročila.
        // MongoDB.Bson.ObjectId je tip, ki se pogosto uporablja za identifikacijo dokumentov v MongoDB.
        public MongoDB.Bson.ObjectId _id { get; set; }

        // Lastnost ProductIds je seznam nizov, ki predstavljajo identifikatorje izdelkov v naročilu.
        public List<string> ProductIds { get; set; }

        // Lastnost Status hranji stanje naročila (npr. "obdelano", "poslano", "dokončano").
        public string Status { get; set; }

        // Lastnost userId hranji identifikator uporabnika, ki je povezan z naročilom.
        public string userId { get; set; }
    }
}
