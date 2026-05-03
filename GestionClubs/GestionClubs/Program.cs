using GestionClubs.API.Controllers;
using GestionClubs.API.Handlers;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Application.Services;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Infrastructure.SqliteDbContext;
using GestionClubs.Infrastructure.SqliteDbContext.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//add database with SQLITE
builder.Services.AddInfrastructureServices_Sqlite();

// Register application services
builder.Services.AddScoped<IClubServices, ClubServices>();
builder.Services.AddScoped<IMembersService, MembersService>();
builder.Services.AddScoped<IAdhesionService, AdhesionService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddExceptionHandler<GlobalExcpectionHandler>();

var app = builder.Build();

// Seed the database
await app.Services.SeedDatabaseAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
    {
        opts.Title = "Documentation of GestionClubs";
    });
}
app.UseExceptionHandler(_ => { });
app.UseHttpsRedirection();

#region endpoints

// --- Clubs ---
app.AddClubEndpoints();

// --- Members ---
app.AddMembersEndpoints();


// --- Adhesions ---
app.AddAdhesionEndpoints();

#endregion




app.Run();


