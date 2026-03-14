<%@ Page Title="Queue" Language="C#" MasterPageFile="~/receptionistpage/receptionist.master" AutoEventWireup="true" CodeBehind="rec_live_courts.aspx.cs" Inherits="Smash_IT.receptionistpage.rec_live_courts" EnableEventValidation="false" %>
<%@ Register Src="~/Controls/CourtLive.ascx" TagPrefix="uc" TagName="CourtLive" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <uc:CourtLive ID="CourtLive1" runat="server" />
</asp:Content>