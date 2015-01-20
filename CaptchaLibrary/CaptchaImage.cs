using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace CaptchaLibrary
{


/// <summary>
/// Summary description for CaptchaImage
/// </summary>
public class CaptchaImage
{
    private int _height;
    private int _width;
    private readonly Random _rand;
    private readonly DateTime _generatedAt;
    private string _randomText;
    private int _randomTextLength;
    private string _randomTextChars;
    private string _fontFamilyName;
    private FontWarpFactor _fontWarp;
    private BackgroundNoiseLevel _backgroundNoise;
    private LineNoiseLevel _lineNoise;
    private string _guid;
    private string _fontWhitelist;

	public CaptchaImage()
	{
		_rand = new Random();
	    _fontWarp = FontWarpFactor.Low;
	    _backgroundNoise = BackgroundNoiseLevel.Low;
	    _lineNoise = LineNoiseLevel.None;
	    _width = 180;
	    _height = 50;
	    _randomTextLength = 5;
	    _randomTextChars = "ACDEFGHJKLNPQRTUVXYZ2346789";
	    _fontFamilyName = "";
        // a list of known good fonts in on both Windows XP and Windows Server 2003
        _fontWhitelist = "arial;arial black;comic sans ms;courier new;estrangelo edessa;franklin gothic medium;" +
                        "georgia;lucida console;lucida sans unicode;mangal;microsoft sans serif;palatino linotype;" +
	                    "sylfaen;tahoma;times new roman;trebuchet ms;verdana";

	    _randomText = GenerateRandomText();
	    _generatedAt = DateTime.Now;
	    _guid = Guid.NewGuid().ToString();

	}

    public enum FontWarpFactor
    {
         None,
        Low,
        Medium,
        High,
        Extreme,
    }

    public enum BackgroundNoiseLevel
    {
        None,
        Low,
        Medium,
        High,
        Extreme
    }

    public enum LineNoiseLevel
    {
        None,
        Low,
        Medium,
        High,
        Extreme
    }

    public string Uniqueid
    {
        get { return _guid; }
    }

    public DateTime RenderedAt
    {
        get { return _generatedAt; }
    }

    public string Font
    {
        get { return _fontFamilyName; }
        set
        {
            //Font font = new Font(value, 12.0!);
            _fontFamilyName = value;
            //font.Dispose();
        }
    }

    public FontWarpFactor FontWarp {
        get { return _fontWarp; }
        set { _fontWarp = value; }
    }

    public BackgroundNoiseLevel BackgroundNoise {
        get { return _backgroundNoise; }
        set { _backgroundNoise = value; }
    }

    public LineNoiseLevel LineNoise {
        get { return _lineNoise; }
        set { _lineNoise = value; }
    }

    public String TextChars {
        get { return _randomTextChars; }
        set { _randomTextChars = value;
            _randomText = GenerateRandomText();
        }
    }

    public int TextLength {
        get { return _randomTextLength; }
        set { _randomTextLength = value;
            _randomText = GenerateRandomText();
        }
    }

    public string Text
    {
        get { return _randomText; }
    }

    /// <summary>
    /// Width of Captcha image to generate, in pixels 
    /// </summary>
    public int Width {
        get { return _width; }
        set {
            if (value <= 60)
            {
                throw new ArgumentOutOfRangeException("width", value, "width must be greater than 60");
            }
            _width = value;
        }
    }

    /// <summary>
    /// Width of Captcha image to generate, in pixels 
    /// </summary>
    public int Height {
        get { return _height; }
        set {
            if (value <= 30)
            {
                throw new ArgumentOutOfRangeException("height", value, "height must be greater than 30");
            }
            _height = value;
        }
    }

    /// <summary>
    /// A semicolon-delimited list of valid fonts to use when no font is provided
    /// </summary>
    public string FontWhitelist {
        get { return _fontWhitelist; }
        set { _fontWhitelist = value; }
    }

    public Bitmap RenderImage()
    {
        return GenerateImagePrivate();
    }


    private string RandomFontFamily()
    {
        string[] ff = _fontWhitelist.Split(';');
        return ff[_rand.Next(0, ff.Length)];
        
    }

    private string GenerateRandomText()
    {
        StringBuilder sb = new StringBuilder(_randomTextLength);
        int maxLength = _randomTextChars.Length;
        for (int n = 0; n <= _randomTextLength - 1; n++)
        {
            sb.Append(_randomTextChars.Substring(_rand.Next(maxLength), 1));
        }
        return sb.ToString();

    }

    private PointF RandomPoint(int xmin, int xmax, int ymin, int ymax)
    {
        return new PointF(_rand.Next(xmin, xmax), _rand.Next(ymin, ymax));
    }

    //Returns a random point within the specified rectangle
    private PointF RandomPoint(Rectangle rect)
    {
        return RandomPoint(rect.Left, rect.Width, rect.Top, rect.Bottom);
    }


    private GraphicsPath TextPath(char s, Font f, Rectangle rectangle)
    {
        StringFormat sf = new StringFormat();
        sf.Alignment = StringAlignment.Near;
        sf.LineAlignment = StringAlignment.Near;

        GraphicsPath gp = new GraphicsPath();
        gp.AddString(s.ToString(), f.FontFamily, (int) f.Style, f.Size, rectangle, sf);
        return gp;
    }

    //Returns the CAPTCHA font in an appropriate size 
    private Font GetFont()
    {
        float fsize = 0;
        string fname = _fontFamilyName;

        if(fname == "")
        {
            fname = RandomFontFamily();
        }

        switch (this.FontWarp)
        {
                case FontWarpFactor.Extreme:
                fsize = Convert.ToInt32(_height*0.95);
                break;

                case FontWarpFactor.High:
                fsize = Convert.ToInt32(_height*0.9);
                break;
                case FontWarpFactor.Low:
                fsize = Convert.ToInt32(_height*0.8);
                break;
                case FontWarpFactor.Medium:
                fsize = Convert.ToInt32(_height*0.85);
                break;
                case FontWarpFactor.None:
                fsize = Convert.ToInt32(_height*0.7);
                break;
        }

        return new Font(fname, fsize, FontStyle.Bold);
    }

    //Renders the CAPTCHA image
    private Bitmap GenerateImagePrivate()
    {
        Font fnt = null;
        Rectangle rect;
        Brush br;
        Bitmap bmp = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
        Graphics gr = Graphics.FromImage(bmp);
        gr.SmoothingMode = SmoothingMode.AntiAlias;

        //fill an empty white rectangle
        rect = new Rectangle(0, 0, _width, _height);
        br = new SolidBrush(Color.White);
        gr.FillRectangle(br, rect);

        int charOffset = 0;
        Double charWidth = _width/_randomTextLength;
        Rectangle rectChar;

        foreach (Char c in _randomText)
        {
            //establish font and draw area
            fnt = GetFont();
            rectChar = new Rectangle(Convert.ToInt32(charOffset*charWidth), 0, Convert.ToInt32(charWidth), _height);

            //warp the character
            GraphicsPath gp = TextPath(c, fnt, rectChar);
            WarpText(gp, rectChar);

            //draw the character
            br = new SolidBrush(Color.Black);
            gr.FillPath(br, gp);

            charOffset += 1;
        }

        AddNoise(gr, rect);
        AddLine(gr, rect);

        //clean up unmanaged resources
        fnt.Dispose();
        br.Dispose();
        gr.Dispose();

        return bmp;
    }


    //Warp the provided text GraphicsPath by a variable amount
    private void WarpText(GraphicsPath textPath, Rectangle rectangle)
    {
        double WarpDivisor = 0;
        double RangeModifier = 0;

        switch (this._fontWarp)
        {
            case FontWarpFactor.Extreme:
                WarpDivisor = 4;
                RangeModifier = 1.5;
                break;
            case FontWarpFactor.High:
                WarpDivisor = 4.5;
                RangeModifier = 1.4;
                break;
                case FontWarpFactor.Low:
                WarpDivisor = 6;
                RangeModifier = 1;
                break;
                case FontWarpFactor.Medium:
                WarpDivisor = 5;
                RangeModifier = 1.3;
                break;
                case FontWarpFactor.None:
                return;
        }


        RectangleF rectF = new RectangleF(Convert.ToSingle(rectangle.Left), 0, Convert.ToSingle(rectangle.Width), rectangle.Height);

        int hrange = Convert.ToInt32(rectangle.Height/WarpDivisor);
        int wrange = Convert.ToInt32(rectangle.Width/WarpDivisor);
        int left = rectangle.Left - Convert.ToInt32(wrange*RangeModifier);
        int top = rectangle.Top - Convert.ToInt32(hrange*RangeModifier);
        int width = rectangle.Left + rectangle.Width + Convert.ToInt32(wrange*RangeModifier);
        int height = rectangle.Top + rectangle.Height + Convert.ToInt32(hrange*RangeModifier);

        if (left < 0) left = 0;
        if (top < 0) top = 0;
        if (width > this.Width) width = this.Width;
        if (height > this.Height) height = this.Height;

        PointF leftTop = RandomPoint(left, left + wrange, top, top + hrange);
        PointF rightTop = RandomPoint(width - wrange, width, top, top + hrange);
        PointF leftBottom = RandomPoint(left, left + wrange, height - hrange, height);
        PointF rightBottom = RandomPoint(width - wrange, width, height - hrange, height);

        PointF[] points = new PointF[] { leftTop, rightTop, leftBottom, rightBottom };
        Matrix m = new Matrix();
        m.Translate(0, 0);
        textPath.Warp(points, rectF, m, WarpMode.Perspective, 0);
    }

    //Add a variable level of graphic noise to the image
    private void AddNoise(Graphics graphics1, Rectangle rect)
    {
        int density = 0;
        int size = 0;

        switch (_backgroundNoise)
        {
                case BackgroundNoiseLevel.Extreme:
                density = 12;
                size = 38;
                break;
                case BackgroundNoiseLevel.High:
                density = 16;
                size = 39;
                break;
                case BackgroundNoiseLevel.Low:
                density = 30;
                size = 40;
                break;
                case BackgroundNoiseLevel.Medium:
                density = 18;
                size = 40;
                break;
                case BackgroundNoiseLevel.None:
                return;
        }

        SolidBrush br = new SolidBrush(Color.Black);
        int max = Convert.ToInt32(Math.Max(rect.Width, rect.Height)/size);


        for (int i = 0; i <= Convert.ToInt32((rect.Width*rect.Height)/density); i++)
        {
            graphics1.FillEllipse(br, _rand.Next(rect.Width), _rand.Next(rect.Height), _rand.Next(max), _rand.Next(max));
        }

        
        br.Dispose();
    }

    //Add variable level of curved lines to the image
    private void AddLine(Graphics graphics, Rectangle rectangle)
    {
        int length = 0;
        Single width = 0;
        int linecount = 0;

        switch (this._lineNoise)
        {
                case LineNoiseLevel.Extreme:
                length = 3;
                width = Convert.ToSingle(_height/22.7272);  // 2.2
                linecount = 3;
                break;
                case LineNoiseLevel.High:
                length = 3;
                width = Convert.ToSingle(_height/25); // 2.0
                linecount = 2;
                break;
                case LineNoiseLevel.Low:
                length = 4;
                width = Convert.ToSingle(_height/31.25); // 1.6
                linecount = 1;
                break;
                case LineNoiseLevel.Medium:
                length = 5;
                width = Convert.ToSingle(_height/27.7777); // 1.8
                linecount = 1;
                break;
                case LineNoiseLevel.None:
                return;
        }

        PointF[] pf = new PointF[length];
        Pen p = new Pen(Color.Black, width);

        for (int l = 0; l <= linecount; l++)
        {
            for (int i = 0; i <= length; i++)
            {
                pf[i] = RandomPoint(rectangle);
            }

            float tension = (float)1.75;
            graphics.DrawCurve(p, pf, tension);

        }

        p.Dispose();
    }

}


}