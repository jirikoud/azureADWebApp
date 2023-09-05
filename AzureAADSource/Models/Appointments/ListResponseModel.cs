namespace AzureAADSource.Models.Appointments
{
    public class ListResponseModel
    {
        public List<ListItemModel> List { get; set; }

        public static ListResponseModel CreateMock(int itemCount)
        {
            var model = new ListResponseModel
            {
                List = new List<ListItemModel>(),
            };
            for (int i = 0; i < itemCount; i++)
            {
                model.List.Add(ListItemModel.CreateMock());
            }
            return model;
        }
    }
}
