using GestionClubs.API.Validators;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace GestionClubs.API.Controllers
{
    public static class AdhesionController
    {
        public static void AddAdhesionEndpoints(this WebApplication app)
        {
            var adhesions = app.MapGroup("/api/adhesions").WithTags("Adhesions");

            adhesions.MapGet("/club/{clubId:int}", async ([FromServices] IAdhesionService adhesionService, [FromRoute] int clubId) =>
            {
                var result = await adhesionService.GetAdhesionsByClub(clubId);
                return Results.Ok(result);
            }).RequireAuthorization(AppRoles.ClubAdmin);

            adhesions.MapGet("/{id:int}", async ([FromServices] IAdhesionService adhesionService, [FromRoute] int id) =>
            {
                var adhesion = await adhesionService.GetAdhesionById(id);
                return adhesion is not null ? Results.Ok(adhesion) : Results.NotFound();
            }).RequireAuthorization(AppRoles.ClubAdmin);

            adhesions.MapPost("/", async ([FromServices] IAdhesionService adhesionService, [FromBody] CreateAdhesionDTO dto) =>
            {
                var result = await adhesionService.AddAdhesion(dto);
                return Results.Created($"/api/adhesions/{result.Id}", result);
            }).RequireAuthorization(AppRoles.Visitor)
                .AddEndpointFilter<ValidationFilter<CreateAdhesionDTO>>();

            adhesions.MapPut("/{id:int}/accept", async ([FromServices] IAdhesionService adhesionService, [FromRoute] int id) =>
            {
                var result = await adhesionService.AcceptAdhesion(id);
                return Results.Ok(result);
            }).RequireAuthorization(AppRoles.ClubAdmin);

            adhesions.MapPut("/{id:int}/refuse", async ([FromServices] IAdhesionService adhesionService, [FromRoute] int id) =>
            {
                var result = await adhesionService.RefuseAdhesion(id);
                return Results.Ok(result);
            }).RequireAuthorization(AppRoles.ClubAdmin);

            adhesions.MapDelete("/{id:int}", async ([FromServices] IAdhesionService adhesionService, [FromRoute] int id) =>
            {
                var deleted = await adhesionService.DeleteAdhesion(id);
                return deleted ? Results.NoContent() : Results.NotFound();
            }).RequireAuthorization(AppRoles.ClubAdmin);
        }
    }
}
