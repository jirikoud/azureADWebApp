using AzureAADSource.Models.Appointments;

namespace AzureAADSource.Models.Commons
{
    public class FacilityModel
    {
        public string Id { get; set; }

        /// <summary>
        /// Název zařízení
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Adresa zařízení
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Souřadnice zeměpisné šířky
        /// </summary>
        public double? Lat { get; set; }

        /// <summary>
        /// Souřadnice zeměpisné délky
        /// </summary>
        public double? Lng { get; set; }

        /// <summary>
        /// Informace o parkování
        /// </summary>
        public string ParkingNote { get; set; }

        /// <summary>
        /// Odkaz na mapu
        /// </summary>
        public string MapUrl { get; set; }

        public static FacilityModel CreateMock(string? id = null)
        {
            id ??= Guid.NewGuid().ToString();
            var detailItem = new FacilityModel
            {
                Id = id,
                Name = "Nemocnice na kraji města",
                Address = "Na kraji města 155\nPraha",
                Lat = 50.0737983,
                Lng = 14.3427903,
                ParkingNote = "Parkování možné",
                MapUrl = "https://mapy.cz/zakladni?source=base&id=2065845&x=14.3427991&y=50.0739061&z=18",
            };
            return detailItem;
        }

    }
}
