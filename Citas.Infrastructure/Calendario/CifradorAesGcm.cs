using System.Security.Cryptography;
using System.Text;
using Citas.Application.Calendario;

namespace Citas.Infrastructure.Calendario;

/// <summary>
/// Cifra secretos en reposo con AES-256-GCM (confidencialidad e integridad). El resultado es
/// base64(nonce[12] | etiqueta[16] | cifrado); cada cifrado usa su propio nonce aleatorio.
/// La clave (32 bytes en base64) viene de la configuración, nunca del código.
/// </summary>
public sealed class CifradorAesGcm : ICifradorSecretos
{
    private const int TamanoNonce = 12;
    private const int TamanoEtiqueta = 16;

    private readonly byte[] _clave;

    public CifradorAesGcm(OpcionesGoogle opciones)
    {
        if (string.IsNullOrWhiteSpace(opciones.ClaveCifradoBase64))
        {
            _clave = [];   // integración apagada: cualquier intento de usarlo falla con un mensaje claro
            return;
        }
        _clave = Convert.FromBase64String(opciones.ClaveCifradoBase64);
        if (_clave.Length != 32)
            throw new InvalidOperationException("Google__ClaveCifradoBase64 debe ser una clave de 32 bytes en base64.");
    }

    public string Cifrar(string textoPlano)
    {
        ExigirClave();
        var datos = Encoding.UTF8.GetBytes(textoPlano);
        var nonce = RandomNumberGenerator.GetBytes(TamanoNonce);
        var cifrado = new byte[datos.Length];
        var etiqueta = new byte[TamanoEtiqueta];

        using var aes = new AesGcm(_clave, TamanoEtiqueta);
        aes.Encrypt(nonce, datos, cifrado, etiqueta);

        return Convert.ToBase64String([.. nonce, .. etiqueta, .. cifrado]);
    }

    public string Descifrar(string textoCifrado)
    {
        ExigirClave();
        var todo = Convert.FromBase64String(textoCifrado);
        if (todo.Length < TamanoNonce + TamanoEtiqueta)
            throw new CryptographicException("El dato cifrado está incompleto.");

        var nonce = todo.AsSpan(0, TamanoNonce);
        var etiqueta = todo.AsSpan(TamanoNonce, TamanoEtiqueta);
        var cifrado = todo.AsSpan(TamanoNonce + TamanoEtiqueta);
        var datos = new byte[cifrado.Length];

        using var aes = new AesGcm(_clave, TamanoEtiqueta);
        aes.Decrypt(nonce, cifrado, etiqueta, datos);
        return Encoding.UTF8.GetString(datos);
    }

    private void ExigirClave()
    {
        if (_clave.Length == 0)
            throw new InvalidOperationException("Falta Google__ClaveCifradoBase64: no se pueden cifrar secretos.");
    }
}
