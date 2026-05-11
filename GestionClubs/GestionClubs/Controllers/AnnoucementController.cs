using GestionClubs.API.Validators;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace GestionClubs.API.Controllers
{
    public static class AnnoucementController
    {
        public static void AddAnnoucementEndpoints(this WebApplication app)
        {
            var annoucements = app.MapGroup("/api/annoucements").WithTags("Annoucements");

            annoucements.MapGet("/club/{clubId:int}", async ([FromServices] IAnnoucementService annoucementService, [FromRoute] int clubId, [AsParameters] PaginationParams pagination) =>
            {
                var result = await annoucementService.GetByClubId(clubId, pagination);
                return Results.Ok(result);
            }).RequireAuthorization(AppRoles.ClubAdmin);

            annoucements.MapGet("/{id:int}", async ([FromServices] IAnnoucementService annoucementService, [FromRoute] int id) =>
            {
                var annoucement = await annoucementService.GetAnnoucementById(id);
                return annoucement is not null ? Results.Ok(annoucement) : Results.NotFound();
            }).RequireAuthorization(AppRoles.ClubMember);

            annoucements.MapPost("/", async ([FromServices] IAnnoucementService annoucementService, [FromBody] CreateAnnoucementDTO dto) =>
            {
                var result = await annoucementService.CreateAnnoucement(dto);
                return Results.Created($"/api/annoucements/{result.Id}", result);
            }).RequireAuthorization(AppRoles.ClubAdmin)
                .AddEndpointFilter<ValidationFilter<CreateAnnoucementDTO>>();

            annoucements.MapDelete("/{id:int}", async ([FromServices] IAnnoucementService annoucementService, [FromRoute] int id) =>
            {
                await annoucementService.DeleteAnnoucement(id);
                return Results.NoContent();
            }).RequireAuthorization(AppRoles.ClubAdmin);
        }
    }
}
