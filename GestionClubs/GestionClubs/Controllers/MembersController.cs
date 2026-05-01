using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace GestionClubs.API.Controllers
{
    public static class MembersController
    {
        public static void AddMembersEndpoints(this WebApplication app)
        {
            var members = app.MapGroup("/api/members").WithTags("Members");
            members.MapGet("/club/{clubId:int}", async ([FromServices] IMembersService membersService, [FromRoute] int clubId) =>
            {
                var result = await membersService.GetMembersByClub(clubId);
                return Results.Ok(result);
            });

            members.MapGet("/{id:int}", async ([FromServices] IMembersService membersService, [FromRoute] int id) =>
            {
                var member = await membersService.GetMemberById(id);
                return member is not null ? Results.Ok(member) : Results.NotFound();
            });

            members.MapPut("/post", async ([FromServices] IMembersService membersService, [FromBody] UpdateMemberPostDTO dto) =>
            {
                var result = await membersService.UpdateMemberPost(dto);
                return Results.Ok(result);
            });
            members.MapDelete("/{id:int}", async ([FromServices] IMembersService membersService, [FromRoute] int id) =>
            {
                var result = await membersService.RemoveMember(id);
                return result ? Results.Ok() : Results.NotFound();
            });

        }
    }
}
