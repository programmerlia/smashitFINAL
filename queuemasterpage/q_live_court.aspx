<%@ Page Title="Queue" Language="C#" MasterPageFile="~/queuemasterpage/queuemaster.master" AutoEventWireup="true" CodeBehind="q_live_court.aspx.cs" Inherits="Smash_IT.queuemasterpage.q_live_court" EnableEventValidation="false" %>
<%@ Register Src="~/Controls/CourtLive.ascx" TagPrefix="uc" TagName="CourtLive" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<div sstyle="display: flex;
justify-content: center;
align-items: center;">
<uc:CourtLive ID="CourtLive1" runat="server" />
</div>
    
</asp:Content>