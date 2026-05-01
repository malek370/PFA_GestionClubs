using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
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
            });

            adhesions.MapGet("/{id:int}", async ([FromServices] IAdhesionService adhesionService, [FromRoute] int id) =>
            {
                var adhesion = await adhesionService.GetAdhesionById(id);
                return adhesion is not null ? Results.Ok(adhesion) : Results.NotFound();
            });

            adhesions.MapPost("/", async ([FromServices] IAdhesionService adhesionService, [FromBody] CreateAdhesionDTO dto) =>
            {
                var result = await adhesionService.AddAdhesion(dto);
                return Results.Created($"/api/adhesions/{result.Id}", result);
            });

            adhesions.MapPut("/{id:int}/accept", async ([FromServices] IAdhesionService adhesionService, [FromRoute] int id) =>
            {
                var result = await adhesionService.AcceptAdhesion(id);
                return Results.Ok(result);
            });

            adhesions.MapPut("/{id:int}/refuse", async ([FromServices] IAdhesionService adhesionService, [FromRoute] int id) =>
            {
                var result = await adhesionService.RefuseAdhesion(id);
                return Results.Ok(result);
            });

            adhesions.MapDelete("/{id:int}", async ([FromServices] IAdhesionService adhesionService, [FromRoute] int id) =>
            {
                var deleted = await adhesionService.DeleteAdhesion(id);
                return deleted ? Results.NoContent() : Results.NotFound();
            });
        }
    }
}
