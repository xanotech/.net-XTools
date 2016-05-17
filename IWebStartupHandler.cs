using System.Web;

namespace XTools {
    public interface IWebStartupHandler {

        void HandleStartup(HttpApplication application);

    } // end class
} // end namespace
