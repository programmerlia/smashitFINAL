<%@ Page Language="C#" AutoEventWireup="true"
    MasterPageFile="~/homepage/homepage.Master"
    CodeBehind="reservation.aspx.cs"
    Inherits="Smash_IT.homepage.reservation" %>

<%@ Import Namespace="System.Web" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
    <LINK rel="stylesheet" href='<%= ResolveUrl("~/css/res_main.css") %>' />
    <LINK rel="stylesheet" href='<%= ResolveUrl("~/css/res_active.css") %>' />

    <SCRIPT>
        window.resConfig = {
            ids: {
                btnReserveNow: "<%= btnReserveNow.ClientID %>",
                ddlSport: "<%= ddlSport.ClientID %>",
                ddlDuration: "<%= ddlDuration.ClientID %>",
                txtFirstname: "<%= txtFirstname.ClientID %>",
                txtLastname: "<%= txtLastname.ClientID %>",
                txtEmail: "<%= txtEmail.ClientID %>",
                txtContact: "<%= txtContact.ClientID %>",
                btnSubmitReservationUnique: "<%= btnSubmitReservation.UniqueID %>",
                ddlSportUnique: "<%= ddlSport.UniqueID %>",
                btnSubmitReservation: "<%= btnSubmitReservation.ClientID %>"
            },
            urls: {
                equipmentAvailability: "<%= ResolveUrl("~/homepage/reservation.aspx/GetEquipmentAvailability") %>"
            },
            isLoggedIn: <%= (Session["UserID"] != null).ToString().ToLower() %>,
            sessionUser: {
                firstname: "<%= HttpUtility.JavaScriptStringEncode(Convert.ToString(Session["Firstname"] ?? "")) %>",
                lastname: "<%= HttpUtility.JavaScriptStringEncode(Convert.ToString(Session["Lastname"] ?? "")) %>",
                email: "<%= HttpUtility.JavaScriptStringEncode(Convert.ToString(Session["Email"] ?? "")) %>",
                phone: "<%= HttpUtility.JavaScriptStringEncode(Convert.ToString(Session["PhoneNumber"] ?? "")) %>"
            },
            pricing: {
                courtPricePerHourPhp: 330.00,
                courtDepositRate: 0.50
            }
        };

        window.hfCourtIDClientID = "<%= hfCourtID.ClientID %>";
        window.hfCourtNumClientID = "<%= hfCourtNum.ClientID %>";
        window.hfResDateClientID = "<%= hfResDate.ClientID %>";
        window.hfStartTimeClientID = "<%= hfStartTime.ClientID %>";
        window.hfEndTimeClientID = "<%= hfEndTime.ClientID %>";
        window.lblSelectedSlotClientID = "<%= lblSelectedSlot.ClientID %>";
        window.hfSelectedSportClientID = "hfSelectedSport";
        
            window.hfSelectedCourtIDClientID = "<%= hfSelectedCourtID.ClientID %>";
    window.hfSelectedDateClientID = "<%= hfSelectedDate.ClientID %>";
    window.hfRentalCartClientID = "<%= hfRentalCart.ClientID %>";
        window.hfConsumableCartClientID = "<%= hfConsumableCart.ClientID %>";
        window.hfPaymentModeClientID = "<%= hfPaymentMode.ClientID %>";
    
        window.formatPhp = function (amount) {
            try {
                return "₱ " + Number(amount || 0).toLocaleString("en-PH", {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 2
                });
            } catch (e) {
                return "₱ " + (amount || 0);
            }
        };
    </SCRIPT>
    <SCRIPT src='<%= ResolveUrl("~/js/reservation.js") %>'></SCRIPT>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server" CssClass="main-flex">

    <!-- HERO -->
    <DIV class="hero-section">
        <DIV class="container hero-content">
            <H1>BOOK YOUR<BR />
                COURT NOW</H1>
            <P class="hero-subtext">Experience the best indoor courts in town. Badminton &amp; Pickleball available.</P>

            <asp:Button ID="btnReserveNow" runat="server"
                CssClass="btn-reserve-hero"
                Text="Reserve Now"
                UseSubmitBehavior="false"
                CausesValidation="false"
                OnClientClick="
                    if (window.showReservation) {
                        showReservation();
                    } else {
                        alert('showReservation is NOT defined. reservation.js is not loaded or has an error.');
                    }
                    return false;" />
        </DIV>
    </DIV>

    <!-- Hidden JSON -->
    <asp:HiddenField ID="hfCourts" runat="server" ClientIDMode="Static" />
    <asp:HiddenField ID="hfQueues" runat="server" ClientIDMode="Static" />

    <!-- Selection hidden fields -->
    <asp:HiddenField ID="hfSelectedCourtID" runat="server" ClientIDMode="Static" />
    <asp:HiddenField ID="hfSelectedDate" runat="server" ClientIDMode="Static" />

    <!-- Static hidden fields used by JS -->
    <asp:HiddenField ID="hfCourtID" runat="server" ClientIDMode="Static" />
    <asp:HiddenField ID="hfCourtNum" runat="server" ClientIDMode="Static" />
    <asp:HiddenField ID="hfStartTime" runat="server" ClientIDMode="Static" />
    <asp:HiddenField ID="hfEndTime" runat="server" ClientIDMode="Static" />
    <asp:HiddenField ID="hfResDate" runat="server" ClientIDMode="Static" />
    <asp:HiddenField ID="hfSelectedSport" runat="server" ClientIDMode="Static" />

    <!-- Rental hidden fields -->
    <asp:HiddenField ID="hfRentalCart" runat="server" ClientIDMode="Static" />
    <asp:HiddenField ID="hfRentalItems" runat="server" ClientIDMode="Static" />
    <asp:HiddenField ID="hfRentalStock" runat="server" ClientIDMode="Static" />

    <!-- Consumable hidden field -->
    <asp:HiddenField ID="hfConsumableCart" runat="server" ClientIDMode="Static" />

    <!-- Payment hidden field -->
    <asp:HiddenField ID="hfPaymentMode" runat="server" ClientIDMode="Static" Value="dp" />

    <DIV style="display: flex; align-items: flex-start; justify-content: center;">
        <DIV id="reservationSection"
            runat="server"
            clientidmode="Static"
            class="reservation-section"
            style="display: none;">

            <DIV class="container" style="display: block;">
                <DIV class="step-indicator d-flex justify-content-between align-items-center mb-4">
                    <DIV class="step-item active text-center flex-fill">
                        <DIV class="step-circle">1</DIV>
                        <SPAN class="step-label">Date &amp; Court</SPAN>
                    </DIV>
                    <DIV class="step-line flex-fill"></DIV>
                    <DIV class="step-item text-center flex-fill">
                        <DIV class="step-circle">2</DIV>
                        <SPAN class="step-label">Items</SPAN>
                    </DIV>
                    <DIV class="step-line flex-fill"></DIV>
                    <DIV class="step-item text-center flex-fill">
                        <DIV class="step-circle">3</DIV>
                        <SPAN class="step-label">Payment</SPAN>
                    </DIV>
                </DIV>

                <DIV class="d-flex justify-content-end gap-2 mb-3">
                    <asp:Button ID="btnResetReservation"
                        runat="server"
                        CssClass="btn btn-outline-danger btn-sm"
                        Text="Cancel / Restart"
                        UseSubmitBehavior="false"
                        CausesValidation="false"
                        OnClientClick="if(window.resetReservationUI){ resetReservationUI(); } return false;" />
                </DIV>
            </DIV>

            <!-- STEP 1 -->
            <DIV class="block">
                <DIV id="dateSelectionSection"
                    class="p-4"
                    style="display: none; flex-direction: column; width: 100%; align-items: center; justify-content: center;">

                    <asp:UpdatePanel ID="updReservation"
                        runat="server"
                        UpdateMode="Conditional"
                        CssClass="row g-4">
                        <ContentTemplate>
       


                            <DIV class="row g-3">
                                <!-- LEFT -->
                                <DIV class="col-12 col-lg-4">
                                    <DIV class="card shadow-sm p-3">
                                        <H5 class="fw-bold mb-2">📅 Choose Date</H5>

                                        <asp:Calendar ID="Calendar1" runat="server"
                                            OnSelectionChanged="Calendar1_SelectionChanged"
                                            OnDayRender="Calendar1_DayRender"
                                            CssClass="calendar-compact w-100">
                                            <SelectedDayStyle BackColor="#1a187c" ForeColor="White" />
                                            <TitleStyle Font-Bold="True" />
                                        </asp:Calendar>

                                        <DIV class="legend d-flex flex-wrap gap-2 small text-muted mt-2">
                                            <SPAN><SPAN class="dot selected"></SPAN>Selected</SPAN>
                                            <SPAN><SPAN class="dot booked"></SPAN>Fully Booked</SPAN>
                                            <SPAN><SPAN class="dot unavailable"></SPAN>Unavailable</SPAN>
                                        </DIV>

                                        <DIV class="mt-3 p-2 bg-light rounded">
                                            <DIV class="fw-bold small mb-1">⚠️ Unavailable:</DIV>
                                            <asp:Label ID="lblUnavailableHours" runat="server" CssClass="text-danger small">
                                                Select a date and sport to see unavailable hours.
                                            </asp:Label>
                                        </DIV>
                                    </DIV>
                                </DIV>

                                <!-- RIGHT -->
                                <DIV class="col-12 col-lg-8">
                                    <DIV class="card shadow-sm p-3">
                                        <DIV class="d-flex flex-wrap justify-content-between align-items-center gap-2">

                                            <DIV class="d-flex gap-2 flex-wrap">
                                                <DIV style="min-width: 160px;">
                                                    <LABEL class="form-label fw-bold small mb-1">Duration</LABEL>
                                                    <asp:DropDownList ID="ddlDuration" runat="server" CssClass="form-select form-select-sm" style="height:60%">
                                                        <asp:ListItem Text="1 Hour" Value="1" />
                                                        <asp:ListItem Text="2 Hours" Value="2" />
                                                        <asp:ListItem Text="3 Hours" Value="3" />
                                                        <asp:ListItem Text="4 Hours" Value="4" />
                                                    </asp:DropDownList>
                                                </DIV>

                                                <DIV style="min-width: 220px;">
                                                    <LABEL class="form-label fw-bold small mb-1">Sport</LABEL>

                                                    <DIV class="d-flex align-items-center gap-2">
                                                        <SPAN id="selectedSportBadge" class="badge bg-primary" style="padding: .90rem .8rem; font-size: .9rem;">No sport selected
                                                        </SPAN>

                                                        <BUTTON type="button" class="btn btn-outline-secondary btn-sm" onclick="openSportPickerModal()">
                                                            Change
                                                        </BUTTON>
                                                    </DIV>

                                                    <asp:DropDownList ID="ddlSport" runat="server"
                                                        AutoPostBack="true"
                                                        OnSelectedIndexChanged="ddlSport_SelectedIndexChanged"
                                                        ClientIDMode="Static"
                                                        CssClass="form-select form-select-sm"
                                                        Style="display: none;">
                                                        <asp:ListItem Text="" Value="" />
                                                        <asp:ListItem Text="Badminton" Value="badminton" />
                                                        <asp:ListItem Text="Pickleball" Value="pickleball" />
                                                    </asp:DropDownList>
                                                </DIV>
                                            </DIV>
                                        </DIV>

                                        <HR class="my-3" />

                                        <DIV class="mb-2">
                                            <asp:Label ID="lblSelectedSlot" runat="server"
                                                ClientIDMode="Static"
                                                CssClass="fw-bold text-primary"
                                                Text="No date and sport selected."></asp:Label>
                                        </DIV>

                                        <DIV class="timetable-shell">
                                            <asp:PlaceHolder ID="phTimeTable" runat="server"></asp:PlaceHolder>
                                        </DIV>

                                        <DIV class="d-flex justify-content-between align-items-center flex-wrap gap-2 mt-2">
                                            <SMALL class="text-muted">
                                                <SPAN class="badge" style="background-color: #DEE9FB; color: #0B61DD;">Reservable</SPAN>
                                                <SPAN class="badge" style="background-color: #D8F6FD; color: #055160;">Queue</SPAN>
                                                <SPAN class="badge" style="background-color: #F8E2E5; color: #90363E;">Blocked</SPAN>
                                            </SMALL>

                                            <asp:Button ID="btn_proceed_rentals"
                                                runat="server"
                                                CssClass="btn btn_proceed_details btn-sm"
                                                style="height:100%; margin-top:0px; border-radius: 5px;"
                                                Text="Items →"
                                                OnClientClick="goToRentals(); return false;" />
                                        </DIV>
                                    </DIV>
                                </DIV>
                            </DIV>

                        </ContentTemplate>
                        <Triggers>
                            <asp:AsyncPostBackTrigger ControlID="Calendar1" EventName="SelectionChanged" />
                            <asp:AsyncPostBackTrigger ControlID="ddlSport" EventName="SelectedIndexChanged" />
                        
                        </Triggers>
                    </asp:UpdatePanel>
                </DIV>

                <!-- STEP 2 -->
                <DIV style="display: block;">
                    <DIV id="rentalSelectionSection" class="row g-4" style="display: none;">
                        <DIV class="row">
                            <DIV class="col-lg-8">
                                <H4 class="fw-bold mb-3">Rental Items</H4>
                                <DIV id="rentalContainer" class="row g-3 mb-4"></DIV>

                                <H4 class="fw-bold mb-3">Sale Items</H4>
                                <DIV id="consumableContainer" class="row g-3"></DIV>
                            </DIV>

                            <DIV class="col-lg-4">
                                <DIV class="card border-0 shadow-sm p-4 h-100" style="border-radius: 15px;">
                                    <H5 class="fw-bold mb-4">Reservation Summary</H5>
                                    <HR class="text-muted" />

                                    <DIV class="summary-details mt-4">
                                        <P class="mb-1 small">Court: <SPAN id="courtSummaryCourt" class="fw-bold">---</SPAN></P>
                                        <P class="mb-1 small">Sport: <SPAN id="courtSummarySport" class="fw-bold">---</SPAN></P>
                                        <P class="mb-1 small">Time: <SPAN id="courtSummaryTime" class="fw-bold">---</SPAN></P>
                                        <P class="mb-3 small">Duration: <SPAN id="courtSummaryDuration" class="fw-bold">---</SPAN></P>

                                        <DIV class="player-input-wrapper mb-4">
                                            <LABEL class="small fw-bold">Number of Players</LABEL>
                                            <INPUT type="number" id="numPlayers" class="form-control" min="1" max="10" value="1" />
                                        </DIV>

                                        <DIV class="p-3">
                                            <DIV class="d-flex justify-content-between">
                                                <SPAN class="fw-bold">Court Total (100%)</SPAN>
                                                <SPAN id="totalPrice" class="fw-bold">₱ 0</SPAN>
                                            </DIV>
                                        </DIV>

                                        <DIV id="cartItems" class="small p-3"></DIV>
                                        <DIV id="consumableItems" class="small p-3"></DIV>
                                    </DIV>

                                    <DIV class="p-3">
                                        <DIV class="d-flex justify-content-between">
                                            <SPAN class="fw-bold">Rentals Total</SPAN>
                                            <SPAN id="rentalsTotal" class="fw-bold">₱ 0</SPAN>
                                        </DIV>
                                    </DIV>

                                    <DIV class="p-3">
                                        <DIV class="d-flex justify-content-between">
                                            <SPAN class="fw-bold">Items Total</SPAN>
                                            <SPAN id="consumablesTotal" class="fw-bold">₱ 0</SPAN>
                                        </DIV>
                                    </DIV>

                                    <DIV style="display: none;">
                                        <SPAN id="requiredAmountStored" class="fw-bold">₱ 0</SPAN>
                                    </DIV>

                                    <DIV class="total-price-display mb-3">
                                        <DIV class="label">Pay Now (PayMongo):</DIV>
                                        <DIV id="summary-totalPrice" class="amount">₱ 0</DIV>
                                        <DIV class="small text-muted mt-1" id="summaryPayNowNoteStep2">
    You pay 50% court deposit now. Rentals and sale items will be paid later.
</DIV>
                                    </DIV>

                                    <DIV class="d-flex gap-2 w-100 align-items-stretch">
                                        <BUTTON type="button" class="btn btn-outline-secondary" onclick="goBackToDate()">Back</BUTTON>
                                        <asp:Button ID="btn_proceed_details" runat="server"
                                            CssClass="btn btn_proceed_details flex-fill"
                                            style="height:100%; margin-top:0px; border-radius: 5px;"
                                            Text="Details →"
                                            OnClientClick="goToInfoSection(); return false;" />
                                    </DIV>
                                </DIV>
                            </DIV>
                        </DIV>
                    </DIV>
                </DIV>

                <!-- STEP 3 -->
                <DIV style="display: block;">
                    <DIV id="infoSection" class="row g-4" style="display: none;">
                        <DIV class="row">
                            <DIV class="col-lg-8">
                                <H4 class="fw-bold mb-4">Player Information</H4>

                                <DIV class="card border-0 shadow-sm p-4 mb-5" style="border-radius: 15px;">
                                    <DIV class="row">
                                        <DIV class="col-md-6">
                                            <DIV class="form-floating-custom">
                                                <LABEL class="form-label-custom">First Name</LABEL>
                                                <asp:TextBox ID="txtFirstname" runat="server" CssClass="form-control-custom" placeholder="Enter First Name"></asp:TextBox>
                                            </DIV>
                                        </DIV>

                                        <DIV class="col-md-6">
                                            <DIV class="form-floating-custom">
                                                <LABEL class="form-label-custom">Last Name</LABEL>
                                                <asp:TextBox ID="txtLastname" runat="server" CssClass="form-control-custom" placeholder="Enter Last Name"></asp:TextBox>
                                            </DIV>
                                        </DIV>

                                        <DIV class="col-md-6">
                                            <DIV class="form-floating-custom">
                                                <LABEL class="form-label-custom">Contact Number</LABEL>
                                                <asp:TextBox ID="txtContact" runat="server" CssClass="form-control-custom" placeholder="09XX XXX XXXX"></asp:TextBox>
                                            </DIV>
                                        </DIV>

                                        <DIV class="col-md-6">
                                            <DIV class="form-floating-custom">
                                                <LABEL class="form-label-custom">Email</LABEL>
                                                <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control-custom" placeholder="email@example.com"></asp:TextBox>
                                            </DIV>
                                        </DIV>
                                    </DIV>
                                </DIV>

                                <div class="card border-0 shadow-sm p-3 mb-3" style="border-radius: 15px;">
    <div class="fw-bold mb-2">Court Payment Option</div>

    <div class="form-check mb-2">
        <input class="form-check-input" type="radio" name="courtPaymentMode"
            id="payModeDp" value="dp" checked onclick="setPaymentMode('dp')" />
        <label class="form-check-label" for="payModeDp">
            Downpayment only (50% of court)
        </label>
    </div>

    <div class="form-check">
        <input class="form-check-input" type="radio" name="courtPaymentMode"
            id="payModeFull" value="full" onclick="setPaymentMode('full')" />
        <label class="form-check-label" for="payModeFull">
            Full court payment (100% of court)
        </label>
    </div>

    <div class="small text-muted mt-2">
        Rentals and consumables are saved with the reservation but will be paid later.
    </div>
</div>
                                <DIV class="d-flex justify-content-between align-items-end">
                               

                                    <DIV class="total-price-display">
                                        <DIV class="label">Pay Now (PayMongo):</DIV>
                                        <DIV id="summary-totalPriceInfo" class="amount">₱ 0</DIV>
                                    </DIV>
                                </DIV>

                                <DIV class="mt-3 small text-muted" style="display: none;">
                                    Required Amount stored in reservation:
                                    <B><SPAN id="summary-requiredAmountStored">₱ 0</SPAN></B>
                                </DIV>
                            </DIV>

                            <DIV class="col-lg-4">
                                <DIV class="card border-0 shadow-sm p-4 h-100" style="border-radius: 15px;">
                                    <H5 class="fw-bold mb-4">Reservation Summary</H5>

                                    <DIV class="mb-4">
                                        <DIV class="summary-text-muted">Full Name: <SPAN id="summaryFirstname">------</SPAN> <SPAN id="summaryLastname">------</SPAN></DIV>
                                        <DIV class="summary-text-muted">Contact Number: <SPAN id="summaryContact">------</SPAN></DIV>
                                        <DIV class="summary-text-muted">Email: <SPAN id="summaryEmail">------</SPAN></DIV>
                                    </DIV>

                                    <HR class="text-muted" />

                                    <DIV class="summary-details mt-4">
                                        <P class="mb-1 small">Court: <SPAN id="summaryCourt" class="fw-bold">---</SPAN></P>
                                        <P class="mb-1 small">Sport: <SPAN id="summarySport" class="fw-bold">---</SPAN></P>
                                        <P class="mb-1 small">Time: <SPAN id="summaryTime" class="fw-bold">---</SPAN></P>
                                        <P class="mb-3 small">Duration: <SPAN id="summaryDuration" class="fw-bold">---</SPAN></P>
                                    
                                    </DIV>
                                    <DIV class="small text-muted mt-1" id="summaryPayNowNoteStep3">
    You pay 50% court deposit now. Rentals and sale items will be paid later.
</DIV>
                                    <DIV class="d-flex gap-2 w-100 align-items-stretch">
                                        <BUTTON type="button" class="btn btn-outline-secondary flex-fill" onclick="goBackToRentals()">Back</BUTTON>

                                        <asp:Button ID="btnBookReservation" runat="server"
                                            CssClass="btn btn-primary flex-fill"
                                           style="height:100%; margin-top:0px; border-radius: 5px;"
                                            Text="Book"
                                            UseSubmitBehavior="false"
                                            OnClientClick="return onBookReservationClick();" />
                                    </DIV>
                                </DIV>

                                <asp:Button ID="btnSubmitReservation" runat="server"
                                    Style="display: none;"
                                    UseSubmitBehavior="false"
                                    OnClick="btnSubmitReservation_Click" />
                            </DIV>
                        </DIV>
                    </DIV>
                </DIV>
            </DIV>
            <!-- /block -->

        </DIV>
        <!-- /reservationSection -->
    </DIV>

    <!-- ABOUT -->
    <DIV class="container section-padding">
        <DIV class="row align-items-center g-5 text-center text-md-start">
            <DIV class="col-md-9">
                <IMG src="https://static.vecteezy.com/system/resources/previews/026/161/690/large_2x/badminton-equipments-rackets-and-white-cream-shuttlecocks-on-sand-floor-of-outdoor-badminton-court-selective-focus-concept-for-outdoor-activity-and-outdoor-sports-for-health-free-photo.jpg"
                    class="about-img" />
            </DIV>

            <DIV class="col-md-3">
                <DIV class="section-header mb-4">
                    <H3>MORE ABOUT US</H3>
                </DIV>

                <P class="text-muted small">
                    Open-access courts with flexible schedules and transparent pricing.
                </P>
            </DIV>
        </DIV>
    </DIV>

    <!-- WHY CHOOSE -->
    <DIV class="container section-padding">
        <DIV class="section-header text-center mb-5">
            <H3>WHY CHOOSE SMASH IT?</H3>
            <P>Premium indoor courts designed for comfort and performance.</P>
        </DIV>

        <DIV class="row g-4 align-items-center text-center text-md-start" style="display: flex; justify-content: center; align-content: center;">
            <DIV class="col-md-6">
                <DIV class="row g-3">
                    <DIV class="col-6">
                        <DIV class="image-box">Indoor Court A</DIV>
                    </DIV>
                    <DIV class="col-6">
                        <DIV class="image-box">Indoor Court B</DIV>
                    </DIV>
                </DIV>
            </DIV>

            <DIV class="col-md-3 offset-md-1">
                <DIV class="feature-box">
                    <UL class="list-unstyled mb-0 lh-lg">
                        <LI>✔ Professional courts</LI>
                        <LI>✔ Badminton &amp; Pickleball</LI>
                        <LI>✔ Easy online booking</LI>
                        <LI>✔ Clean facilities</LI>
                        <LI>✔ Free parking</LI>
                    </UL>
                </DIV>
            </DIV>
        </DIV>
    </DIV>

    <!-- COURT LAYOUT -->
    <DIV class="container section-padding text-center">
        <DIV class="section-header mb-4">
            <H3>COURT LAYOUT &amp; FACILITIES</H3>
            <P>Six professionally maintained courts with premium acrylic flooring.</P>
        </DIV>

        <IMG src='<%= ResolveUrl("~/images/Court.png") %>' class="court-layout-img" />
    </DIV>

    <!-- PAYMENT MODAL -->
    <DIV id="paymentModal" class="custom-modal" style="display: none;">
        <DIV class="custom-modal-content">
            <DIV class="d-flex justify-content-between align-items-start gap-2">
                <DIV>
                    <DIV class="fw-bold" style="font-size: 1.05rem;">Confirm Payment</DIV>
                    <DIV class="text-muted small">Review your booking before paying.</DIV>
                </DIV>
                <BUTTON type="button" class="btn btn-sm btn-light" onclick="closePaymentModal()">✕</BUTTON>
            </DIV>

            <HR class="my-3" />

            <DIV class="small" style="line-height: 1.6;">
                <DIV class="d-flex justify-content-between"><SPAN class="text-muted">Court</SPAN><SPAN id="pmCourt" class="fw-bold">---</SPAN></DIV>
                <DIV class="d-flex justify-content-between"><SPAN class="text-muted">Sport</SPAN><SPAN id="pmSport" class="fw-bold">---</SPAN></DIV>
                <DIV class="d-flex justify-content-between"><SPAN class="text-muted">Date</SPAN><SPAN id="pmDate" class="fw-bold">---</SPAN></DIV>
                <DIV class="d-flex justify-content-between"><SPAN class="text-muted">Time</SPAN><SPAN id="pmTime" class="fw-bold">---</SPAN></DIV>
                <DIV class="d-flex justify-content-between"><SPAN class="text-muted">Duration</SPAN><SPAN id="pmDuration" class="fw-bold">---</SPAN></DIV>
                <DIV class="d-flex justify-content-between"><SPAN class="text-muted">Players</SPAN><SPAN id="pmPlayers" class="fw-bold">---</SPAN></DIV>
            </DIV>

            <HR class="my-3" />

            <DIV class="fw-bold mb-2" style="font-size: .95rem;">Rentals</DIV>
            <DIV id="pmRentals" class="small text-muted" style="max-height: 160px; overflow: auto;">
                No rental items yet.
            </DIV>
            <DIV class="d-flex justify-content-between small mt-2">
                <SPAN class="text-muted">Rentals Total</SPAN>
                <SPAN id="pmRentalsTotal" class="fw-bold">₱ 0.00</SPAN>
            </DIV>

            <HR class="my-3" />

            <DIV class="fw-bold mb-2" style="font-size: .95rem;">Sale Items</DIV>
            <DIV id="pmConsumables" class="small text-muted" style="max-height: 160px; overflow: auto;">
                No sale items bought.
            </DIV>
            <DIV class="d-flex justify-content-between small mt-2">
                <SPAN class="text-muted">Consumables Total</SPAN>
                <SPAN id="pmConsumablesTotal" class="fw-bold">₱ 0.00</SPAN>
            </DIV>

        <HR class="my-3" />

<DIV class="fw-bold mb-2" style="font-size: .95rem;">Court Payment Option</DIV>

<DIV class="form-check mb-2">
    <INPUT class="form-check-input" type="radio" name="courtPaymentModeModal"
        id="payModeDpModal" value="dp" checked onclick="setPaymentMode('dp', true)" />
    <LABEL class="form-check-label" for="payModeDpModal">
        Downpayment only (50% of court)
    </LABEL>
</DIV>

<DIV class="form-check mb-2">
    <INPUT class="form-check-input" type="radio" name="courtPaymentModeModal"
        id="payModeFullModal" value="full" onclick="setPaymentMode('full', true)" />
    <LABEL class="form-check-label" for="payModeFullModal">
        Full court payment (100% of court)
    </LABEL>
</DIV>

<DIV class="text-muted small mt-2">
    Rentals and sale items are not charged in PayMongo now. They stay linked to this reservation and can be paid later.
</DIV>


            <DIV class="d-flex gap-2 w-100 align-items-stretch">
                <BUTTON type="button" class="btn btn-outline-secondary flex-fill" onclick="closePaymentModal()">Cancel</BUTTON>
               <button type="button" class="btn btn_proceed_details flex-fill" style="height:100%; margin-top:0px; border-radius: 5px;" onclick="return confirmAndPay()">Pay Now</button>
            </DIV>
        </DIV>
    </DIV>
    <!-- SPORT PICKER MODAL -->
    <!-- SPORT PICKER MODAL -->
    <DIV id="sportPickerModal" class="custom-modal" style="display: none;">
        <DIV class="custom-modal-content sport-picker-modal">
            <DIV class="sport-picker-title">Select Sport</DIV>
            <DIV class="sport-picker-subtitle">
                Choose sport that you want to play here in SMASH-IT!
            </DIV>

            <DIV class="row g-3">
                <DIV class="col-12 col-md-6">
                    <BUTTON type="button" class="sport-card-btn" onclick="selectSportAndStart('badminton')">
                        <DIV class="sport-card-visual">
                            <IMG src='<%= ResolveUrl("~/images/badmintonvector.png") %>' alt="Badminton" />
                        </DIV>
                        <DIV class="sport-card-desc">Indoor court booking for badminton</DIV>
                    </BUTTON>
                </DIV>

                <DIV class="col-12 col-md-6">
                    <BUTTON type="button" class="sport-card-btn" onclick="selectSportAndStart('pickleball')">
                        <DIV class="sport-card-visual">
                            <IMG src='<%= ResolveUrl("~/images/pickleballvector.png") %>' alt="Pickleball" />
                        </DIV>
                        <DIV class="sport-card-desc">Indoor court booking for pickleball</DIV>
                    </BUTTON>
                </DIV>
            </DIV>

            <DIV class="sport-modal-close-wrap">
                <BUTTON type="button" class="sport-modal-close-btn" onclick="closeSportPickerModal()">Close</BUTTON>
            </DIV>
        </DIV>
    </DIV>
</asp:Content>
