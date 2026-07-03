using Ocelot.DependencyInjection;
using Ocelot.Middleware;
// See https://aka.ms/new-console-template for more information
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json");

builder.Services.AddOcelot();

var app = builder.Build();

app.UseOcelot().Wait();

app.Run();