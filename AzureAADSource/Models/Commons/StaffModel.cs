using AzureAADSource.Models.Appointments;

namespace AzureAADSource.Models.Commons
{
    public class StaffModel
    {
        public string Id { get; set; }
        public string Prefix { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Suffix { get; set; }
        public string Specialization { get; set; }

        public static StaffModel CreateMock(string? id = null)
        {
            id ??= Guid.NewGuid().ToString();
            var staffModel = new StaffModel
            {
                Id = id,
                Prefix = "MUDr.",
                Firstname = "Jan",
                Lastname = "Novák",
                Suffix = string.Empty,
                Specialization = "Praktický lékař",
            };
            return staffModel;
        }

    }
}
