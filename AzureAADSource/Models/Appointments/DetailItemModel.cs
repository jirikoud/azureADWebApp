using AzureAADSource.Models.Commons;

namespace AzureAADSource.Models.Appointments
{
    public class DetailItemModel
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
        /// Datum vytvoření rezervace
        /// </summary>
        public DateTime? CreateDate { get; set; }

        /// <summary>
        /// Datum zrušení rezervace
        /// </summary>
        public DateTime? CancelDate { get; set; }

        /// <summary>
        /// Poznámka ke zrušení rezervace, např. důvod zrušení
        /// </summary>
        public string CancelNote { get; set; }

        /// <summary>
        /// Popis vyšetření
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Informace o čekárně
        /// </summary>
        /// <example>2.patro, napravo od výtahu</example>
        public string WaitingRoom { get; set; }

        /// <summary>
        /// Informace o zdravotním personálu
        /// </summary>
        public StaffModel Staff { get; set; }

        /// <summary>
        /// Informace o klinice
        /// </summary>
        public FacilityModel Facility { get; set; }

        /// <summary>
        /// Popis vyšetření
        /// </summary>
        /// <example>Vezměte prosím na vědomí, že ošetření je zpoplatněno dle platného ceníku.</example>
        public string ExaminationDescription { get; set; }

        /// <summary>
        /// Informace o specializaci
        /// </summary>
        public SpecializationModel Specialization { get; set; }

        /// <summary>
        /// Informace o vyšetření
        /// </summary>
        public ExaminationModel Examination { get; set; }

        public static DetailItemModel CreateMock(string? id = null)
        {
            id ??= Guid.NewGuid().ToString();
            var random = new Random();
            var detailItem = new DetailItemModel
            {
                Id = id,
                State = "Planned",
                Date = DateTime.Today.AddDays(random.Next(3, 30)).AddHours(random.Next(8, 17)),
                CreateDate = DateTime.Today.AddDays(-random.Next(10, 30)),
                CancelDate = null,
                CancelNote = null,
                Text = "Preventivní vyšetření",
                WaitingRoom = "2. patro u výtahu",
                Staff = StaffModel.CreateMock(),
                Facility = FacilityModel.CreateMock(),
                ExaminationDescription = "Moč sebou",
                Specialization = SpecializationModel.CreateMock(),
                Examination = ExaminationModel.CreateMock(),
            };
            return detailItem;
        }
    }
}
