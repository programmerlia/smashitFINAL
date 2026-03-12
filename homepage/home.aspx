<%@ Page Language="C#" AutoEventWireup="true"
    MasterPageFile="~/homepage/homepage.Master"
    CodeBehind="home.aspx.cs"
    Inherits="Smash_IT.homepage.index" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/hero.css") %>' />
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/reserve.css") %>' />
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/promos.css") %>' />
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/map.css") %>' />

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick-theme.css" />
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <section class="hero-section">
        <div class="slider-container">
            <div class="slider-wrapper" id="sliderWrapper">
                <asp:Repeater ID="rptHeroSlider" runat="server">
                    <ItemTemplate>
                        <div class="slide">
                            <img src='<%# GetBase64Image(Eval("ImgPath")) %>' alt='<%# Eval("Title") %>' />
                            <div class="hero-overlay-container">
                                <div class="hero-content-wrapper">
                                    <h1 class="hero-display-title"><%# Eval("Title") %></h1>
                                    <p class="hero-subtext"><%# Eval("Subtitle") %></p>
                                    <div class="hero-schedule-row">
                                        <div class="schedule-item">
                                            <span class="schedule-label">| SCHEDULE & RATES</span>
                                            <span class="schedule-time"><%# Eval("Content") %></span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
            <div class="slider-indicators">
                <asp:Repeater ID="rptIndicators" runat="server">
                    <ItemTemplate>
                        <button type="button" class='<%# Container.ItemIndex == 0 ? "dot active" : "dot" %>'
                            data-index='<%# Container.ItemIndex %>'>
                        </button>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </section>

    <section class="reserve-section container">
        <div class="reserve-content">

            <div class="reserve-image">
                <img src="\images\court.jpg" alt="Badminton Court" />
            </div>

            <div class="reserve-text">
                <h2>Reserve Now</h2>
                <p>
                    Reserve a badminton or pickleball court with ease. Enjoy a clean,
                well-maintained playing environment perfect for training,
                friendly matches, or competitive games.
            
                </p>

                <asp:HyperLink ID="hlReserve" runat="server" NavigateUrl="Reservation.aspx" CssClass="btn btn-reserve">
                Reserve
                </asp:HyperLink>

                <p class="note">
                    View availability and make your booking.
            
                </p>
            </div>

        </div>
    </section>


    <section class="reserve-section container">
        <asp:Repeater ID="rptReserve" runat="server">
            <ItemTemplate>
                <div class="reserve-content">
                    <div class="reserve-image">
                        <img src='<%# GetBase64Image(Eval("ImgPath")) %>' alt='Reserve Court' />
                    </div>
                    <div class="reserve-text">
                        <h2><%# Eval("Title") %></h2>
                        <p class="reserve-subtitle" style="color: #f0d673; font-weight: 600; margin-bottom: 10px;">
                            <%# Eval("Subtitle") %>
                        </p>
                        <p><%# Eval("Content") %></p>
                        <asp:HyperLink ID="hlReserve" runat="server" NavigateUrl="~/homepage/Reservations.aspx" CssClass="btn btn-reserve">
                            Reserve Now
                        </asp:HyperLink>
                        <p class="note">Click the button to view availability and make your booking.</p>
                    </div>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </section>

    <section class="promos-section container">
        <h2 class="promo-title">Promos and services offerings</h2>
        <div class="slider-container_promo">
            <div class="slider-wrapper_promo" id="promoSliderWrapper">
                <asp:Repeater ID="rptPromos" runat="server">
                    <ItemTemplate>
                        <div class="promo-slide">
                            <div class="promo-content">
                                <div class="promo-image">
                                    <img src='<%# GetBase64Image(Eval("FilePath")) %>' alt='<%# Eval("Title") %>' />
                                </div>
                                <div class="promo-text">
                                    <h3><%# Eval("Title") %></h3>
                                    <p class="sub-title">Latest Update</p>
                                    <p><%# Eval("Content").ToString().Length > 150 ? Eval("Content").ToString().Substring(0, 150) + "..." : Eval("Content") %></p>

                                    <asp:HyperLink ID="hlDetails" runat="server" NavigateUrl='<%# Eval("URL_FB") %>' Target="_blank" CssClass="btn-reserve">
                                    View Details
                                    </asp:HyperLink>
                                </div>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
            <div class="slider-indicators_promo" id="promoDotsContainer"></div>
        </div>
    </section>

    <section class="map-section">
        <div class="container">
            <iframe src="https://www.google.com/maps?q=14.364617,121.041413&z=14&output=embed" width="100%" height="400"
                style="border: 0;" allowfullscreen loading="lazy"></iframe>
        </div>
    </section>

    <script src="https://cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick.min.js"></script>
    <script src='<%= ResolveUrl("~/js/script.js") %>'></script>

</asp:Content>
