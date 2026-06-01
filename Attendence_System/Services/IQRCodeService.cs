namespace Attendence_System.Services
{
    public interface IQRCodeService
    {
        string GenerateQRCode(string data);
    }
}
