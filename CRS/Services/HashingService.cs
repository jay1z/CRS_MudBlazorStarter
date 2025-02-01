using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using static CRS.Helpers.LinkHelper;

namespace CRS.Services {
    public class HashingService {
        private byte[]? _key;
        public string HashId(int id, HashType hashType) {
            _key = Encoding.UTF8.GetBytes(hashType.ToString());

            using (var hmac = new HMACSHA256(_key)) {
                var hash = hmac.ComputeHash(BitConverter.GetBytes(id));
                return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
            }
        }

        public int UnhashId(HashType hashType, string hashedId) {
            _key = Encoding.UTF8.GetBytes(hashType.ToString());

            hashedId = hashedId.Replace('-', '+').Replace('_', '/');
            switch (hashedId.Length % 4) {
                case 2: hashedId += "=="; break;
                case 3: hashedId += "="; break;
            }

            var hashBytes = Convert.FromBase64String(hashedId);
            using (var hmac = new HMACSHA256(_key)) {
                for (int i = 0; i < int.MaxValue; i++) {
                    var hash = hmac.ComputeHash(BitConverter.GetBytes(i));
                    if (hashBytes.SequenceEqual(hash)) {
                        return i;
                    }
                }
            }

            throw new ArgumentException("Invalid hash");
        }
        public bool TryUnhashId(HashType hashType, string hashedId, out int id) {
            id = 0;
            _key = Encoding.UTF8.GetBytes(hashType.ToString());

            hashedId = hashedId.Replace('-', '+').Replace('_', '/');
            switch (hashedId.Length % 4) {
                case 2: hashedId += "=="; break;
                case 3: hashedId += "="; break;
            }

            var hashBytes = Convert.FromBase64String(hashedId);
            using (var hmac = new HMACSHA256(_key)) {
                for (int i = 0; i < int.MaxValue; i++) {
                    var hash = hmac.ComputeHash(BitConverter.GetBytes(i));
                    if (hashBytes.SequenceEqual(hash)) {
                        id = i;
                        return true;
                    }
                }
            }
            //throw new ArgumentException("Invalid hash");
            return false;
        }
    }
}
