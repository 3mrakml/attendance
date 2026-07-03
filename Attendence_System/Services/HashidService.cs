using HashidsNet;

namespace Attendence_System.Services
{
    public class HashidService : IHashidService
    {
        private readonly IHashids _hashids;

        public HashidService(IConfiguration config)
        {
            // نستخدم Salt فريد لضمان أن التشفير الخاص بك لا يمكن فكه حتى لو عرفوا الخوارزمية
            var salt = config["Hashids:Salt"] ?? "Attendance_Super_Secret_Salt_2026!";
            var minLength = config.GetValue<int>("Hashids:MinLength", 6); // طول الكود المشفر (مثلاً 6 حروف J9xLp2)
            _hashids = new Hashids(salt, minLength);
        }

        public string Encode(int id)
        {
            return _hashids.Encode(id);
        }

        public int Decode(string hash)
        {
            if (string.IsNullOrEmpty(hash)) return 0;
            
            var decoded = _hashids.Decode(hash);
            return decoded.Length > 0 ? decoded[0] : 0;
        }

        public int? DecodeNullable(string hash)
        {
            if (string.IsNullOrEmpty(hash)) return null;

            var decoded = _hashids.Decode(hash);
            return decoded.Length > 0 ? decoded[0] : null;
        }
    }
}
