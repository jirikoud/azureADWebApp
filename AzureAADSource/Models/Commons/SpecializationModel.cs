namespace AzureAADSource.Models.Commons
{
    public class SpecializationModel
    {
        /// <summary>
        /// ID specializace
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Název specializace
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Identifikátor ikony specializace
        /// </summary>
        public string Image { get; set; }

        public static SpecializationModel CreateMock(string? id = null)
        {
            id ??= Guid.NewGuid().ToString();
            var detailItem = new SpecializationModel
            {
                Id = id,
                Title = "Praktický lékař",
                Image = "icon-general-practice",
            };
            return detailItem;
        }

    }
}
