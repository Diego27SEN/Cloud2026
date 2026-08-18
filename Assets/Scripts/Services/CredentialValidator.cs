using System;

namespace Cloud2026.Services
{
    /// <summary>
    /// Resultado de validar una credencial. Si <see cref="IsValid"/> es false,
    /// <see cref="Error"/> trae un mensaje listo para mostrar al jugador.
    /// </summary>
    public readonly struct CredentialCheck
    {
        public bool IsValid { get; }
        public string Error { get; }

        private CredentialCheck(bool isValid, string error)
        {
            IsValid = isValid;
            Error = error;
        }

        public static CredentialCheck Ok() => new CredentialCheck(true, string.Empty);
        public static CredentialCheck Fail(string error) => new CredentialCheck(false, error);
    }

    /// <summary>
    /// Comprobación local del formato de usuario y contraseña, con las reglas que documenta el
    /// SDK de UGS Authentication:
    ///
    ///   Usuario:    3-20 caracteres alfanuméricos y/o los símbolos . - @ _
    ///   Contraseña: 8-30 caracteres con al menos 1 mayúscula, 1 minúscula, 1 número y 1 símbolo
    ///
    /// Esto **no es autoridad**: el servidor vuelve a validar y su veredicto es el que manda.
    /// Solo existe para dar un mensaje inmediato y en castellano en vez de gastar una llamada de
    /// red para recibir el error en inglés.
    ///
    /// Ante la duda se es permisivo: es preferible dejar pasar algo que el servidor rechace
    /// (el jugador verá el error real) que bloquear en cliente una credencial que sí era válida.
    /// </summary>
    public static class CredentialValidator
    {
        public const int UsernameMinLength = 3;
        public const int UsernameMaxLength = 20;
        public const int PasswordMinLength = 8;
        public const int PasswordMaxLength = 30;

        private const string UsernameExtraChars = ".-@_";

        /// <summary>
        /// Comprueba el formato del nombre de usuario. No comprueba si ya está en uso:
        /// eso solo lo sabe el servidor.
        /// </summary>
        public static CredentialCheck ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return CredentialCheck.Fail("Escribe un nombre de usuario.");
            }

            if (username.Length < UsernameMinLength || username.Length > UsernameMaxLength)
            {
                return CredentialCheck.Fail(
                    $"El usuario necesita entre {UsernameMinLength} y {UsernameMaxLength} caracteres " +
                    $"(has escrito {username.Length}).");
            }

            foreach (char c in username)
            {
                if (!IsAsciiLetterOrDigit(c) && UsernameExtraChars.IndexOf(c) < 0)
                {
                    return CredentialCheck.Fail(
                        $"El usuario no admite el carácter '{c}'. Usa letras sin tilde, números y . - @ _");
                }
            }

            return CredentialCheck.Ok();
        }

        /// <summary>
        /// Comprueba la composición de la contraseña. Aplícalo solo al crear o vincular una
        /// cuenta: al iniciar sesión en una cuenta existente la contraseña ya está fijada y
        /// rechazarla en cliente impediría entrar a quien la creó bajo otras reglas.
        /// </summary>
        public static CredentialCheck ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return CredentialCheck.Fail("Escribe una contraseña.");
            }

            if (password.Length < PasswordMinLength || password.Length > PasswordMaxLength)
            {
                return CredentialCheck.Fail(
                    $"La contraseña necesita entre {PasswordMinLength} y {PasswordMaxLength} caracteres " +
                    $"(has escrito {password.Length}).");
            }

            bool hasUpper = false;
            bool hasLower = false;
            bool hasDigit = false;
            bool hasSymbol = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else hasSymbol = true;
            }

            if (hasUpper && hasLower && hasDigit && hasSymbol)
            {
                return CredentialCheck.Ok();
            }

            return CredentialCheck.Fail("A la contraseña le falta: " + DescribeMissing(
                hasUpper, hasLower, hasDigit, hasSymbol) + ".");
        }

        /// <summary>
        /// Valida ambos campos de una vez y devuelve el primer problema encontrado.
        /// </summary>
        /// <param name="checkPasswordRules">
        /// True al crear o vincular una cuenta. False al iniciar sesión, donde solo interesa
        /// que la contraseña no esté vacía.
        /// </param>
        public static CredentialCheck Validate(string username, string password, bool checkPasswordRules)
        {
            CredentialCheck user = ValidateUsername(username);
            if (!user.IsValid)
            {
                return user;
            }

            if (!checkPasswordRules)
            {
                return string.IsNullOrEmpty(password)
                    ? CredentialCheck.Fail("Escribe una contraseña.")
                    : CredentialCheck.Ok();
            }

            return ValidatePassword(password);
        }

        /// <summary>
        /// Enumera en castellano solo los requisitos que faltan, para no repetirle al jugador
        /// las reglas que ya cumple.
        /// </summary>
        private static string DescribeMissing(bool hasUpper, bool hasLower, bool hasDigit, bool hasSymbol)
        {
            var missing = new System.Collections.Generic.List<string>(4);
            if (!hasUpper) missing.Add("una mayúscula");
            if (!hasLower) missing.Add("una minúscula");
            if (!hasDigit) missing.Add("un número");
            if (!hasSymbol) missing.Add("un símbolo");

            if (missing.Count == 1)
            {
                return missing[0];
            }

            string last = missing[missing.Count - 1];
            missing.RemoveAt(missing.Count - 1);
            return string.Join(", ", missing) + " y " + last;
        }

        /// <summary>
        /// El SDK habla de caracteres alfanuméricos, que en este contexto son ASCII.
        /// char.IsLetterOrDigit aceptaría 'ñ' o 'é', que el servidor rechaza.
        /// </summary>
        private static bool IsAsciiLetterOrDigit(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
        }
    }
}
