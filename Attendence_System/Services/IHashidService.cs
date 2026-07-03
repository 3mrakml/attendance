using HashidsNet;

namespace Attendence_System.Services
{
    public interface IHashidService
    {
        string Encode(int id);
        int Decode(string hash);
        int? DecodeNullable(string hash);
    }
}
