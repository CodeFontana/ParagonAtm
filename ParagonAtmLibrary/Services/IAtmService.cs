using ParagonAtmLibrary.Models;

namespace ParagonAtmLibrary.Services;
public interface IAtmService
{
    Task<bool> ChangeOperatorSwitchAsync(bool supervisor);
    Task<bool> EnterDieboldSupervisorModeAsync();
    Task<bool> ExitDieboldSupervisorModeAsync();
    Task<string> GetDeviceStateAsync(DeviceModel device);
    Task<PinpadKeysModel> GetPinpadKeysAsync(string pinpadName);
    Task<List<AtmServiceModel>> GetServicesAsync();
    Task<bool> InsertCardAsync(CardModel insertCard);
    Task<bool> InsertMediaAsync(MediaModel insertMedia);
    Task<AtmService.SwitchState> OperatorSwitchStatusAsync(string deviceName);
    Task<bool> PressKeyAsync(PressKeyModel keyPress);
    Task<bool> PressTtuKeyAsync(PressTtuKeyModel keyPress);
    Task<bool> PushOperatorSwitchAsync();
    Task<bool> RecoverAsync();
    Task<AtmService.AuditData> TakeCardAsync();
    Task<AtmService.AuditData> TakeMediaAsync(string deviceName, int count);
    Task<ReceiptModel> TakeReceiptAsync(string deviceName);
}