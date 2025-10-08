namespace Ntk.NumberPlate.Hub.Api.Services;

public interface IImageStorageService
{
    Task<string> SaveImageAsync(byte[] imageData, string fileName);
    Task<byte[]?> GetImageAsync(string fileName);
    Task<bool> DeleteImageAsync(string fileName);
}


