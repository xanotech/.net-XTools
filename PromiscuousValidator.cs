using System.Web;
using System.Web.Util;

namespace XTools {
    
    /// <summary>
    ///   Get rid of so called "potentially dangerous Request.Form value" errors
    ///   once and for all with PromiscuousValidator.  Simply activate by adding
    ///   the following to the Web.config file...
    ///   <code>
    ///     &lt;system.web&gt;
    ///         &lt;httpRuntime requestValidationType="XTools.PromiscuousValidator"/&gt;
    ///     &lt;/system.web&gt;
    ///   </code>
    /// </summary>
    public class PromiscuousValidator : RequestValidator {

        protected override bool IsValidRequestString(HttpContext context, string value,
            RequestValidationSource requestValidationSource, string collectionKey, out int validationFailureIndex) {
            validationFailureIndex = 0;
            return true;
        } // end method

    } // end class
} // end namespace