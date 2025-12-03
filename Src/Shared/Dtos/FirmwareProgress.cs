namespace Shared.Dtos
{
    public class FirmwareProgress
    {
        public string FirmwareVersion { get; set; } = "";
        public int TotalDevices { get; set; }
        public int Completed { get; set; }
        public int Failed { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public double Percentage => TotalDevices == 0 ? 0 : (double)Completed / TotalDevices * 100;
        public bool IsCompleted => CompletedAt.HasValue;
    }
}
