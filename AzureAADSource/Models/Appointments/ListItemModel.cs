using AzureAADSource.Models.Commons;

namespace AzureAADSource.Models.Appointments
{
    public class ListItemModel
    {
        /// <summary>
        /// ID rezervace
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Stav rezervace [Unknown, Planned, Realized, Cancelled, InProgress, Current]
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Datum a čas začátku rezervace
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Informace o doktorovi
        /// </summary>
        public StaffModel Staff { get; set; }

        /// <summary>
        /// Název zdravotnického zařízení
        /// </summary>
        public string FacilityName { get; set; }

        /// <summary>
        /// Název zdravotní specializace
        /// </summary>
        public string SpecializationName { get; set; }

        /// <summary>
        /// Informace o vyšetření
        /// </summary>
        public string ExaminationName { get; set; }

        public static ListItemModel CreateMock(string? id = null)
        {
            id ??= Guid.NewGuid().ToString();
            var random = new Random();
            var listItem = new ListItemModel
            {
                Id = id,
                State = "Planned",
                Date = DateTime.Today.AddDays(random.Next(3, 30)).AddHours(random.Next(8, 17)),
                Staff = StaffModel.CreateMock(),
                FacilityName = "Nemocnice na kraji města",
                SpecializationName = "Praktický lékař",
                ExaminationName = "Preventivní prohlídka",
            };
            return listItem;
        }

    }
}
