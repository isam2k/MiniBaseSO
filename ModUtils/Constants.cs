using System.IO;
using System.Reflection;

namespace ModUtils
{
    public static class Constants
    {
        public static readonly string ModPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }
}