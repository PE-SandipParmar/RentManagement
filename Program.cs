using RentManagement.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ILeaseRepository, LeaseRepository>();
builder.Services.AddScoped<IMonthlyRentPaymentRepository, MonthlyRentPaymentRepository>();
builder.Services.AddScoped<IBrokeragePaymentRepository, BrokeragePaymentRepository>();
builder.Services.AddScoped<ISecurityDepositRepository, SecurityDepositRepository>();
builder.Services.AddScoped<IMISReportRepository, MISReportRepository>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
