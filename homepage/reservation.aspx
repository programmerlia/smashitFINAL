<%@ Page Language="C#" AutoEventWireup="true"
    MasterPageFile="~/homepage/homepage.Master"
    CodeBehind="reservation.aspx.cs"
    Inherits="Smash_IT.homepage.reservation" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
  <link rel="stylesheet" href='<%= ResolveUrl("~/css/res_main.css") %>' />
  <link rel="stylesheet" href='<%= ResolveUrl("~/css/res_active.css") %>' />


  <script>
      window.resConfig = {
        ids: {
            btnReserveNow: "<%= btnReserveNow.ClientID %>",
          ddlSport: "<%= ddlSport.ClientID %>",
          ddlDuration: "<%= ddlDuration.ClientID %>",
          txtFName: "<%= txtFName.ClientID %>",
          txtEmail: "<%= txtEmail.ClientID %>",
          txtContact: "<%= txtContact.ClientID %>",
            btnSubmitReservationUnique: "<%= btnSubmitReservation.UniqueID %>",
            ddlSportUnique: "<%= ddlSport.UniqueID %>"
      },
      urls: {
        equipmentAvailability: "<%= ResolveUrl("~/homepage/reservation.aspx/GetEquipmentAvailability") %>"
          },
          isLoggedIn: <%= (Session["UserID"] != null).ToString().ToLower() %>,
      sessionUser: {
        fullName: "<%= (Session["FName"] ?? "").ToString().Replace("\"","\\\"") %>",
        email: "<%= (Session["Email"] ?? "").ToString().Replace("\"","\\\"") %>",
        phone: "<%= (Session["PhoneNumber"] ?? "").ToString().Replace("\"","\\\"") %>"
      }
    };

    window.hfCourtIDClientID = "<%= hfCourtID.ClientID %>";
    window.hfCourtNumClientID = "<%= hfCourtNum.ClientID %>";
    window.hfResDateClientID = "<%= hfResDate.ClientID %>";
    window.hfStartTimeClientID = "<%= hfStartTime.ClientID %>";
    window.hfEndTimeClientID = "<%= hfEndTime.ClientID %>";
      window.lblSelectedSlotClientID = "<%= lblSelectedSlot.ClientID %>";

  </script>

  <script src='<%= ResolveUrl("~/js/reservation.js") %>'></script>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

  <!-- HERO -->
  <div class="hero-section">
    <div class="container hero-content">
      <h1>BOOK YOUR<br />COURT NOW</h1>
      <p class="hero-subtext">Experience the best indoor courts in town. Badminton & Pickleball available.</p>
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
    </div>
  </div>

  <!-- Hidden JSON -->
  <asp:HiddenField ID="hfCourts" runat="server" ClientIDMode="Static" />
  <asp:HiddenField ID="hfQueues" runat="server" ClientIDMode="Static" />

  <!-- Selection hidden fields (server reads these) -->
  <asp:HiddenField ID="hfSelectedCourtID" runat="server" ClientIDMode="Static" />
  <asp:HiddenField ID="hfSelectedDate" runat="server" ClientIDMode="Static" />
  <asp:HiddenField ID="hfCourtID" runat="server" />
  <asp:HiddenField ID="hfCourtNum" runat="server" />
  <asp:HiddenField ID="hfStartTime" runat="server" />
  <asp:HiddenField ID="hfEndTime" runat="server" />
  <asp:HiddenField ID="hfResDate" runat="server" />

  <!-- rentals -->
  <asp:HiddenField ID="hfRentalCart" runat="server" ClientIDMode="Static" />
  <asp:HiddenField ID="hfRentalItems" runat="server" ClientIDMode="Static" />
  <asp:HiddenField ID="hfRentalStock" runat="server" ClientIDMode="Static" />

 <div id="reservationSection"
     runat="server"
     ClientIDMode="Static"
     class="reservation-section"
     style="display:none">
    <!-- Step indicator -->
    <div class="step-indicator d-flex justify-content-between align-items-center mb-4">
      <div class="step-item active text-center flex-fill">
        <div class="step-circle">1</div>
        <span class="step-label">Date &amp; Court</span>
      </div>
      <div class="step-line flex-fill"></div>
      <div class="step-item text-center flex-fill">
        <div class="step-circle">2</div>
        <span class="step-label">Rentals</span>
      </div>
      <div class="step-line flex-fill"></div>
      <div class="step-item text-center flex-fill">
        <div class="step-circle">3</div>
        <span class="step-label">Payment</span>
      </div>
    </div>
    <div class="d-flex justify-content-end gap-2 mb-3">
<asp:Button ID="btnResetReservation"
    runat="server"
    CssClass="btn btn-outline-danger btn-sm"
    Text="Cancel / Restart"
    UseSubmitBehavior="false"
    CausesValidation="false"
    OnClientClick="if(window.resetReservationUI){ resetReservationUI(); } return false;" />
</div>

    <!-- ===================== STEP 1 ===================== -->
<div id="dateSelectionSection" style="display:none;" class="p-4">
  <asp:UpdatePanel ID="updReservation"
      runat="server"
      UpdateMode="Conditional"
      CssClass="row g-4">
    <ContentTemplate>
    <div class="row">
          <!-- LEFT: Calendar -->
          <div class="col-12 col-lg-4">
            <div class="card shadow-sm p-3">
              <h5 class="fw-bold mb-2">📅 Choose Date</h5>

              <asp:Calendar ID="Calendar1" runat="server"
                OnSelectionChanged="Calendar1_SelectionChanged"
                CssClass="calendar-compact w-100">
                <SelectedDayStyle BackColor="#1a187c" ForeColor="White" />
                <TitleStyle Font-Bold="True" />
              </asp:Calendar>

              <div class="legend d-flex flex-wrap gap-2 small text-muted mt-2">
                <span><span class="dot selected"></span>Selected</span>
                <span><span class="dot booked"></span>Fully Booked</span>
                <span><span class="dot unavailable"></span>Unavailable</span>
              </div>

              <div class="mt-3 p-2 bg-light rounded">
                <div class="fw-bold small mb-1">⚠️ Unavailable:</div>
                <asp:Label ID="lblUnavailableHours" runat="server" CssClass="text-danger small">Loading...</asp:Label>
              </div>
            </div>
          </div>

          <!-- RIGHT: Timetable + controls -->
          <div class="col-12 col-lg-8">
            <div class="card shadow-sm p-3">
              <div class="d-flex flex-wrap justify-content-between align-items-center gap-2">
                <h5 class="fw-bold mb-0">⏱️ Pick a Time Slot</h5>

                <div class="d-flex gap-2 flex-wrap">
                  <div style="min-width:160px;">
                    <label class="form-label fw-bold small mb-1">Duration</label>
                    <asp:DropDownList ID="ddlDuration" runat="server" CssClass="form-select form-select-sm">
                      <asp:ListItem Text="1 Hour" Value="1" />
                      <asp:ListItem Text="2 Hours" Value="2" />
                      <asp:ListItem Text="3 Hours" Value="3" />
                      <asp:ListItem Text="4 Hours" Value="4" />
                    </asp:DropDownList>
                  </div>

                  <div style="min-width:160px;">
                    <label class="form-label fw-bold small mb-1">Sport</label>
              
                    <asp:DropDownList ID="ddlSport" runat="server"
  AutoPostBack="true"
  OnSelectedIndexChanged="ddlSport_SelectedIndexChanged"
  ClientIDMode="Static" CssClass="form-select form-select-sm">


                    <asp:ListItem Text="" Value="" />
                      <asp:ListItem Text="Badminton" Value="badminton" />
                      <asp:ListItem Text="Pickleball" Value="pickleball" />
                    </asp:DropDownList>
                  </div>
                </div>
              </div>

              <hr class="my-3" />

              <div class="mb-2">
                <asp:Label ID="lblSelectedSlot" runat="server" CssClass="fw-bold text-primary" Text="No slot selected."></asp:Label>
              </div>

              <div class="timetable-shell">
                <asp:PlaceHolder ID="phTimeTable" runat="server"></asp:PlaceHolder>
              </div>

              <div class="d-flex justify-content-between align-items-center flex-wrap gap-2 mt-2">
                <small class="text-muted">
                  <span class="badge bg-success">Reservable</span>
                  <span class="badge bg-info text-dark">Queue</span>
                  <span class="badge bg-danger">Blocked</span>
                  <span class="badge bg-primary">Badminton</span>
                  <span class="badge bg-warning text-dark">Pickleball</span>
                </small>

                <asp:Button ID="btn_proceed_rentals"
                  runat="server"
                  CssClass="btn btn_proceed_details btn-sm"
                  Text="Proceed to Rentals →"
                  OnClientClick="goToRentals(); return false;" />
              </div>
            </div>
          </div>
          </div>
        </ContentTemplate>
        <Triggers>
          <asp:AsyncPostBackTrigger ControlID="Calendar1" EventName="SelectionChanged" />
           <asp:PostBackTrigger ControlID="btnSubmitReservation" />
           
<asp:AsyncPostBackTrigger ControlID="btnResetReservation" EventName="Click" />
        </Triggers>
      </asp:UpdatePanel>
    </div>

    <!-- ===================== STEP 2: RENTALS ===================== -->
    <div id="rentalSelectionSection" class="row g-4" style="display:none;">
    <div class="row">
      <div class="col-lg-8">
        <h4 class="fw-bold mb-3">Rental Items</h4>
        <div id="equipmentContainer" class="row g-3"></div>
      </div>

      <div class="col-lg-4">
        <div class="card border-0 shadow-sm p-4 h-100" style="border-radius:15px;">
          <h5 class="fw-bold mb-4">Reservation Summary</h5>
          <hr class="text-muted" />

          <div class="summary-details mt-4">
            <p class="mb-1 small">Court: <span id="courtSummaryCourt" class="fw-bold">---</span></p>
            <p class="mb-1 small">Sport: <span id="courtSummarySport" class="fw-bold">---</span></p>
            <p class="mb-1 small">Time: <span id="courtSummaryTime" class="fw-bold">---</span></p>
            <p class="mb-3 small">Duration: <span id="courtSummaryDuration" class="fw-bold">---</span></p>

            <div class="player-input-wrapper mb-4">
              <label class="small fw-bold">Number of Players</label>
              <input type="number" id="numPlayers" class="form-control" min="1" max="10" value="1" />
            </div>

            <div class="p-3">
              <div class="d-flex justify-content-between">
                <span class="fw-bold">Court Total</span>
                <span id="totalPrice" class="fw-bold">₱ 0</span>
              </div>
            </div>

            <div>
              <div id="cartItems" class="small p-3"></div>
              
            </div>
          </div>

          <div class="p-3">
  <div class="d-flex justify-content-between">
    <span class="fw-bold">Rentals Total</span>
    <span id="rentalsTotal" class="fw-bold">₱ 0</span>
  </div>
</div>

          <div class="total-price-display mb-3">
            <div class="label">Total Price:</div>
            <div id="totalFinal" class="amount">₱ 0</div>
          </div>

          <div class="d-flex gap-2 mt-3">
            <button type="button" class="btn btn-outline-secondary flex-fill" onclick="goBackToDate()">← Back</button>
            <asp:Button ID="btn_proceed_details" runat="server" CssClass="btn btn_proceed_details flex-fill"
              Text="Proceed to Details →" OnClientClick="goToInfoSection(); return false;" />
          </div>
        </div>
      </div>
      </div>
    </div>

    <!-- ===================== STEP 3: INFO/PAYMENT ===================== -->
    <div id="infoSection" class="row g-5" style="display:none;">
    <div class="row">
      <div class="col-lg-8">
        <h4 class="fw-bold mb-4">Player Information</h4>

        <div class="card border-0 shadow-sm p-4 mb-5" style="border-radius:15px;">
          <div class="row">
            <div class="col-md-6">
              <div class="form-floating-custom">
                <label class="form-label-custom">Full Name</label>
                <asp:TextBox ID="txtFName" runat="server" CssClass="form-control-custom" placeholder="Enter Full Name"></asp:TextBox>
              </div>
            </div>

            <div class="col-md-6">
              <div class="form-floating-custom">
                <label class="form-label-custom">Contact Number</label>
                <asp:TextBox ID="txtContact" runat="server" CssClass="form-control-custom" placeholder="09XX XXX XXXX"></asp:TextBox>
              </div>
            </div>

            <div class="col-md-6">
              <div class="form-floating-custom">
                <label class="form-label-custom">Email</label>
                <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control-custom" placeholder="email@example.com"></asp:TextBox>
              </div>
            </div>
          </div>
        </div>

        <div class="d-flex justify-content-between align-items-end">
          <div>
            <h5 class="fw-bold mb-3">
              Payment Method
              <small class="fw-normal text-success" style="font-size:0.7rem; cursor:pointer;">click here for payment details</small>
            </h5>
            <div class="payment-methods">
              <asp:FileUpload ID="fuPayment" runat="server" CssClass="form-control mb-3" />
            </div>
          </div>

          <div class="total-price-display">
            <div class="label">Total Price:</div>
            <div id="summary-totalPrice" class="amount"></div>
          </div>
        </div>
      </div>

      <div class="col-lg-4">
        <div class="card border-0 shadow-sm p-4 h-100" style="border-radius:15px;">
          <h5 class="fw-bold mb-4">Reservation Summary</h5>

          <div class="mb-4">
            <div class="summary-text-muted">Full Name: <span id="summaryFName">------</span></div>
            <div class="summary-text-muted">Contact Number: <span id="summaryContact">------</span></div>
            <div class="summary-text-muted">Email: <span id="summaryEmail">------</span></div>
          </div>

          <hr class="text-muted" />

          <div class="summary-details mt-4">
            <p class="mb-1 small">Court: <span id="summaryCourt" class="fw-bold">---</span></p>
            <p class="mb-1 small">Sport: <span id="summarySport" class="fw-bold">---</span></p>
            <p class="mb-1 small">Time: <span id="summaryTime" class="fw-bold">---</span></p>
            <p class="mb-3 small">Duration: <span id="summaryDuration" class="fw-bold">---</span></p>
            <p class="mb-1 small">Players: <span id="summaryPlayers" class="fw-bold">1</span></p>
          </div>

          <div class="d-flex gap-2 mt-3">
            <button type="button" class="btn btn-outline-secondary flex-fill" onclick="goBackToRentals()">← Back</button>
            <asp:Button ID="btnBookReservation" runat="server"
              CssClass="btn btn-primary flex-fill"
              Text="Book Reservation"
              UseSubmitBehavior="false"
              OnClientClick="return onBookReservationClick();" />
          </div>
        </div>

        <asp:Button ID="btnSubmitReservation" runat="server" Style="display:none;"
          UseSubmitBehavior="false"
          OnClick="btnSubmitReservation_Click" />
      </div>
      </div>
    </div>

  </div>

     <!-- ============ ABOUT (3/4 IMAGE + 1/4 TEXT) ============ -->
   <div class="container section-padding">
       <div class="row align-items-center g-5 text-center text-md-start">
           <div class="col-md-9">
               <img src="https://static.vecteezy.com/system/resources/previews/026/161/690/large_2x/badminton-equipments-rackets-and-white-cream-shuttlecocks-on-sand-floor-of-outdoor-badminton-court-selective-focus-concept-for-outdoor-activity-and-outdoor-sports-for-health-free-photo.jpg"
                   class="about-img" />
           </div>

           <div class="col-md-3">
               <div class="section-header mb-4">
                   <h3>MORE ABOUT US</h3>
               </div>

               <p class="text-muted small">
                   Open-access courts with flexible schedules and transparent pricing.
               </p>
           </div>
       </div>
   </div>

   <!-- ============ WHY CHOOSE (IMAGES + GRAY TEXT BOX ONLY) ============ -->
   <div class="container section-padding">

       <!-- HEADER OUTSIDE GRAY BOX -->
       <div class="section-header text-center mb-5">
           <h3>WHY CHOOSE SMASH IT?</h3>
           <p>Premium indoor courts designed for comfort and performance.</p>
       </div>

       <div class="row g-4 align-items-center text-center text-md-start">

           <!-- Images -->
           <div class="col-md-6">
               <div class="row g-3">
                   <div class="col-6">
                       <div class="image-box">Indoor Court A</div>
                   </div>
                   <div class="col-6">
                       <div class="image-box">Indoor Court B</div>
                   </div>
               </div>
           </div>

           <!-- GRAY BOX ONLY FOR TEXT -->
           <div class="col-md-3 offset-md-1">
               <div class="feature-box">
                   <ul class="list-unstyled mb-0 lh-lg">
                       <li>✔ Professional courts</li>
                       <li>✔ Badminton & Pickleball</li>
                       <li>✔ Easy online booking</li>
                       <li>✔ Clean facilities</li>
                       <li>✔ Free parking</li>
                   </ul>
               </div>
           </div>

       </div>
   </div>

   <!-- ============ COURT LAYOUT ============ -->
   <div class="container section-padding text-center">
       <div class="section-header mb-4">
           <h3>COURT LAYOUT & FACILITIES</h3>
           <p>
               Six professionally maintained courts with premium acrylic flooring.
           </p>
       </div>

       <img src='<%= ResolveUrl("~/images/Court.png") %>' class="court-layout-img" />
   </div>

  <!-- PAYMENT MODAL -->
  <div id="paymentModal" class="custom-modal" style="display:none;">
    <div class="custom-modal-content">
      <h5 style="margin: 0 0 10px;">Confirm &amp; Proceed to Payment</h5>
      <p style="margin: 0 0 10px;">You will be redirected to PayMongo to complete your payment.</p>

      <div style="font-size:14px; margin-bottom:6px;">
        <div><b>Court:</b> <span id="pmCourt">---</span></div>
        <div><b>Sport:</b> <span id="pmSport">---</span></div>
        <div><b>Date:</b> <span id="pmDate">---</span></div>
        <div><b>Time:</b> <span id="pmTime">---</span></div>
        <div><b>Duration:</b> <span id="pmDuration">---</span></div>
        <div><b>Players:</b> <span id="pmPlayers">---</span></div>
     
      </div>

     

      <div style="font-size:14px; margin-bottom:12px;">
        <div style="display:flex; justify-content:space-between; margin-bottom:6px;">
          <b>Rentals</b>
          <span id="pmRentalsTotal">₱ 0.00</span>
        </div>
        <div id="pmRentals" class="text-muted" style="font-size:13px;">No rental items yet.</div>
      </div>
       <hr style="margin:12px 0;" />
         <div><b>Total:</b> <span id="pmTotal">---</span></div>

      <div style="display:flex; gap:10px; justify-content:flex-end;">
        <button type="button" class="btn btn-secondary" onclick="closePaymentModal()">Cancel</button>
        <button type="button" class="btn btn-success" onclick="confirmAndPay()">Proceed</button>
      </div>
    </div>
  </div>

</asp:Content>