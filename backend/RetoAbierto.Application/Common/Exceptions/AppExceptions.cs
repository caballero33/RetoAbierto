using FluentValidation.Results;

namespace RetoAbierto.Application.Common.Exceptions;

public class AppValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public AppValidationException() : base("Uno o más campos no son válidos.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public AppValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(e => char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName.Substring(1), e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public AppValidationException(string field, string message) : this()
    {
        Errors = new Dictionary<string, string[]>
        {
            { field, new[] { message } }
        };
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string name, object key) : base($"El recurso '{name}' con identificador ({key}) no fue encontrado.") { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "No tiene permiso para realizar esta operación.") : base(message) { }
}

