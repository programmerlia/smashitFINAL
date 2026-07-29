<%@ Page Language="C#" AutoEventWireup="true"
    MasterPageFile="~/homepage/homepage.Master"
    CodeBehind="home.aspx.cs"
    Inherits="Smash_IT.homepage.index" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/hero.css") %>' />
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/reserve.css") %>' />
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/promos.css") %>' />
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/map.css") %>' />
     <link rel="stylesheet" href='<%= ResolveUrl("~/css/home_sliding_promos.css") %>' />
      <link rel="stylesheet" href='<%= ResolveUrl("~/css/SlidingHome.css") %>' />

    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick-theme.css" />


    <style>
    .hero-section {
    position: relative;
    width: 100%;
    height: 100%;
    max-height: 910px;
    overflow: hidden;
    background-color: #000;
}

.slider-wrapper {
    display: flex;
    width: 100%;
    height: 100%;
    transition: transform 0.5s ease-in-out;
}

.slide {
    min-width: 100%;
    height: 100%;
    position: relative;
    overflow: hidden;
}

    .slide img {
        position: relative;
        z-index: 1;
        width: 100%;
        height: 100%;
        object-fit: cover;
        display: block;
        filter: brightness(0.4) contrast(1.1);
    }

/* ================= TEXT OVERLAY ================= */
.hero-overlay-container {
    position: absolute !important;
    z-index: 10 !important;
    bottom: 12%;
    left: 8%;
    color: #ffffff;
    display: block;
    visibility: visible;
    width: 80% !important;
    max-width: 800px !important;
    pointer-events: none;
    animation: fadeInUp 0.8s ease-out forwards;
}

.hero-display-title {
    font-size: clamp(3rem, 8vw, 5rem) !important;
    font-weight: 800 !important;
    color: #ffffff !important;
    margin-bottom: 15px !important;
    letter-spacing: -2px !important;
    line-height: 1.1 !important;
    text-transform: uppercase !important;
}

.hero-subtext {
    font-size: 1.2rem !important;
    line-height: 1.6 !important;
    margin-bottom: 35px !important;
    opacity: 0.95 !important;
    color: #ffffff !important;
    display: block !important;
}

.hero-schedule-row {
    display: flex;
    gap: 50px;
    border-top: 1px solid rgba(255, 255, 255, 0.2);
    padding-top: 25px;
}
/* Removed the extra } that was here */

.schedule-item {
    display: flex;
    flex-direction: column;
    gap: 5px;
}

.schedule-label {
    color: #f0d673;
}


/* ================= INDICATORS (DOTS) ================= */
.slider-indicators {
    position: absolute;
    bottom: 30px;
    left: 50%;
    transform: translateX(-50%);
    display: flex;
    gap: 12px;
    z-index: 20;
}

.dot {
    width: 12px;
    height: 12px;
    border-radius: 50%;
    background: rgba(255, 255, 255, 0.3);
    border: 2px solid transparent;
    cursor: pointer;
    transition: all 0.3s ease;
    padding: 0;
}

    .dot.active {
        background: #ffffff;
        transform: scale(1.2);
        box-shadow: 0 0 10px rgba(255, 255, 255, 0.5);
    }

/* Simple entry animation */
@keyframes fadeInUp {
    from {
        opacity: 0;
        transform: translateY(30px);
    }

    to {
        opacity: 1;
        transform: translateY(0);
    }
}

/* Responsive adjustments */
@media (max-width: 768px) {
    .hero-display-title {
        font-size: 3rem;
    }

    .hero-schedule-row {
        gap: 20px;
        flex-direction: column;
    }

    .hero-overlay-container {
        bottom: 15%;
        left: 5%;
    }
}

    </style>


    <style>
    /*.RESERVER*/
    :root {
    --primary-color: #000058;
    --accent-color: #ff4d4d;
    --text-dark: #1a1a1a;
    --text-muted: #64748b;
    --transition: all 0.4s cubic-bezier(0.165, 0.84, 0.44, 1);
}

/* ================= SECTION LAYOUT ================= */
.reserve-section {
    background-color: #f5f7f9;
    width: 100%;
    padding: 80px 20px;
    margin: 0 auto;
    box-shadow: inset 0 20px 20px -20px rgba(0,0,0,0.05);
}

.reserve-content {
    display: flex;
    align-items: center;
    gap: 80px;
    width: 100%;
    box-sizing: border-box;
}

/* ================= IMAGE STYLING ================= */
.reserve-image {
    flex: 1;
    width: 100%;
    height: 450px;
    border-radius: 24px;
    overflow: hidden;
    box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
}

    .reserve-image img {
        width: 100%;
        height: 100%;
        object-fit: cover;
        display: block;
        transition: var(--transition);
    }

.reserve-content:hover .reserve-image img {
    transform: scale(1.06);
}

/* ================= TEXT STYLING ================= */
.reserve-text {
    flex: 1.2;
}

    .reserve-text h2 {
        font-family: 'Inter', 'Poppins', -apple-system, sans-serif;
        font-size: 2.8rem;
        color: var(--text-dark);
        font-weight: 800;
        margin-bottom: 20px;
        letter-spacing: -1px;
        line-height: 1.1;
    }

    .reserve-text p {
        line-height: 1.8;
        color: var(--text-muted);
        margin-bottom: 35px;
        font-size: 1.15rem;
    }

    .reserve-text .note {
        display: block;
        font-size: 0.8rem;
        color: #94a3b8;
        background: transparent;
        padding: 0;
        margin-top: 12px;
        margin-bottom: 0;
        text-transform: none;
    }

/* ================= BUTTONS ================= */
.btn-reserve {
    display: inline-block;
    background-color: var(--primary-color);
    color: white !important;
    padding: 16px 40px;
    text-decoration: none;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 1px;
    border-radius: 12px;
    font-size: 0.85rem;
    transition: var(--transition);
}

    .btn-reserve:hover {
        background-color: var(--text-dark);
        transform: translateY(-3px);
        box-shadow: 0 10px 20px rgba(0, 0, 88, 0.2);
    }

/* ================= MOBILE RESPONSIVENESS ================= */
@media (max-width: 992px) {
    .reserve-content {
        gap: 40px;
    }

    .reserve-text h2 {
        font-size: 2.2rem;
    }
}

@media (max-width: 850px) {
    .reserve-content {
        flex-direction: column !important;
        text-align: center !important;
        margin-bottom: 80px;
    }

    .reserve-text, .reserve-image {
        width: 100%;
    }

    .reserve-image {
        height: 350px;
    }
}

    </style>

    <style>
    /*PROMOS*/
    
.promos-section {
    padding: 80px 0;
    background-color: var(--light-bg);
}

.section-title {
    text-align: center;
    font-size: 2rem;
    margin-bottom: 40px;
    position: relative;
    padding-bottom: 15px;
}

    .section-title::after {
        content: '';
        position: absolute;
        bottom: 0;
        left: 50%;
        transform: translateX(-50%);
        width: 60px;
        height: 3px;
        background-color: var(--primary-color);
    }

.promos-slider {
    margin-bottom: 40px;
}

.promo-slide {
    padding: 20px;
}

.promo-content {
    display: flex;
    background: var(--white);
    box-shadow: 0 5px 15px rgba(0,0,0,0.05);
    border-radius: 10px;
    overflow: hidden;
}

.promo-image {
    flex: 1;
    min-height: 300px;
}

    .promo-image img {
        width: 100%;
        height: 100%;
        object-fit: cover;
    }

.promo-text {
    flex: 1.2;
    padding: 30px;
}

    .promo-text h3 {
        font-size: 1.5rem;
        margin-bottom: 10px;
    }

    .promo-text .sub-title {
        font-weight: 600;
        color: var(--primary-color);
        margin-bottom: 15px;
    }

.promos-slider .slick-dots {
    bottom: -40px;
}

    .promos-slider .slick-dots li button:before {
        font-size: 12px;
        color: var(--secondary-color);
    }

    </style>

    <style>
    /* ================= PROMOS SECTION ================= */
.promos-section {
    padding: 80px 20px;
    width: 100%;
    max-width: 1920px;
    margin: 0 auto;
    box-sizing: border-box;
}

.promo-title {
    font-family: 'Inter', 'Poppins', -apple-system, sans-serif;
    text-align: center;
    font-size: 2.8rem;
    color: var(--text-dark);
    font-weight: 800;
    margin-bottom: 40px;
    letter-spacing: -1px;
    line-height: 1.1;
}

/* ================= SLIDER MECHANICS ================= */
.slider-container_promo {
    position: relative;
    width: 100%;
    max-width: 1500px;
    margin: 0 auto;
    overflow: hidden;
}

.slider-wrapper_promo {
    display: flex;
    width: 100%;
    transition: transform 0.6s cubic-bezier(0.165, 0.84, 0.44, 1);
}

.promo-slide {
    flex: 0 0 100%;
    width: 100%;
    box-sizing: border-box;
    padding: 0 15px;
}

/* ================= PROMO CARD ================= */
.promo-content {
    display: flex;
    align-items: center;
    gap: 80px;
    background-color: #ffffff;
    border-radius: 24px;
    padding: 40px;
    box-shadow: 0 10px 40px rgba(0, 0, 0, 0.04);
}

.promo-image {
    flex: 1;
    height: 400px;
    border-radius: 20px;
    overflow: hidden;
    box-shadow: 0 15px 30px rgba(0, 0, 0, 0.08);
}

    .promo-image img {
        width: 100%;
        height: 100%;
        object-fit: cover;
        display: block;
        transition: var(--transition);
    }

.promo-content:hover .promo-image img {
    transform: scale(1.06);
}

.promo-text {
    flex: 1.2;
}

    .promo-text h3 {
        font-family: 'Inter', -apple-system, sans-serif;
        font-size: 2.2rem;
        color: var(--text-dark);
        font-weight: 800;
        margin-bottom: 15px;
        letter-spacing: -0.5px;
        line-height: 1.2;
    }

    .promo-text p {
        line-height: 1.8;
        color: var(--text-muted);
        margin-bottom: 30px;
        font-size: 1.1rem;
    }

    .promo-text .sub-title {
        display: inline-flex;
        align-items: center;
        font-size: 0.85rem;
        font-weight: 700;
        text-transform: uppercase;
        color: var(--primary-color);
        background: rgba(0, 0, 88, 0.05);
        padding: 6px 16px;
        border-radius: 50px;
        margin-bottom: 20px;
        letter-spacing: 0.5px;
    }

/* ================= SLIDER DOTS ================= */
.slider-indicators_promo {
    position: absolute;
    bottom: 30px;
    left: 50%;
    transform: translateX(-50%);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 10;
}

.dot_promo {
    width: 12px;
    height: 12px;
    border-radius: 50%;
    background-color: var(--text-muted);
    opacity: 0.3;
    border: none;
    padding: 0;
    margin: 0 8px;
    cursor: pointer;
    outline: none;
    transition: var(--transition);
}

    .dot_promo.active,
    .dot_promo:hover {
        background-color: var(--primary-color);
        opacity: 1;
        transform: scale(1.2);
    }

/* ================= LARGE SCREEN OPTIMIZATION (1920px+) ================= */
@media (min-width: 1400px) {
    .promo-content {
        padding: 60px;
        gap: 100px;
    }

    .promo-image {
        height: 480px;
    }

    .promo-text h3 {
        font-size: 2.6rem;
    }
}

/* ================= MOBILE RESPONSIVENESS ================= */
@media (max-width: 992px) {
    .promo-content {
        gap: 40px;
        padding: 30px;
    }

    .promo-text h3 {
        font-size: 1.8rem;
    }
}

@media (max-width: 850px) {
    .promo-content {
        flex-direction: column !important;
        text-align: center;
        padding: 20px;
    }

    .promo-image, .promo-text {
        width: 100%;
    }

    .promo-image {
        height: 300px;
    }
}

    </style>


    <style>
    
body {
    overflow-x: hidden;
    margin: 0;
    padding: 0;
}

.slider-container {
    position: relative;
    width: 100%;
    aspect-ratio: 1920 / 910;
    background-color: #000;
    overflow: hidden;
}

.slider-wrapper {
    display: flex;
    width: 100%;
    height: 100%;
    transition: transform 0.5s ease-in-out;
}

    .slider-wrapper img {
        flex: 0 0 100%;
        width: 100%;
        height: 100%;
        object-fit: cover;
        display: block;
    }


.slider-indicators {
    position: absolute;
    bottom: 20px;
    left: 50%;
    transform: translateX(-50%);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 10;
}

.dot {
    width: 14px;
    height: 14px;
    border-radius: 50%;
    background-color: rgba(255, 255, 255, 0.4);
    border: none;
    padding: 0;
    margin: 0 6px;
    cursor: pointer;
    outline: none;
    transition: background-color 0.3s ease;
}

    .dot.active, .dot:hover {
        background-color: white;
    }

    </style>
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
                <img src="/images/announcement_draft.jpg" alt="Badminton Court" />
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
