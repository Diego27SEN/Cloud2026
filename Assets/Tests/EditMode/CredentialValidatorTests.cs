using NUnit.Framework;
using Cloud2026.Services;

namespace Cloud2026.Tests
{
    /// <summary>
    /// Pruebas del validador local de credenciales. Es una clase estática y pura, sin Unity
    /// de por medio, así que se cubre entera en EditMode y sin tocar la red.
    ///
    /// Las reglas replicadas aquí son las que documenta el SDK de UGS Authentication:
    /// usuario de 3-20 caracteres alfanuméricos y/o . - @ _, y contraseña de 8-30 caracteres
    /// con al menos una mayúscula, una minúscula, un número y un símbolo.
    /// </summary>
    public class CredentialValidatorTests
    {
        // ---------- Contraseña ----------

        [Test]
        public void Password_MeetingEveryRule_IsAccepted()
        {
            Assert.IsTrue(CredentialValidator.ValidatePassword("Passw0rd!").IsValid);
        }

        [Test]
        public void Password_TooShort_IsRejected()
        {
            var check = CredentialValidator.ValidatePassword("Pw0rd!");
            Assert.IsFalse(check.IsValid);
            StringAssert.Contains("8", check.Error, "El mensaje debe indicar la longitud mínima.");
        }

        [Test]
        public void Password_TooLong_IsRejected()
        {
            string tooLong = "Passw0rd!" + new string('a', 30);
            var check = CredentialValidator.ValidatePassword(tooLong);
            Assert.IsFalse(check.IsValid);
            StringAssert.Contains("30", check.Error, "El mensaje debe indicar la longitud máxima.");
        }

        [Test]
        public void Password_WithoutSymbol_IsRejectedAndSaysSo()
        {
            var check = CredentialValidator.ValidatePassword("Password1");
            Assert.IsFalse(check.IsValid);
            StringAssert.Contains("símbolo", check.Error);
        }

        [Test]
        public void Password_WithoutUppercase_IsRejectedAndSaysSo()
        {
            var check = CredentialValidator.ValidatePassword("passw0rd!");
            Assert.IsFalse(check.IsValid);
            StringAssert.Contains("mayúscula", check.Error);
        }

        [Test]
        public void Password_WithoutDigit_IsRejectedAndSaysSo()
        {
            var check = CredentialValidator.ValidatePassword("Password!");
            Assert.IsFalse(check.IsValid);
            StringAssert.Contains("número", check.Error);
        }

        [Test]
        public void Password_ErrorListsOnlyWhatIsMissing()
        {
            // Solo minúsculas y longitud correcta: faltan mayúscula, número y símbolo.
            var check = CredentialValidator.ValidatePassword("abcdefghij");

            Assert.IsFalse(check.IsValid);
            StringAssert.Contains("mayúscula", check.Error);
            StringAssert.Contains("número", check.Error);
            StringAssert.Contains("símbolo", check.Error);
            StringAssert.DoesNotContain("minúscula", check.Error,
                "No debe reclamar un requisito que la contraseña ya cumple.");
        }

        // ---------- Usuario ----------

        [Test]
        public void Username_WithAllowedSymbols_IsAccepted()
        {
            Assert.IsTrue(CredentialValidator.ValidateUsername("jugador.01-a@b_c").IsValid);
        }

        [Test]
        public void Username_TooShort_IsRejected()
        {
            Assert.IsFalse(CredentialValidator.ValidateUsername("ab").IsValid);
        }

        [Test]
        public void Username_TooLong_IsRejected()
        {
            Assert.IsFalse(CredentialValidator.ValidateUsername(new string('a', 21)).IsValid);
        }

        [Test]
        public void Username_WithAccentedLetter_IsRejected()
        {
            // El SDK habla de caracteres alfanuméricos, que aquí son ASCII: 'ñ' lo rechaza el servidor.
            var check = CredentialValidator.ValidateUsername("niño123");
            Assert.IsFalse(check.IsValid);
            StringAssert.Contains("ñ", check.Error, "El mensaje debe señalar el carácter culpable.");
        }

        [Test]
        public void Username_WithSpace_IsRejected()
        {
            Assert.IsFalse(CredentialValidator.ValidateUsername("jugador 01").IsValid);
        }

        // ---------- Validación combinada ----------

        [Test]
        public void Validate_OnSignIn_AcceptsAnExistingWeakPassword()
        {
            // Al iniciar sesión la contraseña ya está fijada: comprobar su composición
            // dejaría fuera a quien la creó bajo reglas anteriores.
            var check = CredentialValidator.Validate("jugador01", "vieja", checkPasswordRules: false);
            Assert.IsTrue(check.IsValid);
        }

        [Test]
        public void Validate_OnSignUp_RejectsTheSameWeakPassword()
        {
            var check = CredentialValidator.Validate("jugador01", "vieja", checkPasswordRules: true);
            Assert.IsFalse(check.IsValid);
        }

        [Test]
        public void Validate_ReportsTheUsernameProblemFirst()
        {
            // Con ambos campos mal, el jugador debe ver primero el del campo de arriba.
            var check = CredentialValidator.Validate("ab", "corta", checkPasswordRules: true);

            Assert.IsFalse(check.IsValid);
            StringAssert.Contains("usuario", check.Error.ToLowerInvariant());
        }

        [Test]
        public void Validate_EmptyPasswordOnSignIn_IsRejected()
        {
            var check = CredentialValidator.Validate("jugador01", "", checkPasswordRules: false);
            Assert.IsFalse(check.IsValid);
        }
    }
}
