using CRS.Models;
using System.Globalization;

using CRS.Services.Interfaces;

using MudBlazor;

using static CRS.Components.Pages.Dashboard.Index;
using CRS.Data;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services {
    public class DashboardService : IDashboardService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public DashboardService(IDbContextFactory<ApplicationDbContext> dbFactory) {
            _dbFactory = dbFactory;
        }
    }
}
