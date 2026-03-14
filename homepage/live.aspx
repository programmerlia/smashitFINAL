 <%@ Page Title="Court" Language="C#" MasterPageFile="~/homepage/homepage.master" AutoEventWireup="true" CodeBehind="live.aspx.cs" Inherits="Smash_IT.homepage.live" EnableEventValidation="false" %>

<%@ Register Src="~/Controls/CourtLive.ascx" TagPrefix="uc" TagName="CourtLive" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <uc:CourtLive ID="CourtLive1" runat="server" />
</asp:Content>