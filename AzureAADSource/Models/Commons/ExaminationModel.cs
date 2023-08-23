namespace AzureAADSource.Models.Commons
{
    public class ExaminationModel
    {
        /// <summary>
        /// ID vyšetření
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Název vyšetření
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Detailní popisek vyšetření
        /// </summary>
        public string Text { get; set; }

        public static ExaminationModel CreateMock(string? id = null)
        {
            id ??= Guid.NewGuid().ToString();
            var detailItem = new ExaminationModel
            {
                Id = id,
                Title = "Preventivní návštěva",
                Text = "Vyšetření jednou za dva roky",
            };
            return detailItem;
        }

    }
}
