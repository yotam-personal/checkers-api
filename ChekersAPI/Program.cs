using ChekersAPI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyAllowSpecificOrigins",
                      builder =>
                      {
                          builder.WithOrigins(
                              "http://localhost:4200",
                              "https://checkers-198a5.web.app",
                              "https://yotam.xyz"
                              )
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                      });
}); 

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.TypeInfoResolver = new MyJsonSerializerContext();
});

var app = builder.Build();
app.UseCors("MyAllowSpecificOrigins");
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
