using GestionClubs.API.Validators;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace GestionClubs.API.Controllers
{
    public static class ClubController
    {
        public static void AddClubEndpoints(this WebApplication app)
        {
            var clubs = app.MapGroup("/api/clubs").WithTags("Clubs");

            clubs.MapGet("/", async ([FromServices] IClubServices clubServices, [FromHeader] string? name, [FromHeader] string? description) =>
            {
                var filter = new FilterClubDTO
                {
                    Description = description,
                    Name = name
                };
                var result = await clubServices.GetClubs(filter);
                return Results.Ok(result);
            });

            clubs.MapPost("/", async ([FromServices] IClubServices clubServices, [FromBody] CreateClubDTO dto) =>
            {
                await clubServices.CreateClub(dto);
                return Results.Created();
            }).AddEndpointFilter<ValidationFilter<CreateClubDTO>>();
        }
    }
}
