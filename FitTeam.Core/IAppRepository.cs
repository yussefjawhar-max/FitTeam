namespace FitTeam.Core;

/// <summary>Model katmanının kalıcı depolama sözleşmesi. Arayüz, veritabanı sürücüsünü bilmez.</summary>
public interface IAppRepository
{
    string StorageId { get; }
    AppData? Load();
    void Save(AppData data);
    User? FindUser(string identifier);
}

public sealed class PersistenceException(string message, Exception? inner = null) : Exception(message, inner);
