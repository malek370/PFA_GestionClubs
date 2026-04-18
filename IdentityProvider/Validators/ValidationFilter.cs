using System.ComponentModel.DataAnnotations;

namespace IdentityProvider.Validators
{
    public class ValidationFilter<T> : IEndpointFilter where T : class
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            // Extract the argument of type T (e.g., the request body)
            var argument = context.Arguments.OfType<T>().FirstOrDefault();
            if (argument == null) return Results.BadRequest("Invalid request payload.");

            // Perform validation (example using Data Annotations)
            var validationContext = new ValidationContext(argument);
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(argument, validationContext, errors, true))
            {
                var errorMessages = errors
                    .Select(e => e.ErrorMessage ?? "")
                    .ToList();

                return Results.BadRequest(errorMessages);
            }

            return await next(context);
        }
    }
}
