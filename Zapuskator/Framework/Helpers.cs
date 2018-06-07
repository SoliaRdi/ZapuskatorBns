using System.Security.Principal;
using ToastNotifications;
using Zapuskator.ViewModels;

namespace STO.Framework
{
    public static class Helpers
    {
        public static bool ContainsAny(this string haystack, params string[] needles)
        {
            foreach (var needle in needles)
                if (haystack.Contains(needle))
                    return true;

            return false;
        }
        public static bool EqualsAny(this string haystack, params string[] needles)
        {
            foreach (var needle in needles)
                if (haystack==needle)
                    return true;

            return false;
        }


        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
