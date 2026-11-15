using FluentValidation;
using RetoAbierto.Application.Features.Auth.DTOs;

namespace RetoAbierto.Application.Features.Auth.Validators;

public class RegistroEmpresaValidator : AbstractValidator<RegistroEmpresaRequest>
{
    public RegistroEmpresaValidator()
    {
        RuleFor(x => x.EmpresaNombre)
            .NotEmpty().WithMessage("El nombre de la empresa es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre de la empresa no debe superar los 150 caracteres.");

        RuleFor(x => x.EmpresaCorreo)
            .NotEmpty().WithMessage("El correo de la empresa es obligatorio.")
            .EmailAddress().WithMessage("El correo de la empresa no tiene un formato válido.");

        RuleFor(x => x.AdminNombre)
            .NotEmpty().WithMessage("El nombre del administrador es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre del administrador no debe superar los 100 caracteres.");

        RuleFor(x => x.AdminCorreo)
            .NotEmpty().WithMessage("El correo del administrador es obligatorio.")
            .EmailAddress().WithMessage("El correo del administrador no tiene un formato válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .Matches(@"[A-Z]").WithMessage("La contraseña debe contener al menos una letra mayúscula.")
            .Matches(@"[0-9]").WithMessage("La contraseña debe contener al menos un número.");
    }
}

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Correo)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo es inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}

public class CambiarPasswordValidator : AbstractValidator<CambiarPasswordRequest>
{
    public CambiarPasswordValidator()
    {
        RuleFor(x => x.PasswordActual)
            .NotEmpty().WithMessage("La contraseña actual es obligatoria.");

        RuleFor(x => x.PasswordNueva)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La nueva contraseña debe tener al menos 8 caracteres.")
            .Matches(@"[A-Z]").WithMessage("La nueva contraseña debe contener al menos una letra mayúscula.")
            .Matches(@"[0-9]").WithMessage("La nueva contraseña debe contener al menos un número.");

        RuleFor(x => x.Confirmacion)
            .Equal(x => x.PasswordNueva).WithMessage("La confirmación debe ser igual a la nueva contraseña.");
    }
}

