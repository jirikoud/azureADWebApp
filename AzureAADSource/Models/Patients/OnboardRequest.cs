namespace AzureAADSource.Models.Patients
{
    public class OnboardRequest
    {
        public string Username { get; set; }
        public int PatientId { get; set; }
        public int NISId { get; set; }
        public string CommunicationKey { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Ident { get; set; }
    }
}
