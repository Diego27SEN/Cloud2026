using HelloWorld;

namespace TestProject;

/// <summary>
/// Pruebas del saneamiento del nombre que llega del cliente. Es la única parte del
/// módulo que no necesita servidor: una función pura con entrada y salida.
/// </summary>
public class SanitizeDisplayNameTests
{
    [Test]
    public void DevuelveNombrePorDefectoCuandoLlegaVacio()
    {
        Assert.That(HelloWorldModule.SanitizeDisplayName(null), Is.EqualTo("jugador"));
        Assert.That(HelloWorldModule.SanitizeDisplayName(""), Is.EqualTo("jugador"));
        Assert.That(HelloWorldModule.SanitizeDisplayName("   "), Is.EqualTo("jugador"));
    }

    [Test]
    public void QuitaEspaciosSobrantes()
    {
        Assert.That(HelloWorldModule.SanitizeDisplayName("  Ana  "), Is.EqualTo("Ana"));
    }

    [Test]
    public void RecortaNombresDemasiadoLargos()
    {
        var largo = new string('a', 100);
        Assert.That(HelloWorldModule.SanitizeDisplayName(largo), Has.Length.EqualTo(24));
    }
}
