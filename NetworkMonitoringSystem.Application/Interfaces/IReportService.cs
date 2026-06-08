using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Application.Interfaces
{
    public interface IReportService
    {
        Task<byte[]> GenerateSlaReportPdfAsync(int deviceId, int year, int month);
        Task<byte[]> GenerateCapacityReportExcelAsync();
    }
}
