using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
//using System.Web.Routing;
using io = System.IO;

namespace XTools {
    public class GooglebotHelperController : Controller {

        public ActionResult Index() {
            return Content(Request.RawUrl);
        } // end method

    } // end class
} // end namespace