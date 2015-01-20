using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Reflection.Emit;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Web.UI;
using System.Web.UI.WebControls;
using CaptchaImage = CaptchaLibrary.CaptchaImage;

public partial class Recaptcha : System.Web.UI.UserControl, IPostBackDataHandler, IValidator
{
    public enum Layout
    {
        Horizontal,
        Vertical
    }

    public enum CacheType
    {
        HttpRuntime,
        Session
    }


    private int _timeoutSecondsMax = 90;
    private int _timeoutSecondsMin = 3;
    private bool _userValidated = true;
    private string _text = "Enter the code shown:";
    private string _font = "";
    private CaptchaImage _captcha = new CaptchaImage();

    private Layout _layoutStyle = Layout.Horizontal;
    private string _prevguid;
    private string _errorMessage = "";
    private CacheType _cacheStrategy = CacheType.HttpRuntime;


    //Message to display in a Validation Summary when the CAPTCHA fails to validate.
    public string ErrorMessage
    {
        get
        {
            if (!_userValidated)
            {
                return _errorMessage;
            }
            return string.Empty;
        }
        set { _errorMessage = value; }
    }

   public bool IsValid
    {
        get { return _userValidated; }
        set { _userValidated = value; }
    }

    public void Validate()
    {
        //a no-op, since we validate in LoadPostData
    }

    /// <summary>
    /// Returns True if the user was CAPTCHA validated after a postback.
    /// </summary>
    public bool UserValidated
    {
        get { return _userValidated; }
    }

    public bool Enabled
    {
        get { return true; } //this.Enabled; }
        set
        {
            this.Enabled = value; 
            //When a validator is disabled, generally, the intent is not to make the page invalid for that round trip.

            if (!value)
            {
                _userValidated = true;
            }
            
        }
    }


#region Public Properties

    //Instructional text displayed next to CAPTCHA image.
    //DefaultValue("Enter the code shown above:")
    public string Text
    {
        get { return _text; }
        set { _text = value; }
    }

    //Determines if image and input area are displayed horizontally, or vertically.
    public Layout LayoutStyle
    {
        get { return _layoutStyle; }
        set { _layoutStyle = value; }
    }

    //Determines if CAPTCHA codes are stored in HttpRuntime (fast, but local to current server) or Session (more portable across web farms
    public CacheType CacheStrategy
    {
        get { return (CacheType) _cacheStrategy; }
        set { _cacheStrategy = (CacheType) value; }
    }

    //Font used to render CAPTCHA text. If font name is blank, a random font will be chosen.
    public string CaptchaFont
    {
        get { return _font; }
        set 
        {
            _font = value;
            _captcha.Font = _font;
        }
    }

    //Characters used to render CAPTCHA text. A character will be picked randomly from the string.
    public string CaptchaChars
    {
        get { return _captcha.TextChars; }
        set 
        {
           _captcha.TextChars = value;
        }
    }

    //Number of CaptchaChars used in the CAPTCHA text
    public int CaptchaLength
    {
        get { return _captcha.TextLength; }
        set 
        {
           _captcha.TextLength = value;
        }
    }


    public int CaptchaMinTimeout
    {
        get { return this._timeoutSecondsMin; }
        set
        {
            if (value > 15)
            {
                throw new ArgumentOutOfRangeException("CaptchaTimeout", "Timeout must be less than 15 seconds. Humans aren't that slow!");
            }
            _timeoutSecondsMin = value;
        }
    }

    public int CaptchaMaxTimeout
    {
        get { return this._timeoutSecondsMax; }
        set
        {
            if (value < 15 && value != 0)
            {
                throw new ArgumentOutOfRangeException("CaptchaTimeout", "Timeout must be greater than 15 seconds.");
            }
            _timeoutSecondsMax = value;
        }
    }

    //Height of generated CAPTCHA image.
    public int CaptchaHeight
    {
        get { return this._captcha.Height; }
        set
        {
            _captcha.Height = value;
        }
    }

    //Width of generated CAPTCHA image.
    public int CaptchaWidth
    {
        get { return this._captcha.Width; }
        set
        {
            _captcha.Width = value;
        }
    }

    //Amount of random font warping used on the CAPTCHA text
    public CaptchaImage.FontWarpFactor CaptchaFontWarping
    {
        get { return this._captcha.FontWarp; }
        set
        {
            _captcha.FontWarp = value;
        }
    }

    //Amount of background noise to generate in the CAPTCHA image
    public CaptchaImage.BackgroundNoiseLevel CaptchaBackgroundNoise
    {
        get { return this._captcha.BackgroundNoise; }
        set
        {
            _captcha.BackgroundNoise = value;
        }
    }

     //Add line noise to the CAPTCHA image
    public CaptchaImage.LineNoiseLevel CaptchaLineNoise
    {
        get { return this._captcha.LineNoise; }
        set
        {
            _captcha.LineNoise = value;
        }
    }

#endregion

    private CaptchaImage GetCachedCaptcha(string guid)
    {
        if (this._cacheStrategy == CacheType.HttpRuntime)
        {
            return (CaptchaImage) HttpRuntime.Cache.Get(guid);
        }
        else
        {
            return (CaptchaImage)HttpContext.Current.Session[guid];
        }
    }

    private void RemoveCachedCaptcha(string guid)
    {
        if (this._cacheStrategy == CacheType.HttpRuntime)
        {
            HttpRuntime.Cache.Remove(guid);
        }
        else
        {
            HttpContext.Current.Session.Remove(guid);
        }
    }

    //Validate the user's text against the CAPTCHA text
    private void ValidateCaptcha(string userEntry)
    {
        if (!Visible || !Enabled)
        {
            _userValidated = true;
            return;
        }
        
        //retrieve the previous captcha from the cache to inspect its properties
        CaptchaImage ci = GetCachedCaptcha(_prevguid);

        if (ci == null)
        {
            this.ErrorMessage = "The code you typed has expired after " + this.CaptchaMaxTimeout + " seconds.";
            _userValidated = false;
            return;
        }

        //was it entered too quickly?
        if (this.CaptchaMinTimeout > 0)
        {
            if (ci.RenderedAt.AddSeconds(this.CaptchaMinTimeout) > DateTime.Now)
            {
                 _userValidated = false;
                ErrorMessage = "Code was typed too quickly. Wait at least " + CaptchaMinTimeout + " seconds.";
                RemoveCachedCaptcha(_prevguid);
                return;
            }
        }

        if(System.String.Compare(userEntry, ci.Text, System.StringComparison.OrdinalIgnoreCase) != 0)
        {
            ErrorMessage = "The code you typed does not match the code in the image.";
            _userValidated = false;
            RemoveCachedCaptcha(_prevguid);
            return;
        }


        _userValidated = true;
        RemoveCachedCaptcha(_prevguid);
    }


    /// <summary>
    /// returns HTML-ized color strings
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    private string HtmlColor(Color color)
    {
        if (color.IsEmpty) return string.Empty;
        if (color.IsNamedColor) return color.ToKnownColor().ToString();
        if (color.IsSystemColor) return color.ToString();
        return "#" + color.ToArgb().ToString("x").Substring(2);
    }

    protected override void Render(HtmlTextWriter writer)
    {
        //base.Render(writer);

        //'-- master DIV
        writer.Write("<div");
        //image DIV/SPAN
        if (this.LayoutStyle == Layout.Vertical)
        {
            writer.Write("<div style='text-align:center;margin:5px;'>");
        }
        else
        {
            writer.Write("<span style='margin:5px;float:left;'>");
        }
        
        //this is the URL that triggers the CaptchaImageHandler
        writer.Write("<img src=\"CaptchaImage.ashx?guid=" + Convert.ToString(_captcha.Uniqueid) + "\"");

        if (CacheStrategy == (CacheType) CacheType.Session)
        {
            writer.Write("&s=1");
        }
        writer.Write(" border='0'");
        writer.Write(" width=" + _captcha.Width);
        writer.Write(" height=" + _captcha.Height);
        writer.Write(">");

        if (this.LayoutStyle == Layout.Vertical)
        {
            writer.Write("</div>");
            //text input and submit button DIV/SPAN
            writer.Write("<div style='text-align:center;margin:5px;'>");
        }
        else
        {
            writer.Write("</span>");
            //text input and submit button DIV/SPAN
            writer.Write("<span style='margin:5px;float:left;'>");

        }

        if (Text.Length > 0)
        {
            writer.Write(Text);
            writer.Write("<br>");
        }

        writer.Write("<input name=" + UniqueID + " type=text size=");
        writer.Write(_captcha.TextLength.ToString());
        writer.Write(" maxlength=" + _captcha.TextLength.ToString(CultureInfo.InvariantCulture));

        if(!Enabled)writer.Write(" disabled=\"disabled\"");
        writer.Write(" value=''>");


        if (this.LayoutStyle == Layout.Vertical)
        {
            writer.Write("</div>");
            
        }
        else
        {
            writer.Write("</span>");
            //text input and submit button DIV/SPAN
            writer.Write("<br clear='all'>");

        }
        //closing tag for master DIV
        writer.Write("</div>");

    }

    /// <summary>
    /// generate a new captcha and store it in the ASP.NET Cache by unique GUID
    /// </summary>
    private void GenerateNewCaptcha()
    {
        if (_cacheStrategy == CacheType.HttpRuntime)
        {
            HttpRuntime.Cache.Add(_captcha.Uniqueid, _captcha, null,
                DateTime.Now.AddSeconds(Convert.ToDouble(this.CaptchaMaxTimeout == 0 ? 90 : this.CaptchaMaxTimeout)),
                TimeSpan.Zero, CacheItemPriority.NotRemovable, null);
        }
        else
        {
            HttpContext.Current.Session.Add(_captcha.Uniqueid, _captcha);
        }
    }

   

    /// <summary>
    /// Retrieve the user's CAPTCHA input from the posted data
    /// </summary>
    /// <param name="postDataKey"></param>
    /// <param name="postCollection"></param>
    /// <returns></returns>
    public bool LoadPostData(string postDataKey, System.Collections.Specialized.NameValueCollection postCollection)
    {
       //Retrieve the user's CAPTCHA input from the posted data
        ValidateCaptcha(Convert.ToString(postCollection[this.UniqueID]));
        return false;
    }

    public void RaisePostDataChangedEvent() 
    {
        throw new NotImplementedException();
    }

    protected override void LoadControlState(object savedState)
    {
        if (savedState != null)
        {
            _prevguid = savedState.ToString();
        }
    }

    protected override object SaveControlState()
    {
        return (object) _captcha.Uniqueid;
    }

    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);
        Page.RegisterRequiresControlState(this);
        Page.Validators.Add(this);
    }


    protected override void OnUnload(EventArgs e)
    {
        if (Page != null)
        {
            Page.Validators.Remove(this);
        }

        base.OnUnload(e);
    }


    protected override void OnPreRender(EventArgs e)
    {
        if (this.Visible)
        {
            GenerateNewCaptcha();
        }
        base.OnPreRender(e);
    }
}