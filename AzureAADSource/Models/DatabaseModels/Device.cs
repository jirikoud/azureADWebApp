namespace AzureAADSource.Models.DatabaseModels
{
    public class Device
    {
        public string Id { get; set; }
        public string CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public bool Sale { get;set; }

    }
}
