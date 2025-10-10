using ChatRum.InterCommunication.ServiceDiscovery;
using ChatRumi.Feed.Api;
using ChatRumi.Feed.Application;

var builder = WebApplication.CreateBuilder(args);
 
builder.Services.AddApi(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddConsulService(builder.Configuration);

var app = builder.Build();
  
app.UseCors("CorsPolicy");

app.UseHttpsRedirection();
 
app.Run();
 