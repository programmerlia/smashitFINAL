<%@ Page Title="Reservation Success" Language="C#" MasterPageFile="~/homepage/homepage.Master"
    AutoEventWireup="true" CodeBehind="ReservationSuccess.aspx.cs" Inherits="Smash_IT.payments.ReservationSuccess" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
    <style>
        .success-wrap{
            max-width:720px;
            margin:60px auto;
            padding:28px;
            border-radius:16px;
            background:#fff;
            box-shadow:0 8px 30px rgba(0,0,0,.08);
            text-align:center;
        }
        .success-title{
            font-size:1.35rem;
            font-weight:800;
            margin:0 0 8px 0;
        }
        .success-sub{
            margin:0;
            opacity:.85;
            font-size:1rem;
        }
        .spinner{
            width:56px;height:56px;
            border:6px solid rgba(0,0,0,.12);
            border-top-color: rgba(0,0,0,.65);
            border-radius:50%;
            margin:22px auto 10px auto;
            animation:spin .9s linear infinite;
        }
        @keyframes spin{ to{ transform:rotate(360deg);} }
        .countdown{
            font-size:.95rem;
            opacity:.7;
            margin-top:6px;
        }
    </style>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">
    <div class="success-wrap">
        <div class="success-title">✅ Payment successful</div>
        <p class="success-sub">Reservation pending request.</p>

        <div class="spinner" aria-label="Loading"></div>
        <div class="countdown">Redirecting to Home…</div>
    </div>

    <script type="text/javascript">
        (function () {
            try {
                if (window.sessionStorage) {
                    sessionStorage.removeItem("pendingBooking");
                    sessionStorage.removeItem("rentalCart");
                    sessionStorage.removeItem("isPaying");
                }
            } catch (e) { }

            var redirectUrl = '<%= HttpUtility.JavaScriptStringEncode(ResolveUrl("~/homepage/reservation.aspx?reset=1")) %>';

            setTimeout(function () {
                window.location.replace(redirectUrl);
            }, 3000);
        })();
    </script>
</asp:Content>