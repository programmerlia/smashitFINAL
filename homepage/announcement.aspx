<%@ Page Title="Announcements" Language="C#" MasterPageFile="~/homepage/homepage.Master" AutoEventWireup="true" CodeBehind="announcement.aspx.cs" Inherits="Smash_IT.homepage.announcement" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/announcement.css?v=" + DateTime.Now.Ticks) %>' />
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <section class="announcement-hero">
        <div class="hero-overlay">
            <h1>Daily Announcements</h1>
        </div>
    </section>

    <section class="announcement-feed container">
        
        <asp:Repeater ID="rptAnnouncements" runat="server">
            <ItemTemplate>
                <%-- Logic to add 'reverse' class to every second item for the zig-zag effect --%>
                <div class='<%# Container.ItemIndex % 2 != 0 ? "announcement-item-wrapper reverse" : "announcement-item-wrapper" %>'>
                    
                    <div class="anno-text-side">
                        <span class="anno-date">
                            <i class="far fa-calendar-alt"></i> <%# Eval("CreatedAt", "{0:MMMM dd, yyyy}") %>
                        </span>

                        <h3><%# Eval("Title") %></h3>
                        
                        <div class="anno-desc">
                            <%# Eval("Content") %>
                        </div>

                        <a href="Reservations.aspx" class="btn-apply-now">Apply Now</a>
                    </div>

                    <div class="anno-image-side">
                        <div class="image-container">
                            <img src='<%# GetBase64Image(Eval("ImageFIleData"), Eval("ImageFileType")) %>' alt='<%# Eval("Title") %>' />
                        </div>
                    </div>

                </div>
            </ItemTemplate>
        </asp:Repeater>

    </section>

</asp:Content>