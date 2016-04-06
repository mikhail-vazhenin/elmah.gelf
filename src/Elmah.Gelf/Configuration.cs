using System.Configuration;

namespace Elmah.Gelf
{

    internal static class Configuration
    {
        internal const string GroupName = "elmah";
        internal const string GroupSlash = GroupName + "/";

        public static object GetSubsection(string name)
        {
            return GetSection(GroupSlash + name);
        }

        public static object GetSection(string name)
        {
            return ConfigurationManager.GetSection(name);
        }
    }
}
