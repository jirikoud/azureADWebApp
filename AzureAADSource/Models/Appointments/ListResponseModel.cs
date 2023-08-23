namespace AzureAADSource.Models.Appointments
{
    public class ListResponseModel
    {
        public List<ListItemModel> List { get; set; }

        public static ListResponseModel CreateMock()
        {
            var random = new Random();
            var model = new ListResponseModel
            {
                List = new List<ListItemModel>(),
            };
            for (int i = 0; i < random.Next(1, 10); i++)
            {
                model.List.Add(ListItemModel.CreateMock());
            }
            return model;
        }
    }
}
