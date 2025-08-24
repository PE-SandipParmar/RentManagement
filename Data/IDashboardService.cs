﻿using RentManagement.Models;

namespace RentManagement.Data
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(int financialYear);
        Task<DashboardStatistics> GetStatisticsAsync(int financialYear);
        Task<object> GetChartDataAsync(string chartType, int financialYear);
    }
}
