using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace GestionClubs.API.Controllers
{
    public static class EventController
    {
        public static void AddEventEndpoints(this WebApplication app)
        {
            var events = app.MapGroup("/api/events").WithTags("Events");

            events.MapGet("/", async ([FromServices] IEventService eventService, [FromQuery(Name = "Tags")] string tags, [AsParameters] PaginationParams pagination) =>
            {
                var tagList = string.IsNullOrEmpty(tags) ? null : tags.Split(';').ToList();
                var result = await eventService.GetEvents(tagList, pagination);
                return Results.Ok(result);
            }).RequireAuthorization(AppRoles.Visitor);

            events.MapPost("/", async ([FromServices] IEventService eventService, [FromBody] CreateEventDTO dto) =>
            {
                var result = await eventService.AddEvent(dto);
                return Results.Created($"/api/events/{result.Id}", result);
            }).RequireAuthorization(AppRoles.ClubAdmin);

            events.MapDelete("/{id}", async ([FromServices] IEventService eventService, int id) =>
            {
                var result = await eventService.DeleteEvent(id);
                return result ? Results.NoContent() : Results.NotFound();
            }).RequireAuthorization(AppRoles.ClubAdmin);

            events.MapPut("/{id}/join", async ([FromServices] IEventService eventService, int id) =>
            {
                var result = await eventService.JoinEvent(id);
                return Results.Ok(result);
            }).RequireAuthorization(AppRoles.Visitor);

                events.MapPut("/{id}/leave", async ([FromServices] IEventService eventService, int id) =>
                {
                    var result = await eventService.LeaveEvent(id);
                    return Results.Ok(result);
                }).RequireAuthorization(AppRoles.Visitor);

            events.MapGet("/{id}", async ([FromServices] IEventService eventService, int id) =>
            {
                var result = await eventService.GetEventById(id);
                return result != null ? Results.Ok(result) : Results.NotFound();
            }).RequireAuthorization(AppRoles.Visitor);


            events.MapGet("/user-events", async ([FromServices] IEventService eventService, [AsParameters] PaginationParams pagination) =>
            {
                var result = await eventService.GetUserEvents(pagination);
                return Results.Ok(result);
            }).RequireAuthorization(AppRoles.Visitor);
            events.MapGet("/club-events/{clubId}", async ([FromServices] IEventService eventService, int clubId, [AsParameters] PaginationParams pagination) =>
            {
                var result = await eventService.GetClubEvents(clubId, pagination);
                return Results.Ok(result);
            }).RequireAuthorization(AppRoles.Visitor);
        }
    }
}
