using ChatRumi.Friendship.Api;
using ChatRumi.Friendship.Application;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApi()
    .AddApplication();

builder.Services.AddHostedService<AccountCreatedConsumer>();

var app = builder.Build();
 
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();
app.UseCors("CorsPolicy");  

app.UseHttpsRedirection();
