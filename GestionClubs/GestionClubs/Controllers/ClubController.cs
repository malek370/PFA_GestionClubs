using GestionClubs.API.Validators;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace GestionClubs.API.Controllers
{
    public static class ClubController
    {
        public static void AddClubEndpoints(this WebApplication app)
        {
            var clubs = app.MapGroup("/api/clubs").WithTags("Clubs");

            clubs.MapGet("/", async ([FromServices] IClubServices clubServices, [FromQuery] string? name, [FromQuery] string? description, [AsParameters] PaginationParams pagination) =>
            {
                var filter = new FilterClubDTO
                {
                    Description = description,
                    Name = name
                };
                var result = await clubServices.GetClubs(filter, pagination);
                return Results.Ok(result);
            }).RequireAuthorization(AppRoles.Visitor);


            clubs.MapPost("/", async ([FromServices] IClubServices clubServices, [FromBody] CreateClubDTO dto) =>
            {
                await clubServices.CreateClub(dto);
                return Results.Created();
            }).RequireAuthorization(AppRoles.PlatformAdmin)
                .AddEndpointFilter<ValidationFilter<CreateClubDTO>>();

            clubs.MapGet("/user-clubs", async ([FromServices] IClubServices clubServices, [AsParameters] PaginationParams pagination) =>
                {
                    var result = await clubServices.GetUserClubs(pagination);
                    return Results.Ok(result);
                }).RequireAuthorization(AppRoles.ClubMember);
        }
    }
}
