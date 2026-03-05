<%@ Page Language="C#" AutoEventWireup="true"
    MasterPageFile="~/homepage/homepage.Master"
    CodeBehind="profile.aspx.cs"
    Inherits="Smash_IT.homepage.profile" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
    <style>
        .profile-hero {
            background: linear-gradient(135deg, #1a187c 0%, #1a187c 100%);
            padding: 36px 18px;
            color: #fff;
            border-radius: 0 0 28px 28px;
            margin-bottom: 26px;
        }
        .avatar-wrap {
            width: 92px; height: 92px;
            border-radius: 50%;
            overflow: hidden;
            background: #fff;
            border: 3px solid rgba(255,255,255,.6);
            box-shadow: 0 6px 18px rgba(0,0,0,.18);
            margin: 0 auto 12px;
        }
        .avatar-wrap img { width: 100%; height: 100%; object-fit: cover; }
        .cardx {
            background: #fff;
            border: 1px solid #eee;
            border-radius: 14px;
            box-shadow: 0 8px 24px rgba(0,0,0,.06);
            padding: 18px;
            margin-bottom: 16px;
        }
        .cardx h4 {
            font-size: 1.05rem;
            font-weight: 700;
            margin-bottom: 12px;
            display: inline-block;
            border-bottom: 2px solid #1a187c;
            padding-bottom: 6px;
        }
        .stat {
            background:#f8f9fa;
            border-radius: 12px;
            padding: 14px;
            text-align:center;
        }
        .stat .n { font-size: 1.6rem; font-weight: 800; color:#1a187c; line-height: 1; }
        .stat .t { font-size: .85rem; color:#6c757d; margin-top: 6px; }

        .info-row { display:flex; justify-content:space-between; gap:12px; padding:10px 0; border-bottom:1px solid #f0f0f0; }
        .info-row:last-child { border-bottom:0; }
        .info-label { color:#6c757d; font-weight:600; }
        .info-value { color:#333; font-weight:700; }

        .btn-danger-soft { background:#dc3545; color:#fff; border:0; border-radius: 999px; padding: 10px 16px; font-weight:700; }
        .btn-success-soft { background:#1a187c; color:#fff; border:0; border-radius: 999px; padding: 10px 16px; font-weight:700; }
        .btn-outline-soft { border-radius:999px; font-weight:700; }

        .badge-status { font-size:.8rem; padding:.35rem .55rem; border-radius:999px; }
        .badge-pending { background:#fff3cd; color:#856404; border:1px solid #ffeeba; }
        .badge-approved { background:#d1ecf1; color:#0c5460; border:1px solid #bee5eb; }
        .badge-cancelled { background:#f8d7da; color:#721c24; border:1px solid #f5c6cb; }
        .badge-completed { background:#d4edda; color:#155724; border:1px solid #c3e6cb; }
        .badge-request { background:#e2e3e5; color:#383d41; border:1px solid #d6d8db; }

        .res-item { border:1px solid #eee; border-radius:12px; padding:12px; margin-bottom:10px; }
        .res-top { display:flex; justify-content:space-between; gap:10px; align-items:flex-start; flex-wrap: wrap; }
        .res-actions { display:flex; gap:8px; flex-wrap:wrap; margin-top:10px; }
        .muted { color:#6c757d; }
    </style>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">



    <!-- HERO -->
    <div class="profile-hero text-center" style="margin-top:80px;">
        <div class="container">
            <div class="avatar-wrap">
                <asp:Image ID="imgAvatar" runat="server" />
            </div>
            <div style="font-size:1.7rem; font-weight:800;">
                <asp:Label ID="lblFirstname" runat="server" />  <span>  </span><asp:Label ID="lblLastname" runat="server" />
            </div>
      
            <div style="opacity:.9;">
                <asp:Label ID="lblEmail" runat="server" />
            </div>
        </div>
    </div>

    <div class="container" style="margin-top:-10px;">
        <asp:Label ID="lblMsg" runat="server" EnableViewState="false" />

        <div class="row g-3">
            <!-- LEFT -->
            <div class="col-lg-8">

                <!-- PERSONAL -->
                <div class="cardx">
                    <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
                        <h4 class="mb-0">Personal Information</h4>
                        <button type="button" class="btn btn-outline-success btn-outline-soft" data-bs-toggle="modal" data-bs-target="#editProfileModal">
                            Edit Profile
                        </button>
                    </div>

                    <div class="mt-3">
                        <div class="info-row">
                            <span class="info-label">Full Name</span>
                            <span class="info-value"><asp:Label ID="lblFirstname2" runat="server" /> <span>  </span><asp:Label ID="lblLastname2" runat="server" /> </span>
                        </div>
                    
                        <div class="info-row">
                            <span class="info-label">Phone Number</span>
                            <span class="info-value"><asp:Label ID="lblPhone" runat="server" /></span>
                        </div>
                        <div class="info-row">
                            <span class="info-label">Username</span>
                            <span class="info-value"><asp:Label ID="lblUsername" runat="server" /></span>
                        </div>
                        <div class="info-row">
                            <span class="info-label">Member Since</span>
                            <span class="info-value"><asp:Label ID="lblCreatedAt" runat="server" /></span>
                        </div>
                    </div>
                </div>

                <!-- PENDING (default 1) -->
                <div class="cardx">
                    <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
                        <h4 class="mb-0">My Reservations</h4>
                        <a class="btn btn-outline-success btn-outline-soft" href='<%= ResolveUrl("~/homepage/reservation.aspx") %>'>Book Another</a>
                    </div>

                    <div class="mt-3">
                        <div class="muted mb-2">
                            Pending shows 1 by default. Expand to view all.
                        </div>

                        <asp:Panel ID="pnlPendingTop1" runat="server" />

                    <button type="button"
        class="btn btn-outline-secondary btn-outline-soft"
        data-bs-toggle="modal"
        data-bs-target="#allResModal">
  View all reservations
</button>
                        </div>

                        <!-- MODAL FOR RESERVATION CANCELLATIONS AT BOTTOM OF THE PAGE-->
                    </div>
                </div>

               

            <!-- RIGHT -->
            <div class="col-lg-4">
                <!-- STATS -->
                <div class="cardx">
                    <h4>Your Stats</h4>
                    <!--Bookings-->
                    <div class="row g-2 mt-2">
                        <div class="col-6">
                            <div class="stat">
                                <div class="n"><asp:Label ID="lblTotalBookings" runat="server" Text="0" /></div>
                                <div class="t">Total Bookings</div>
                            </div>
                        </div>
                        <div class="col-6">
                            <div class="stat">
                                <div class="n"><asp:Label ID="lblPendingCount" runat="server" Text="0" /></div>
                                <div class="t">Pending</div>
                            </div>
                        </div>
                        <div class="col-12">
                            <div class="stat">
                                <div class="n"><asp:Label ID="lblHoursPlayed" runat="server" Text="0" /></div>
                                <div class="t">Hours Played</div>
                            </div>
                        </div>
                    </div>
                    <!--Buttons for payments and REntals-->
                    <div style="display: flex;
    justify-content: space-evenly;
    margin: 10px 10px 10px;">
  <button type="button" class="btn btn-outline-success btn-outline-soft"
          data-bs-toggle="modal" data-bs-target="#paymentsModal">
    Payments
  </button>

  <button type="button" class="btn btn-outline-success btn-outline-soft"
          data-bs-toggle="modal" data-bs-target="#rentalsModal">
    Rentals
  </button>
</div>
                </div>
            

                <!-- LOGOUT -->
                <div class="cardx text-center">
                    <h4>Session</h4>
                    <div class="muted mb-2">Ready to leave?</div>
                    <asp:Button ID="btnLogout" runat="server" CssClass="btn-danger-soft w-100" Text="Logout" OnClick="btnLogout_Click" />
                </div>

                <!-- SUPPORT -->
                <div class="cardx" style="background:#f8f9fa;">
                    <h4>Need Help?</h4>
                    <div class="muted small mb-2">Contact support for any issues.</div>
                    <a class="btn btn-outline-success w-100 btn-outline-soft" href="mailto:smashitsportscenter@gmail.com">
                        Email Support
                    </a>
                </div>
            </div>
             </div>
    </div>

    <!-- EDIT MODAL -->
    <div class="modal fade" id="editProfileModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content" style="border-radius:16px;">
                <div class="modal-header">
                    <h5 class="modal-title" style="font-weight:800;">Edit Profile</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>

                <div class="modal-body">
                    <div class="mb-2">
                        <label class="form-label fw-bold">Phone Number</label>
                        <asp:TextBox ID="txtPhone" runat="server" CssClass="form-control" />
                    </div>

                    <div class="mb-2">
                        <label class="form-label fw-bold">Username</label>
                        <asp:TextBox ID="txtUsername" runat="server" CssClass="form-control" />
                    </div>

                    <hr />

                    <div class="mb-2">
                        <label class="form-label fw-bold">New Password (optional)</label>
                        <asp:TextBox ID="txtNewPassword" runat="server" TextMode="Password" CssClass="form-control" />
                    </div>
                    <div class="mb-2">
                        <label class="form-label fw-bold">Confirm New Password</label>
                        <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password" CssClass="form-control" />
                    </div>

                    <hr />

                    <div class="mb-2">
                        <label class="form-label fw-bold">Profile Image (optional)</label>
                        <asp:FileUpload ID="fuAvatar" runat="server" CssClass="form-control" />
                        <div class="small muted mt-1">Allowed: .jpg, .png, .webp</div>
                    </div>

                    <asp:Label ID="lblEditMsg" runat="server" EnableViewState="false" />
                </div>

                <div class="modal-footer">
                    <button type="button" class="btn btn-outline-secondary btn-outline-soft" data-bs-dismiss="modal">Close</button>
                    <asp:Button ID="btnSaveProfile" runat="server" CssClass="btn-success-soft" Text="Save Changes" OnClick="btnSaveProfile_Click" />
                </div>
            </div>
        </div>
    </div>

<script type="text/javascript">
    (function () {
        function setState(open) {
            try { sessionStorage.setItem("collapseAllRes", open ? "open" : "closed"); } catch (e) { }
        }

        document.addEventListener("DOMContentLoaded", function () {
            var el = document.getElementById("collapseAllRes");
            if (!el) return;

            // restore previous state
            try {
                if (sessionStorage.getItem("collapseAllRes") === "open") {
                    if (window.bootstrap && bootstrap.Collapse) {

                        new bootstrap.Collapse(el, { toggle: true }); // Bootstrap 5
                    } else if (window.jQuery && jQuery.fn && jQuery.fn.collapse) {
                        jQuery(el).collapse("show"); // Bootstrap 4 fallback
                    } else {
                        // fallback: at least show it
                        el.className += " show";
                    }
                }
            } catch (e) { }

            el.addEventListener("shown.bs.collapse", function () { setState(true); });
            el.addEventListener("hidden.bs.collapse", function () { setState(false); });
        });
    })();
</script>


<!--MODAL FOR RESERVATOIN-->
<div class="modal fade" id="allResModal" tabindex="-1">
  <div class="modal-dialog modal-lg modal-dialog-scrollable">
    <div class="modal-content">

      <div class="modal-header">
        <h5 class="modal-title">All Reservations</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>

      <div class="modal-body">

        <!-- TABS -->
        <ul class="nav nav-tabs" role="tablist">
          <li class="nav-item" role="presentation">
            <button class="nav-link active" data-bs-toggle="tab" data-bs-target="#tabApproved" type="button" role="tab">
              Approved
            </button>
          </li>
          <li class="nav-item" role="presentation">
            <button class="nav-link" data-bs-toggle="tab" data-bs-target="#tabPending" type="button" role="tab">
              Pending
            </button>
          </li>
          <li class="nav-item" role="presentation">
            <button class="nav-link" data-bs-toggle="tab" data-bs-target="#tabRequests" type="button" role="tab">
              Requests
            </button>
          </li>
          <li class="nav-item" role="presentation">
            <button class="nav-link" data-bs-toggle="tab" data-bs-target="#tabCancelled" type="button" role="tab">
              Cancelled
            </button>
          </li>
          <li class="nav-item" role="presentation">
            <button class="nav-link" data-bs-toggle="tab" data-bs-target="#tabCompleted" type="button" role="tab">
              Completed
            </button>
          </li>
        </ul>

        <div class="tab-content pt-3">

          <!-- APPROVED -->
          <div class="tab-pane fade show active" id="tabApproved" role="tabpanel">

            <asp:Repeater ID="rptApproved" runat="server"
                OnItemCommand="rptReservations_ItemCommand"
                OnItemDataBound="rptReservations_ItemDataBound">
              <ItemTemplate>
                <div class="res-item mb-3">
                  <div class="res-top d-flex justify-content-between">
                    <div>
                      <div style="font-weight:800;">
                        Reservation #<%# Eval("ReservationID") %>
                        • Court <%# Eval("CourtNumber") %>
                      </div>

                      <div class="muted">
                        <%# Eval("ResDate","{0:yyyy-MM-dd}") %> •
                        <%# Eval("StartStr") %> - <%# Eval("EndStr") %>
                      </div>

                      <div class="mt-1">
                        <span class='badge-status <%# Eval("StatusBadgeClass") %>'>
                          <%# Eval("ReservationStatusName") %>
                        </span>

                        <%# Convert.ToString(Eval("RequestStatus")).Trim().Length > 0
                            ? "<span class='badge-status badge-request ms-1'>Request: " + Eval("RequestStatus") + "</span>"
                            : "" %>
                      </div>
                    </div>

                    <div class="text-end">
                      <div class="muted">Payment</div>
                      <div style="font-weight:800;">
                        <%# Eval("PaymentStatusDisplay") %>
                      </div>
                    </div>
                  </div>

                  <div class="res-actions mt-2">
                    <asp:Button ID="btnCancelReq" runat="server"
                        CssClass="btn btn-outline-danger btn-outline-soft"
                        Text="Request Cancel"
                        CommandName="cancel"
                        CommandArgument='<%# Eval("ReservationID") %>' />


                  </div>

                  <asp:Label ID="lblRuleHint" runat="server" CssClass="muted small d-block mt-2" />
                </div>
              </ItemTemplate>
            </asp:Repeater>

            <asp:Panel ID="pnlEmptyApproved" runat="server" Visible="false">
              <div class="muted">No approved reservations.</div>
            </asp:Panel>

          </div>

          <!-- PENDING -->
          <div class="tab-pane fade" id="tabPending" role="tabpanel">

            <asp:Repeater ID="rptPending" runat="server"
                OnItemCommand="rptReservations_ItemCommand"
                OnItemDataBound="rptReservations_ItemDataBound">
              <ItemTemplate>
                <div class="res-item mb-3">
                  <div class="res-top d-flex justify-content-between">
                    <div>
                      <div style="font-weight:800;">
                        Reservation #<%# Eval("ReservationID") %>
                        • Court <%# Eval("CourtNumber") %>
                        • <%# Eval("SportName") %>
                      </div>

                      <div class="muted">
                        <%# Eval("ResDate","{0:yyyy-MM-dd}") %> •
                        <%# Eval("StartStr") %> - <%# Eval("EndStr") %>
                      </div>

                      <div class="mt-1">
                        <span class='badge-status <%# Eval("StatusBadgeClass") %>'><%# Eval("ReservationStatusName") %></span>
                        <%# Convert.ToString(Eval("RequestStatus")).Trim().Length > 0
                            ? "<span class='badge-status badge-request ms-1'>Request: " + Eval("RequestStatus") + "</span>"
                            : "" %>
                      </div>
                    </div>

                    <div class="text-end">
                      <div class="muted">Payment</div>
                      <div style="font-weight:800;"><%# Eval("PaymentStatusDisplay") %></div>
                    </div>
                  </div>

                  <div class="res-actions mt-2">
                    <asp:Button ID="btnCancelReq" runat="server" CssClass="btn btn-outline-danger btn-outline-soft"
                        Text="Request Cancel" CommandName="cancel" CommandArgument='<%# Eval("ReservationID") %>' />
                  </div>

                  <asp:Label ID="lblRuleHint" runat="server" CssClass="muted small d-block mt-2" />
                </div>
              </ItemTemplate>
            </asp:Repeater>

            <asp:Panel ID="pnlEmptyPending" runat="server" Visible="false">
              <div class="muted">No pending reservations.</div>
            </asp:Panel>

          </div>

          <!-- REQUESTS -->
          <div class="tab-pane fade" id="tabRequests" role="tabpanel">

            <asp:Repeater ID="rptRequests" runat="server"
                OnItemCommand="rptReservations_ItemCommand"
                OnItemDataBound="rptReservations_ItemDataBound">
              <ItemTemplate>
                <div class="res-item mb-3">
                  <div class="res-top d-flex justify-content-between">
                    <div>
                      <div style="font-weight:800;">
                        Reservation #<%# Eval("ReservationID") %> • Court <%# Eval("CourtNumber") %> • <%# Eval("SportName") %>
                      </div>

                      <div class="muted">
                        <%# Eval("ResDate","{0:yyyy-MM-dd}") %> • <%# Eval("StartStr") %> - <%# Eval("EndStr") %>
                      </div>

                      <div class="mt-1">
                        <span class='badge-status <%# Eval("StatusBadgeClass") %>'><%# Eval("ReservationStatusName") %></span>
                        <%# Convert.ToString(Eval("RequestStatus")).Trim().Length > 0
                            ? "<span class='badge-status badge-request ms-1'>Request: " + Eval("RequestStatus") + "</span>"
                            : "" %>
                      </div>
                    </div>

                    <div class="text-end">
                      <div class="muted">Payment</div>
                      <div style="font-weight:800;"><%# Eval("PaymentStatusDisplay") %></div>
                    </div>
                  </div>

                  <div class="res-actions mt-2">
                    <asp:Button ID="btnCancelReq" runat="server" CssClass="btn btn-outline-danger btn-outline-soft"
                        Text="Request Cancel" CommandName="cancel" CommandArgument='<%# Eval("ReservationID") %>' />


                  </div>

                  <asp:Label ID="lblRuleHint" runat="server" CssClass="muted small d-block mt-2" />
                </div>
              </ItemTemplate>
            </asp:Repeater>

            <asp:Panel ID="pnlEmptyRequests" runat="server" Visible="false">
              <div class="muted">No requests yet.</div>
            </asp:Panel>

          </div>

          <!-- CANCELLED -->
          <div class="tab-pane fade" id="tabCancelled" role="tabpanel">

            <asp:Repeater ID="rptCancelled" runat="server"
                OnItemCommand="rptReservations_ItemCommand"
                OnItemDataBound="rptReservations_ItemDataBound">
              <ItemTemplate>
                <div class="res-item mb-3">
                  <div class="res-top d-flex justify-content-between">
                    <div>
                      <div style="font-weight:800;">
                        Reservation #<%# Eval("ReservationID") %> • Court <%# Eval("CourtNumber") %> • <%# Eval("SportName") %>
                      </div>

                      <div class="muted">
                        <%# Eval("ResDate","{0:yyyy-MM-dd}") %> • <%# Eval("StartStr") %> - <%# Eval("EndStr") %>
                      </div>

                      <div class="mt-1">
                        <span class='badge-status <%# Eval("StatusBadgeClass") %>'><%# Eval("ReservationStatusName") %></span>
                        <%# Convert.ToString(Eval("RequestStatus")).Trim().Length > 0
                            ? "<span class='badge-status badge-request ms-1'>Request: " + Eval("RequestStatus") + "</span>"
                            : "" %>
                      </div>
                    </div>

                    <div class="text-end">
                      <div class="muted">Payment</div>
                      <div style="font-weight:800;"><%# Eval("PaymentStatusDisplay") %></div>
                    </div>
                  </div>

                  <div class="res-actions mt-2">
                    <asp:Button ID="btnCancelReq" runat="server" CssClass="btn btn-outline-danger btn-outline-soft"
                        Text="Request Cancel" CommandName="cancel" CommandArgument='<%# Eval("ReservationID") %>' />
                  </div>

                  <asp:Label ID="lblRuleHint" runat="server" CssClass="muted small d-block mt-2" />
                </div>
              </ItemTemplate>
            </asp:Repeater>

            <asp:Panel ID="pnlEmptyCancelled" runat="server" Visible="false">
              <div class="muted">No cancelled reservations.</div>
            </asp:Panel>

          </div>

          <!-- COMPLETED -->
          <div class="tab-pane fade" id="tabCompleted" role="tabpanel">

            <asp:Repeater ID="rptCompleted" runat="server"
                OnItemCommand="rptReservations_ItemCommand"
                OnItemDataBound="rptReservations_ItemDataBound">
              <ItemTemplate>
                <div class="res-item mb-3">
                  <div class="res-top d-flex justify-content-between">
                    <div>
                      <div style="font-weight:800;">
                        Reservation #<%# Eval("ReservationID") %> • Court <%# Eval("CourtNumber") %> • <%# Eval("SportName") %>
                      </div>

                      <div class="muted">
                        <%# Eval("ResDate","{0:yyyy-MM-dd}") %> • <%# Eval("StartStr") %> - <%# Eval("EndStr") %>
                      </div>

                      <div class="mt-1">
                        <span class='badge-status <%# Eval("StatusBadgeClass") %>'><%# Eval("ReservationStatusName") %></span>
                        <%# Convert.ToString(Eval("RequestStatus")).Trim().Length > 0
                            ? "<span class='badge-status badge-request ms-1'>Request: " + Eval("RequestStatus") + "</span>"
                            : "" %>
                      </div>
                    </div>

                    <div class="text-end">
                      <div class="muted">Payment</div>
                      <div style="font-weight:800;"><%# Eval("PaymentStatusDisplay") %></div>
                    </div>
                  </div>

                  <div class="res-actions mt-2">
                    <asp:Button ID="btnCancelReq" runat="server" CssClass="btn btn-outline-danger btn-outline-soft"
                        Text="Request Cancel" CommandName="cancel" CommandArgument='<%# Eval("ReservationID") %>' />
                  </div>

                  <asp:Label ID="lblRuleHint" runat="server" CssClass="muted small d-block mt-2" />
                </div>
              </ItemTemplate>
            </asp:Repeater>

            <asp:Panel ID="pnlEmptyCompleted" runat="server" Visible="false">
              <div class="muted">No completed reservations.</div>
            </asp:Panel>

          </div>

        </div>
      </div>
    </div>
  </div>
</div>

<!--MODAL FOR PAYMENTT-->
<div class="modal fade" id="paymentsModal" tabindex="-1" aria-hidden="true">
  <div class="modal-dialog modal-lg modal-dialog-scrollable modal-dialog-centered">
    <div class="modal-content" style="border-radius:16px;">
      <div class="modal-header">
        <h5 class="modal-title" style="font-weight:800;">My Payments</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>

      <div class="modal-body">
        <asp:Panel ID="pnlPaymentsEmpty" runat="server" Visible="false">
          <div class="muted">No payments yet.</div>
        </asp:Panel>

      <asp:Repeater ID="rptPayments" runat="server">
  <HeaderTemplate>
    <div class="table-responsive">
      <table class="table table-sm align-middle">
        <thead>
          <tr>
            <th>Date</th>
            <th>Type</th>
            <th>Details</th>
            <th class="text-end">Amount</th>
          </tr>
        </thead>
        <tbody>
  </HeaderTemplate>

  <ItemTemplate>
    <tr>
      <td><%# Eval("PaymentDate") == null ? "—" : ((DateTime)Eval("PaymentDate")).ToString("yyyy-MM-dd") %></td>
      <td style="font-weight:800;"><%# Eval("PaymentTypeName") %></td>
      <td class="muted">
        <div style="font-weight:700; color:#333;"><%# Eval("DetailsLine1") %></div>
        <div class="small muted"><%# Eval("DetailsLine2") %></div>
      </td>
      <td class="text-end" style="font-weight:900;">₱ <%# Eval("AmountPhp","{0:N2}") %></td>
    </tr>
  </ItemTemplate>

  <FooterTemplate>
        </tbody>
      </table>
    </div>
  </FooterTemplate>
</asp:Repeater>
      </div>

      <div class="modal-footer">
        <button type="button" class="btn btn-outline-secondary btn-outline-soft" data-bs-dismiss="modal">Close</button>
      </div>
    </div>
  </div>
</div>
<!--MODAL FOR RENTAL-->
<div class="modal fade" id="rentalsModal" tabindex="-1" aria-hidden="true">
  <div class="modal-dialog modal-lg modal-dialog-scrollable modal-dialog-centered">
    <div class="modal-content" style="border-radius:16px;">
      <div class="modal-header">
        <h5 class="modal-title" style="font-weight:800;">My Rentals</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>

      <div class="modal-body">

        <asp:Panel ID="pnlRentalsEmpty" runat="server" Visible="false">
          <div class="muted">No rentals yet.</div>
        </asp:Panel>

     <asp:Repeater ID="rptRentals" runat="server">
  <ItemTemplate>
    <div class="res-item mb-3">
      <div style="font-weight:900;">
        Rental #<%# Eval("RentalID") %> • <%# Eval("ItemName") %>
      </div>

      <div class="muted small">
        <%# Eval("ReservationInfo") %>
      </div>

      <div class="mt-2 d-flex justify-content-between flex-wrap">
        <div class="muted small">
          Rented: <b><%# Eval("RentalDate","{0:yyyy-MM-dd HH:mm}") %></b><br />
          Returned: <b><%# Eval("ReturnedAtDisplay") %></b>
        </div>

        <div class="text-end">
          <div class="muted small">Unit Price</div>
          <div style="font-weight:900;">₱ <%# Eval("UnitPrice","{0:N2}") %></div>
          <div class="muted small">Paid: <b><%# Eval("IsPaidDisplay") %></b></div>
        </div>
      </div>
    </div>
  </ItemTemplate>
</asp:Repeater>

      </div>

      <div class="modal-footer">
        <button type="button" class="btn btn-outline-secondary btn-outline-soft" data-bs-dismiss="modal">Close</button>
      </div>
    </div>
  </div>
</div>
</asp:Content>



