<%@ Page Title="Live Courts" Language="C#" AutoEventWireup="true" CodeBehind="live_courts.aspx.cs" Inherits="Smash_IT.live_courts" EnableEventValidation="false" %>
<%@ Register Src="~/controls/CourtLive.ascx" TagPrefix="uc" TagName="CourtLive" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="UTF-8" />
    <title>Live Courts</title>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>

        <uc:CourtLive ID="CourtLive1" runat="server" />
    </form>
</body>
</html>