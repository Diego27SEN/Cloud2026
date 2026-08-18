using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Cloud2026.Services;

namespace Cloud2026.Tests
{
    /// <summary>
    /// Pruebas EditMode para validar los contratos de arquitectura de autenticación y manejo de estados.
    /// </summary>
    public class AuthServiceTests
    {
        private class FakeAuthService : IAuthService
        {
            public event Action<string> OnSignedIn;
            public event Action OnSignedOut;
            public event Action<string> OnSignInFailed;

            public bool IsInitialized { get; set; }
            public bool IsSignedIn { get; set; }
            public string PlayerId { get; set; }
            public string PlayerName { get; set; }

            public bool ShouldFail { get; set; }
            public string SimulatedPlayerId { get; set; } = "test-player-123456";

            public Task InitializeAsync()
            {
                IsInitialized = true;
                return Task.CompletedTask;
            }

            public Task<bool> SignInAnonymouslyAsync()
            {
                if (ShouldFail)
                {
                    OnSignInFailed?.Invoke("Error de prueba simulado");
                    return Task.FromResult(false);
                }

                IsSignedIn = true;
                PlayerId = SimulatedPlayerId;
                OnSignedIn?.Invoke(PlayerId);
                return Task.FromResult(true);
            }

            public void SignOut(bool clearCredentials = false)
            {
                IsSignedIn = false;
                PlayerId = string.Empty;
                OnSignedOut?.Invoke();
            }
        }

        [Test]
        public async Task FakeAuthService_SignInAnonymously_RaisesSignedInEventAndSetsState()
        {
            var auth = new FakeAuthService();
            string receivedPlayerId = null;
            auth.OnSignedIn += id => receivedPlayerId = id;

            bool result = await auth.SignInAnonymouslyAsync();

            Assert.IsTrue(result);
            Assert.IsTrue(auth.IsSignedIn);
            Assert.AreEqual("test-player-123456", auth.PlayerId);
            Assert.AreEqual("test-player-123456", receivedPlayerId);
        }

        [Test]
        public async Task FakeAuthService_SignInFailure_RaisesFailedEvent()
        {
            var auth = new FakeAuthService { ShouldFail = true };
            string errorMessage = null;
            auth.OnSignInFailed += err => errorMessage = err;

            bool result = await auth.SignInAnonymouslyAsync();

            Assert.IsFalse(result);
            Assert.IsFalse(auth.IsSignedIn);
            Assert.IsNotNull(errorMessage);
        }

        [Test]
        public async Task FakeAuthService_SignOut_ClearsPlayerStateAndRaisesEvent()
        {
            var auth = new FakeAuthService();
            bool signedOutFired = false;
            auth.OnSignedOut += () => signedOutFired = true;

            await auth.SignInAnonymouslyAsync();
            Assert.IsTrue(auth.IsSignedIn);

            auth.SignOut();
            Assert.IsFalse(auth.IsSignedIn);
            Assert.IsEmpty(auth.PlayerId);
            Assert.IsTrue(signedOutFired);
        }
    }
}
