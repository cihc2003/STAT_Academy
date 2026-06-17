using Microsoft.AspNetCore.Identity;

namespace STAT_Academy.Web.Services;

public class SpanishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Error("Ocurrió un error inesperado.");
    public override IdentityError ConcurrencyFailure() => Error("El registro fue modificado por otro proceso. Intenta de nuevo.");
    public override IdentityError PasswordMismatch() => Error("La contraseña actual no es correcta.");
    public override IdentityError InvalidToken() => Error("El token no es válido.");
    public override IdentityError LoginAlreadyAssociated() => Error("Este inicio de sesión ya está asociado a otra cuenta.");
    public override IdentityError InvalidUserName(string? userName) => Error($"El usuario '{userName}' no es válido.");
    public override IdentityError InvalidEmail(string? email) => Error($"El correo '{email}' no es válido.");
    public override IdentityError DuplicateUserName(string userName) => Error($"El usuario '{userName}' ya existe.");
    public override IdentityError DuplicateEmail(string email) => Error($"El correo '{email}' ya está registrado.");
    public override IdentityError InvalidRoleName(string? role) => Error($"El rol '{role}' no es válido.");
    public override IdentityError DuplicateRoleName(string role) => Error($"El rol '{role}' ya existe.");
    public override IdentityError UserAlreadyHasPassword() => Error("El usuario ya tiene una contraseña asignada.");
    public override IdentityError UserLockoutNotEnabled() => Error("El bloqueo no está habilitado para este usuario.");
    public override IdentityError UserAlreadyInRole(string role) => Error($"El usuario ya pertenece al rol '{role}'.");
    public override IdentityError UserNotInRole(string role) => Error($"El usuario no pertenece al rol '{role}'.");
    public override IdentityError PasswordTooShort(int length) => Error($"La contraseña debe tener al menos {length} caracteres.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => Error("La contraseña debe incluir al menos un carácter especial.");
    public override IdentityError PasswordRequiresDigit() => Error("La contraseña debe incluir al menos un número.");
    public override IdentityError PasswordRequiresLower() => Error("La contraseña debe incluir al menos una letra minúscula.");
    public override IdentityError PasswordRequiresUpper() => Error("La contraseña debe incluir al menos una letra mayúscula.");

    private static IdentityError Error(string description) => new() { Description = description };
}
