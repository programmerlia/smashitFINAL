<%@ Page Language="C#" AutoEventWireup="true"
    MasterPageFile="~/homepage/homepage.Master"
    CodeBehind="home.aspx.cs"
    Inherits="Smash_IT.homepage.index" %>


<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">

    <!-- Page-specific CSS -->
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/hero.css") %>' />
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/reserve.css") %>' />
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/promos.css") %>' />
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/map.css") %>' />

    <!-- Slick (only if homepage uses it) -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick-theme.css" />

</asp:Content>


<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <!-- ================= HERO SECTION ================= -->
    <section class="hero-section">
        <div class="slider-container">
            <div class="slider-wrapper" id="sliderWrapper">
                <img src="/images/Badminton.png" alt="Slide 1" />
                <img src="/images/Pickleball.png" alt="Slide 2" />
            </div>

            <div class="slider-indicators">
                <button type="button" class="dot active" aria-label="Go to slide 1"></button>
                <button type="button" class="dot" aria-label="Go to slide 2"></button>
            </div>
        </div>
    </section>


    <!-- ================= RESERVE ================= -->
    <section class="reserve-section container">
        <div class="reserve-content">

            <div class="reserve-image">
                <img src="https://placehold.co/600x400/000000/ffffff?text=Court+Image" alt="Badminton Court" />
            </div>

            <div class="reserve-text">
                <h2>Reserve Now</h2>
                <p>
                    Reserve a badminton or pickleball court with ease. Enjoy a clean,
                well-maintained playing environment perfect for training,
                friendly matches, or competitive games.
           
                </p>

                <asp:HyperLink ID="hlReserve" runat="server" NavigateUrl="#" CssClass="btn btn-reserve">
                Reserve
                </asp:HyperLink>

                <p class="note">
                    Click the button to view availability and make your booking.
           
                </p>
            </div>

        </div>
    </section>


    <!-- ================= PROMOS ================= -->
    <section class="promos-section container">
        <h2 class="section-title">Promos and services offerings</h2>

        <div class="slider-container_promo">
            <div class="slider-wrapper_promo" id="promoSliderWrapper">
                <asp:Repeater ID="rptPromos" runat="server">
                    <ItemTemplate>
                        <div class="promo-slide">
                            <div class="promo-content">
                                <div class="promo-image">
                                    <img src='<%# GetBase64Image(Eval("ImageFIleData"), Eval("ImageFileType")) %>' alt='<%# Eval("Title") %>' />
                                </div>
                                <div class="promo-text">
                                    <h3><%# Eval("Title") %></h3>
                                    <p class="sub-title">Latest Update</p>
                                    <p><%# Eval("Content").ToString().Length > 150 ? Eval("Content").ToString().Substring(0, 150) + "..." : Eval("Content") %></p>
                                    <asp:HyperLink ID="hlApply" runat="server" NavigateUrl='<%# "AnnouncementDetails.aspx?id=" + Eval("AnnouncementID") %>' CssClass="btn btn-apply">
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


    <!-- ================= MAP ================= -->
    <section class="map-section">
        <div class="container">
            <iframe
                src="https://www.google.com/maps?q=14.364617,121.041413&z=14&output=embed"
                width="100%"
                height="400"
                style="border: 0;"
                allowfullscreen
                loading="lazy"></iframe>
        </div>
    </section>


    <!-- Page scripts -->
    <script src="https://cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick.min.js"></script>
    <script src='<%= ResolveUrl("~/js/script.js") %>'></script>

</asp:Content>
