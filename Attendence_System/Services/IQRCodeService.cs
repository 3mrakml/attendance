namespace Attendence_System.Services
{
    public interface IQRCodeService
    {
        string GenerateQRCode(string data);
        string GenerateBarcode(string data);
    }
}
