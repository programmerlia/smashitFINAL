<%@ Page Language="C#" AutoEventWireup="true"
    MasterPageFile="~/homepage/homepage.Master"
    CodeBehind="about.aspx.cs"
    Inherits="Smash_IT.homepage.abouut" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/about-page.css") %>' />
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <section class="about-hero">
        <div class="hero-bg">
            <asp:Image ID="imgHero" runat="server" CssClass="hero-img" ImageUrl="~/images/placeholder.jpg" />
            <div class="hero-overlay"></div>
        </div>

        <div class="hero-content">
            <h1 class="hero-title">
                <asp:Literal ID="litHeroTitle" runat="server" Text="ABOUT US"></asp:Literal>
            </h1>
            <div class="hero-divider"></div>
        </div>
    </section>

    <section class="story-section about-container">
        <div class="story-grid">
            
            <div class="story-text-wrapper">
                <h2 class="story-heading">Our Story</h2>
                <div class="story-text">
                    <asp:Literal ID="litStoryContent" runat="server" Text="Our story will appear here."></asp:Literal>
                </div>
            </div>

            <div class="story-img-wrapper">
                <div class="story-backdrop"></div>
                <asp:Image ID="imgStory" runat="server" CssClass="story-img" ImageUrl="~/images/placeholder.jpg" />
            </div>

        </div>
    </section>

    <section class="mv-section">
        <div class="mv-grid about-container">
            
            <div class="mv-card">
                <h3 class="mv-heading">Mission</h3>
                <p class="mv-text">
                    <asp:Literal ID="litMission" runat="server" Text="Our mission statement."></asp:Literal>
                </p>
            </div>

            <div class="mv-card">
                <h3 class="mv-heading">Vision</h3>
                <p class="mv-text">
                    <asp:Literal ID="litVision" runat="server" Text="Our vision statement."></asp:Literal>
                </p>
            </div>

        </div>
    </section>

    <section class="members-section about-container">
        <h2 class="members-heading">Meet Our Team</h2>
        
        <div class="members-grid">
            <asp:Repeater ID="rptMembers" runat="server">
                <ItemTemplate>
                    <div class="member-card">
                        <img src='<%# GetBase64Image(Eval("ImgPath")) %>' class="member-img" />
                        <h4 class="member-name"><%# Eval("Title") %></h4>
                        <p class="member-role"><%# Eval("Subtitle") %></p>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </div>
    </section>

    <script src='<%= ResolveUrl("~/js/script.js") %>'></script>

</asp:Content>