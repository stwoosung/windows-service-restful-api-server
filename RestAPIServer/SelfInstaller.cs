using System.Configuration.Install;
using System.Reflection;

namespace RestAPIServer
{
    internal class SelfInstaller
    {
        private static readonly string _exePath = Assembly.GetExecutingAssembly().Location;
        
        public static bool InstallMe()
        {
            try
            {
                ManagedInstallerClass.InstallHelper(new string[] {
                    SelfInstaller._exePath
                });
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool UninstallMe()
        {
            try
            {
                ManagedInstallerClass.InstallHelper(new string[] {
                    "/u", SelfInstaller._exePath
                });
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
