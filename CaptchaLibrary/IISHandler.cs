using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web;

namespace CaptchaLibrary
{
    public class IisHandler : IHttpHandler
    {
        /// <summary>
        /// You will need to configure this handler in the Web.config file of your 
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: http://go.microsoft.com/?linkid=8101007
        /// </summary>
        #region IHttpHandler Members

        public bool IsReusable
        {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {

            HttpApplication app = context.ApplicationInstance;

            //get the unique GUID of the captcha; this must be passed in via the querystring
            string guid = app.Request.QueryString["guid"];

            CaptchaImage ci = null;

            if (guid != string.Empty)
            {
                if (String.IsNullOrEmpty(app.Request.QueryString["s"]))
                {
                    ci = (CaptchaImage) HttpRuntime.Cache.Get(guid);
                }
                else
                {
                    ci = (CaptchaImage) HttpContext.Current.Session[guid];
                }
            }

            if (ci == null)
            {
                app.Response.StatusCode = 404;
                context.ApplicationInstance.CompleteRequest();
                return;
            }

            //write the image to the HTTP output stream as an array of bytes
            Bitmap b = ci.RenderImage();
            b.Save(app.Context.Response.OutputStream, ImageFormat.Jpeg);
            b.Dispose();
            app.Response.ContentType = "image/jpeg";
            app.Response.StatusCode = 200;
            context.ApplicationInstance.CompleteRequest();
        }

        #endregion
    }
}
