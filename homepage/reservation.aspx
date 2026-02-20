<%@ Page Language="C#" AutoEventWireup="true"
    MasterPageFile="~/homepage/homepage.Master"
    CodeBehind="reservation.aspx.cs"
    Inherits="Smash_IT.homepage.reservation" %>


<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">

    <!-- Page-specific CSS (only what this page needs) -->
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/reserve.css") %>' />
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/promos.css") %>' />
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/map.css") %>' />

    <!-- Slick (only if reservation page actually uses sliders) -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick-theme.css" />

</asp:Content>


<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <!-- ================= RESERVATION CONTENT ================= -->

    <section class="container py-10">
        <h1 class="text-3xl font-bold mb-6">Court Reservation</h1>

        <p>
            Your reservation interface goes here. Replace this section
            with your actual booking UI, calendar, or form.
        </p>
    </section>

    <!-- Page-specific scripts -->
    <script src="https://cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick.min.js"></script>
    <script src='<%= ResolveUrl("~/js/script.js") %>'></script>

</asp:Content>
