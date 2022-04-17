namespace ConsoleUI.Services;

public interface ITransactionService
{
    bool LoadFromJson<T>(string folderPath, List<T> itemList);
    bool LoadUserData();
    void RunPlaylists();
}