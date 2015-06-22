using System.Net;
using System.Security;

namespace Trik.Upload_Extension
{
    internal class TargetProfile
    {
        public TargetProfile(IPAddress ip, string login, SecureString pass)
        {
            IpAddress = ip;
            Login = login;
            Pass = pass;
        }

        public TargetProfile(IPAddress ip, string login) : this(ip, login, new SecureString())
        {
        }

        public TargetProfile(IPAddress ip) : this(ip, "root")
        {
        }

        public IPAddress IpAddress { get; private set; }
        public string Login { get; private set; }
        public SecureString Pass { get; private set; }
    }
}
