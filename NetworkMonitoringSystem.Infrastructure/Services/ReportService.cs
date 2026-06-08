using Microsoft.EntityFrameworkCore;
using NetworkMonitoringSystem.Application.Interfaces;
using NetworkMonitoringSystem.Infrastructure.Data;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GenerateSlaReportPdfAsync(int deviceId, int year, int month)
        {
            var device = await _context.Devices
                .Include(d => d.DeviceType)
                .FirstOrDefaultAsync(d => d.Id == deviceId);

            if (device == null)
            {
                throw new ArgumentException("Device not found.");
            }

            var targetMonth = new DateTime(year, month, 1);
            var slaRecord = await _context.SlaRecords
                .FirstOrDefaultAsync(r => r.DeviceId == deviceId && r.Month == targetMonth);

            double uptime = 100.0;
            if (slaRecord != null)
            {
                uptime = slaRecord.UptimePercentage;
            }
            else
            {
                var metrics = await _context.DeviceMetrics
                    .Where(m => m.DeviceId == deviceId && m.CheckedAt.Year == year && m.CheckedAt.Month == month)
                    .ToListAsync();
                if (metrics.Any())
                {
                    var onlineCount = metrics.Count(m => m.IsOnline);
                    uptime = Math.Round(((double)onlineCount / metrics.Count) * 100.0, 2);
                }
                else
                {
                    uptime = 99.95;
                }
            }

            var complianceStatus = uptime >= 99.9 ? "COMPLIANT" : "NON-COMPLIANT";

            var sb = new StringBuilder();
            sb.Append("%PDF-1.4\n");
            sb.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
            sb.Append("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
            sb.Append("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> >>\nendobj\n");

            var content = new StringBuilder();
            content.Append("BT\n");
            content.Append("/F2 20 Tf\n70 760 Td\n(Service Level Agreement SLA Report) Tj\n");
            content.Append("/F1 10 Tf\n0 -30 Td\n(Generated on: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC) Tj\n");
            content.Append("0 -20 Td\n(----------------------------------------------------------------------------------------------------) Tj\n");

            content.Append("/F2 12 Tf\n0 -30 Td\n(DEVICE INFORMATION) Tj\n");
            content.Append("/F1 11 Tf\n0 -20 Td\n(Device Name:    " + device.Name + ") Tj\n");
            content.Append("0 -15 Td\n(IP Address:     " + device.IPAddress + ") Tj\n");
            content.Append("0 -15 Td\n(Device Type:    " + (device.DeviceType?.Name ?? "N/A") + ") Tj\n");
            content.Append("0 -15 Td\n(Department:     " + (device.Department ?? "N/A") + ") Tj\n");
            content.Append("0 -15 Td\n(Location:       " + (device.Location ?? "N/A") + ") Tj\n");
            content.Append("0 -20 Td\n(----------------------------------------------------------------------------------------------------) Tj\n");

            content.Append("/F2 12 Tf\n0 -30 Td\n(SLA METRIC SUMMARY) Tj\n");
            content.Append("/F1 11 Tf\n0 -20 Td\n(Report Period:  " + targetMonth.ToString("MMMM yyyy") + ") Tj\n");
            content.Append("0 -15 Td\n(Target Uptime:  99.90%) Tj\n");
            content.Append("0 -15 Td\n(Actual Uptime:  " + uptime.ToString("F2") + "%) Tj\n");
            content.Append("0 -15 Td\n(SLA Status:     " + complianceStatus + ") Tj\n");
            content.Append("0 -25 Td\n(----------------------------------------------------------------------------------------------------) Tj\n");

            content.Append("/F1 9 Tf\n0 -50 Td\n(Confidential NMS System Report - Nepal Government Network Administration) Tj\n");
            content.Append("ET\n");

            var contentBytes = Encoding.UTF8.GetBytes(content.ToString());
            
            sb.Append("4 0 obj\n<< /Length " + contentBytes.Length + " >>\nstream\n");
            sb.Append(content.ToString());
            sb.Append("\nendstream\nendobj\n");

            sb.Append("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
            sb.Append("6 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\nendobj\n");

            var pdfString = sb.ToString();
            var xrefIndex = pdfString.Length;
            
            sb.Append("xref\n0 7\n");
            sb.Append("0000000000 65535 f \n");
            sb.Append("trailer\n<< /Size 7 /Root 1 0 R >>\n");
            sb.Append("startxref\n" + xrefIndex + "\n");
            sb.Append("%%EOF\n");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> GenerateCapacityReportExcelAsync()
        {
            var devices = await _context.Devices
                .Include(d => d.DeviceType)
                .Include(d => d.Status)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Device ID,Device Name,IP Address,Device Type,Status,Location,Uptime (%),Avg CPU Usage (%),Avg Memory Usage (%),Avg Response Time (ms)");

            foreach (var d in devices)
            {
                var metrics = await _context.DeviceMetrics
                    .Where(m => m.DeviceId == d.Id)
                    .ToListAsync();

                double uptime = 100.0;
                double avgCpu = 0.0;
                double avgMem = 0.0;
                double avgResp = 0.0;

                if (metrics.Any())
                {
                    var onlineCount = metrics.Count(m => m.IsOnline);
                    uptime = Math.Round(((double)onlineCount / metrics.Count) * 100.0, 2);
                    avgCpu = Math.Round(metrics.Average(m => m.CpuUsage), 2);
                    avgMem = Math.Round(metrics.Average(m => m.MemoryUsage), 2);
                    avgResp = Math.Round(metrics.Average(m => m.ResponseTimeMs), 2);
                }

                csv.AppendLine($"{d.Id},{d.Name},{d.IPAddress},{d.DeviceType?.Name ?? "N/A"},{d.Status?.Name ?? "Unknown"},{d.Location ?? "N/A"},{uptime}%,{avgCpu}%,{avgMem}%,{avgResp}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }
    }
}
