using System.Security.Cryptography;
using System.Text;
namespace FitTeam.Core;

public interface IAccountPasswordProtector
{
    string Protect(Guid id,string password);
    string? Unprotect(Guid id,string value);
}
public sealed class LocalAccountPasswordProtector(string keyPath):IAccountPasswordProtector
{
    private byte[] Key(bool create)
    {
        if(!File.Exists(keyPath))
        {
            if(!create)throw new FileNotFoundException();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(keyPath))!);
            try
            {
                using var stream=new FileStream(keyPath,FileMode.CreateNew,FileAccess.Write,FileShare.None);stream.Write(RandomNumberGenerator.GetBytes(32));
                if(!OperatingSystem.IsWindows())File.SetUnixFileMode(keyPath,UnixFileMode.UserRead|UnixFileMode.UserWrite);
            }
            catch(IOException) when(File.Exists(keyPath)){ }
        }
        var key=File.ReadAllBytes(keyPath);if(key.Length!=32)throw new InvalidDataException("Şifre koruma anahtarı okunamadı.");return key;
    }
    public string Protect(Guid id,string password)
    {
        var nonce=RandomNumberGenerator.GetBytes(12);var tag=new byte[16];var plain=Encoding.UTF8.GetBytes(password);var cipher=new byte[plain.Length];var key=Key(true);
        try{using var aes=new AesGcm(key,16);aes.Encrypt(nonce,plain,cipher,tag,id.ToByteArray());return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());}
        finally{CryptographicOperations.ZeroMemory(plain);CryptographicOperations.ZeroMemory(key);}
    }
    public string? Unprotect(Guid id,string value)
    {
        byte[]? plain=null,key=null;
        try
        {
            var bytes=Convert.FromBase64String(value);if(bytes.Length<29)return null;plain=new byte[bytes.Length-28];key=Key(false);
            using var aes=new AesGcm(key,16);aes.Decrypt(bytes.AsSpan(0,12),bytes.AsSpan(28),bytes.AsSpan(12,16),plain,id.ToByteArray());return Encoding.UTF8.GetString(plain);
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or CryptographicException or FormatException){return null;}
        finally{if(plain!=null)CryptographicOperations.ZeroMemory(plain);if(key!=null)CryptographicOperations.ZeroMemory(key);}
    }
}
public sealed partial class FitService
{
    public string? AdminReadPasswordCopy(Guid id)
    {
        if(!HasFullAdministrationView)throw new InvalidOperationException("Yetkili hesabıyla giriş yap.");var user=Data.Users.FirstOrDefault(u=>u.Id==id)??throw new ArgumentException("Kişi bulunamadı.");return Passwords.ReadCopy(user);
    }
}
