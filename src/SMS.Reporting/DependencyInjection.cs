using Microsoft.Extensions.DependencyInjection;
using SMS.Domain.Interfaces;
using SMS.Reporting.Services;

namespace SMS.Reporting
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddReporting(this IServiceCollection services)
        {
            // Register PDF generator
            services.AddScoped<IPdfGenerator, PdfGeneratorService>();

            // Register Excel generator
            services.AddScoped<IExcelGenerator, ExcelGeneratorService>();

            // Register CSV service
            services.AddScoped<ICsvService, CsvService>();

            // Register reporting service
            services.AddScoped<IReportingService, ReportingService>();

            return services;
        }
    }
}
