namespace AzureAADSource.Models.Appointments
{
    public class DetailResponseModel
    {
        public ListItemModel ListItem { get; set; }
        public DetailItemModel DetailItem { get; set; }

        public static DetailResponseModel CreateMock(string? id = null)
        {
            id ??= Guid.NewGuid().ToString();
            var detail = new DetailResponseModel 
            { 
                ListItem = ListItemModel.CreateMock(id),
                DetailItem = DetailItemModel.CreateMock(id), 
            };
            return detail;
        }
    }
}
