using DinnerPicker.Models;

namespace DinnerPicker.Services;

public interface IScanService
{
    Task<ScanResult> ScanImageAsync(Stream imageStream, string contentType);
}
