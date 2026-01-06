using System;

namespace MRP_Server.Http.Helpers
{
    public static class RouteHelper
    {
        public static bool TryGetIdAfterPrefix(string path, string prefix, out int id)
        {
            id = 0;
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;

            var rest = path.Substring(prefix.Length).Trim('/');
            if (string.IsNullOrWhiteSpace(rest)) return false;

            if (rest.Contains("/")) return false;

            return int.TryParse(rest, out id) && id > 0;
        }


        public static bool TryGetIdBeforeSuffix(string path, string prefix, string suffix, out int id)
        {
            id = 0;
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            if (!path.EndsWith("/" + suffix, StringComparison.OrdinalIgnoreCase)) return false;

            var core = path.Substring(prefix.Length);
            core = core.Substring(0, core.Length - (suffix.Length + 1)).Trim('/'); 
            return int.TryParse(core, out id) && id > 0;
        }
    }
}
