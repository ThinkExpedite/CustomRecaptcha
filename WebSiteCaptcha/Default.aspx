<%--<%@ Register TagPrefix="cc1" Namespace="WebControlCaptcha" Assembly="WebControlCaptcha" %>--%>
<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" EnableSessionState="False" %>

<%@ Register Src="~/Recaptcha.ascx" TagPrefix="uc1" TagName="Recaptcha" %>


<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>CAPTCHA demo page</title>
</head>
<body>
    <form id="form1" runat="server" enableviewstate="false">
        <div>
            <uc1:Recaptcha runat="server" ID="recaptchaControl" CaptchaLength="3" FontWarp="Extreme" BackgroundNoise="Extreme"  />

            <asp:ValidationSummary ID="ValidationSummary1" runat="server" />
            <p></p>
            
            <br />
            <asp:Button ID="Button1" runat="server" Text="Submit" />
        </div>
    </form>
</body>
</html>
