<%@ Page Title="Announcements" Language="C#" MasterPageFile="~/homepage/homepage.Master" AutoEventWireup="true" CodeBehind="announcement.aspx.cs" Inherits="Smash_IT.homepage.announcement" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/announcement.css?v=" + DateTime.Now.Ticks) %>' />
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <section class="announcement-hero">
        <div class="anno-hero-bg">
            <asp:Image ID="AnnoImgHero" runat="server" CssClass="anno-hero-img" ImageUrl="~/images/placeholder.jpg" />
        </div>
        
        <div class="anno-hero-overlay"></div>
        
        <div class="anno-hero-content">
            <h1 class="anno-hero-title">Announcements</h1>
            <div class="anno-hero-divider"></div>
        </div>
    </section>

    <section class="announcement-feed container">
        
        <asp:Repeater ID="rptAnnouncements" runat="server">
            <ItemTemplate>
                <div class='<%# Container.ItemIndex % 2 != 0 ? "announcement-item-wrapper reverse" : "announcement-item-wrapper" %>'>
                    
                    <div class="anno-text-side">
                        <span class="anno-date">
                            <i class="far fa-calendar-alt"></i> <%# Eval("CreatedAt", "{0:MMMM dd, yyyy}") %>
                        </span>

                        <h3><%# Eval("Title") %></h3>
                        
                        <div class="anno-desc">
                            <%# Eval("Content") %>
                        </div>

                        <asp:HyperLink ID="hlApply" runat="server" 
                            NavigateUrl='<%# GetValidUrl(Eval("URL_FB")) %>' 
                            CssClass="btn-apply-now" 
                            Target="_blank"
                            Visible='<%# Eval("URL_FB") != DBNull.Value && !string.IsNullOrWhiteSpace(Eval("URL_FB").ToString()) %>'>
                            Apply Now
                        </asp:HyperLink>
                    </div>

                    <div class="anno-image-side">
                        <div class="image-container">
                            <img src='<%# ResolveImagePath(Eval("FilePath")) %>' alt='<%# Eval("Title") %>' />
                        </div>
                    </div>

                </div>
            </ItemTemplate>
        </asp:Repeater>

    </section>

</asp:Content>