namespace ConsoleUI.Interfaces;

public interface ITransactionService
{
    bool LoadFromJson<T>(string folderPath, List<T> itemList);
    bool LoadUserData();
    Task RunPlaylists();
}