<%@ Page Language="C#" AutoEventWireup="true"
    MasterPageFile="~/homepage/homepage.Master"
    CodeBehind="profile.aspx.cs"
    Inherits="Smash_IT.homepage.profile" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
    <style>
        :root {
            --navy: #1a187c;
            --navy-dark: #12115c;
            --navy-soft: #eef0ff;
            --yellow: #f4c542;
            --yellow-soft: #fff8df;
            --green-soft: #eefaf2;
            --red-soft: #fde1e3;
            --gray-soft: #f7f8fc;
            --border-soft: #e7eaf5;
            --text-soft: #6c757d;
        }

        .profile-hero {
            background: linear-gradient(135deg, var(--navy) 0%, #23239d 100%);
            padding: 42px 18px 34px;
            color: #fff;
            border-radius: 0 0 30px 30px;
            margin-bottom: 28px;
            position: relative;
            overflow: hidden;
        }

        .profile-hero::before {
            content: "";
            position: absolute;
            right: -40px;
            top: -40px;
            width: 180px;
            height: 180px;
            background: rgba(255,255,255,.08);
            border-radius: 50%;
        }

        .profile-hero::after {
            content: "";
            position: absolute;
            left: -30px;
            bottom: -60px;
            width: 160px;
            height: 160px;
            background: rgba(244,197,66,.14);
            border-radius: 50%;
        }

        .avatar-wrap {
            width: 96px;
            height: 96px;
            border-radius: 50%;
            overflow: hidden;
            background: #fff;
            border: 4px solid rgba(255,255,255,.75);
            box-shadow: 0 8px 24px rgba(0,0,0,.18);
            margin: 0 auto 14px;
            position: relative;
            z-index: 2;
        }

        .avatar-wrap img {
            width: 100%;
            height: 100%;
            object-fit: cover;
        }

        .cardx {
            background: #fff;
            border: 1px solid var(--border-soft);
            border-radius: 20px;
            box-shadow: 0 12px 28px rgba(16, 24, 40, 0.06);
            padding: 20px;
            margin-bottom: 16px;
        }

        .cardx h4 {
            font-size: 1.05rem;
            font-weight: 800;
            margin-bottom: 12px;
            display: inline-block;
            border-bottom: 3px solid var(--yellow);
            padding-bottom: 6px;
            color: var(--navy);
        }

        .section-subtext {
            color: var(--text-soft);
            font-size: .92rem;
        }

        .info-row {
            display: flex;
            justify-content: space-between;
            gap: 12px;
            padding: 12px 0;
            border-bottom: 1px solid #eff1f7;
            align-items: center;
        }

        .info-row:last-child {
            border-bottom: 0;
        }

        .info-label {
            color: var(--text-soft);
            font-weight: 500;
        }

        .info-value {
            color: #222;
            font-weight: 600;
            text-align: right;
        }

        .btn-success-soft {
            background: linear-gradient(135deg, var(--navy) 0%, #23239d 100%);
            color: #fff;
            border: 0;
            border-radius: 999px;
            padding: 10px 16px;
            font-weight: 600;
        }

        .btn-success-soft:hover {
            color: #fff;
            opacity: .96;
        }

        .btn-danger-soft {
            background: #dc3545;
            color: #fff;
            border: 0;
            border-radius: 999px;
            padding: 10px 16px;
            font-weight: 600;
        }

        .btn-outline-soft {
            border-radius: 999px;
            font-weight: 600;
            border-color: var(--navy);
            color: var(--navy);
        }

        .btn-outline-soft:hover {
            background: var(--navy);
            color: #fff;
        }

        .stats-grid .stat {
            background: linear-gradient(180deg, #fff 0%, #fafbff 100%);
            border-radius: 18px;
            padding: 16px;
            text-align: center;
            height: 100%;
            border: 1px solid var(--border-soft);
            box-shadow: 0 8px 18px rgba(26,24,124,.04);
        }

        .stat-icon {
            width: 46px;
            height: 46px;
            margin: 0 auto 10px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            background: var(--navy-soft);
            color: var(--navy);
            font-size: 1.2rem;
            font-weight: 900;
        }

        .stat .n {
            font-size: 1.35rem;
            font-weight: 900;
            color: var(--navy);
            line-height: 1.1;
        }

        .stat .t {
            font-size: .82rem;
            color: var(--text-soft);
            margin-top: 6px;
            line-height: 1.25;
            font-weight: 700;
        }

        .quick-actions {
            display: flex;
            justify-content: space-evenly;
            margin-top: 14px;
            gap: 8px;
            flex-wrap: wrap;
        }

        .res-clean {
            background: linear-gradient(180deg, #ffffff 0%, #fbfbff 100%);
            border: 1px solid #e8eaf7;
            border-left: 5px solid var(--navy);
            border-radius: 18px;
            padding: 16px;
            margin-bottom: 14px;
            box-shadow: 0 10px 24px rgba(26,24,124,.06);
        }

        .res-clean-top {
            display: flex;
            justify-content: space-between;
            gap: 14px;
            align-items: flex-start;
            flex-wrap: wrap;
        }

        .res-main-title {
            font-size: 1.08rem;
            font-weight: 900;
            color: var(--navy);
            line-height: 1.2;
        }

        .res-subline {
            color: var(--text-soft);
            font-size: .92rem;
            margin-top: 4px;
            font-weight: 700;
        }

        .res-meta-row {
            display: flex;
            flex-wrap: wrap;
            gap: 8px;
            margin-top: 10px;
        }

        .pill-chip {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 7px 12px;
            border-radius: 999px;
            font-size: .82rem;
            font-weight: 800;
            border: 1px solid transparent;
        }

        .pill-sport {
            background: var(--navy-soft);
            color: var(--navy);
            border-color: #d9ddff;
        }

        .pill-date {
            background: var(--yellow-soft);
            color: #7a5a00;
            border-color: #f1df98;
        }

        .pill-time {
            background: var(--green-soft);
            color: #1c6d39;
            border-color: #cae6d3;
        }

        .badge-status {
            font-size: .78rem;
            padding: .42rem .72rem;
            border-radius: 999px;
            font-weight: 900;
        }

        .badge-pending {
            background: #fff4cc;
            color: #8a6500;
            border: 1px solid #f4dfa0;
        }

        .badge-approved {
            background: #dcecff;
            color: #104e94;
            border: 1px solid #bdd8ff;
        }

        .badge-cancelled {
            background: #fde1e3;
            color: #922c34;
            border: 1px solid #f6bcc3;
        }

        .badge-completed {
            background: #daf4e3;
            color: #1d6a3a;
            border: 1px solid #b8e3c8;
        }
        .badge-refunded {
    background: #e0f7ea;
    color: #166534;
    border: 1px solid #b7ebc9;
}

        .badge-request {
            background: #ececf3;
            color: #454b57;
            border: 1px solid #d8dce6;
        }

        .payment-box {
            min-width: 220px;
            background: linear-gradient(135deg, #f8f9ff 0%, #fffdf4 100%);
            border: 1px solid #ebe7c9;
            border-radius: 16px;
            padding: 12px 14px;
        }

        .payment-label {
            font-size: .74rem;
            color: #7d8494;
            font-weight: 800;
            text-transform: uppercase;
            letter-spacing: .04em;
        }

        .payment-paid {
            font-size: 1.06rem;
            font-weight: 900;
            color: var(--navy);
            margin-top: 3px;
        }

        .payment-status-line {
            font-size: .85rem;
            font-weight: 800;
            margin-top: 5px;
            color: #3d4658;
        }

        .payment-balance {
            font-size: .84rem;
            font-weight: 900;
            color: #a14d00;
            margin-top: 4px;
        }

        .extras-box {
            margin-top: 12px;
            background: var(--gray-soft);
            border: 1px dashed #d9deef;
            border-radius: 14px;
            padding: 12px;
        }

        .extras-title {
            font-size: .78rem;
            font-weight: 900;
            color: var(--navy);
            margin-bottom: 6px;
            text-transform: uppercase;
            letter-spacing: .04em;
        }

        .extras-line {
            font-size: .88rem;
            color: #4f5666;
            margin-bottom: 4px;
        }

        .res-actions {
            display: flex;
            gap: 8px;
            flex-wrap: wrap;
            margin-top: 12px;
        }

        .muted {
            color: var(--text-soft);
        }

        .modal-content {
            border: 0;
            border-radius: 20px;
            overflow: hidden;
            box-shadow: 0 20px 50px rgba(15,23,42,.18);
        }

        .modal-header {
            background: linear-gradient(135deg, var(--navy) 0%, #23239d 100%);
            color: #fff;
            border-bottom: 0;
        }

        .modal-header .btn-close {
            filter: invert(1);
        }

        .group-title {
            font-weight: 900;
            color: var(--navy);
            background: var(--yellow-soft);
            border: 1px solid #f1df98;
            padding: 10px 14px;
            border-radius: 12px;
            margin-bottom: 10px;
        }

        .payments-table thead th {
            background: #f7f8fd;
            color: var(--navy);
            font-weight: 800;
            border-bottom: 0;
        }

        .payments-table tbody tr:hover {
            background: #fafbff;
        }

        .mini-card {
            border: 1px solid #eceff6;
            border-radius: 16px;
            padding: 14px;
            background: linear-gradient(180deg, #fff 0%, #fcfcff 100%);
            margin-bottom: 12px;
        }

        .mini-card-title {
            font-weight: 900;
            color: var(--navy);
            margin-bottom: 4px;
        }

        .mini-price {
            font-weight: 900;
            color: var(--navy-dark);
            font-size: 1rem;
        }

        .nav-tabs {
            border-bottom: 1px solid #e8ebf3;
            gap: 6px;
        }

        .nav-tabs .nav-link {
            border: 0;
            border-radius: 999px;
            color: var(--navy);
            font-weight: 800;
            background: #f4f6fd;
            padding: 8px 14px;
        }

        .nav-tabs .nav-link.active {
            background: var(--navy);
            color: #fff;
        }

        @media (max-width: 767px) {
            .payment-box {
                width: 100%;
                min-width: unset;
            }

            .info-row {
                flex-direction: column;
                align-items: flex-start;
            }

            .info-value {
                text-align: left;
            }
        }
    </style>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <!-- HERO -->
    <div class="profile-hero text-center" style="margin-top:80px;">
        <div class="container position-relative" style="z-index:2;">
            <div class="avatar-wrap">
                <asp:Image ID="imgAvatar" runat="server" />
            </div>

            <div style="font-size:1.75rem; font-weight:900;">
                <asp:Label ID="lblFirstname" runat="server" />
                <span> </span>
                <asp:Label ID="lblLastname" runat="server" />
            </div>

            <div style="opacity:.92; font-weight:600;">
                <asp:Label ID="lblEmail" runat="server" />
            </div>
        </div>
    </div>

    <div class="container" style="margin-top:-10px;">
        <asp:Label ID="lblMsg" runat="server" EnableViewState="false" />

        <div class="row g-3">

            <!-- LEFT -->
            <div class="col-lg-8">

                <!-- PERSONAL INFO -->
                <div class="cardx">
                    <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
                        <h4 class="mb-0">Personal Information</h4>
                        <button type="button" class="btn btn-outline-soft" data-bs-toggle="modal" data-bs-target="#editProfileModal">
                            Edit Profile
                        </button>
                    </div>

                    <div class="mt-3">
                        <div class="info-row">
                            <span class="info-label">Full Name</span>
                            <span class="info-value">
                                <asp:Label ID="lblFirstname2" runat="server" />
                                <span> </span>
                                <asp:Label ID="lblLastname2" runat="server" />
                            </span>
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

                <!-- MY RESERVATIONS -->
                <div class="cardx">
                    <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
                        <div>
                            <h4 class="mb-0">My Reservations</h4>
                            <div class="section-subtext mt-1">Your next reservation is highlighted below.</div>
                        </div>

                        <a class="btn btn-outline-soft" href='<%= ResolveUrl("~/homepage/reservation.aspx") %>'>
                            Book Another
                        </a>
                    </div>

                    <div class="mt-3">
                        <asp:Panel ID="pnlPendingTop1" runat="server" />

                        <button type="button"
                            class="btn btn-outline-soft mt-2"
                            data-bs-toggle="modal"
                            data-bs-target="#allResModal">
                            View all reservations
                        </button>
                    </div>
                </div>

            </div>

            <!-- RIGHT -->
            <div class="col-lg-4">

                <!-- STATS -->
                <div class="cardx">
                    <h4>Your Stats</h4>

                    <div class="row g-2 mt-2 stats-grid">
                        <div class="col-6">
                            <div class="stat">
                                <div class="stat-icon">📅</div>
                                <div class="n"><asp:Label ID="lblTotalBookings" runat="server" Text="0" /></div>
                                <div class="t">Total Bookings</div>
                            </div>
                        </div>

                        <div class="col-6">
                            <div class="stat">
                                <div class="stat-icon">⏳</div>
                                <div class="n"><asp:Label ID="lblPendingCount" runat="server" Text="0" /></div>
                                <div class="t">Pending</div>
                            </div>
                        </div>

                        <div class="col-6">
                            <div class="stat">
                                <div class="stat-icon">🏸</div>
                                <div class="n"><asp:Label ID="lblHoursPlayed" runat="server" Text="0" /></div>
                                <div class="t">Hours Played</div>
                            </div>
                        </div>

                        <div class="col-6">
                            <div class="stat">
                                <div class="stat-icon">🎒</div>
                                <div class="n"><asp:Label ID="lblTotalRentals" runat="server" Text="0" /></div>
                                <div class="t">Total Rentals</div>
                            </div>
                        </div>

                        <div class="col-6">
                            <div class="stat">
                                <div class="stat-icon">🧃</div>
                                <div class="n"><asp:Label ID="lblTotalConsumables" runat="server" Text="0" /></div>
                                <div class="t">Items Bought</div>
                            </div>
                        </div>

                        <div class="col-6">
                            <div class="stat">
                                <div class="stat-icon">💸</div>
                                <div class="n"><asp:Label ID="lblSpentThisWeek" runat="server" Text="₱ 0.00" /></div>
                                <div class="t">Spent This Week</div>
                            </div>
                        </div>

                        <div class="col-12">
                            <div class="stat">
                                <div class="stat-icon">📈</div>
                                <div class="n"><asp:Label ID="lblSpentThisMonth" runat="server" Text="₱ 0.00" /></div>
                                <div class="t">Spent This Month</div>
                            </div>
                        </div>
                    </div>

                    <div class="quick-actions">
                        <button type="button" class="btn btn-outline-soft"
                            data-bs-toggle="modal" data-bs-target="#paymentsModal">
                            Payments
                        </button>

                        <button type="button" class="btn btn-outline-soft"
                            data-bs-toggle="modal" data-bs-target="#rentalsModal">
                            Rentals
                        </button>

                        <button type="button" class="btn btn-outline-soft"
                            data-bs-toggle="modal" data-bs-target="#consumablesModal">
                            Consumables
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
                    <a class="btn btn-outline-soft w-100" href="mailto:smashitsportscenter@gmail.com">
                        Email Support
                    </a>
                </div>

            </div>
        </div>
    </div>

    <!-- EDIT PROFILE MODAL -->
    <div class="modal fade" id="editProfileModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content" style="border-radius:18px;">
                <div class="modal-header">
                    <h5 class="modal-title" style="font-weight:900;">Edit Profile</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>

                <div class="modal-body">
                    <div class="mb-3">
                        <label class="form-label fw-bold">Phone Number</label>
                        <asp:TextBox ID="txtPhone" runat="server" CssClass="form-control" />
                    </div>

                    <div class="mb-3">
                        <label class="form-label fw-bold">Username</label>
                        <asp:TextBox ID="txtUsername" runat="server" CssClass="form-control" />
                    </div>

                    <hr />

                    <div class="mb-3">
                        <label class="form-label fw-bold">New Password (optional)</label>
                        <asp:TextBox ID="txtNewPassword" runat="server" TextMode="Password" CssClass="form-control" />
                    </div>

                    <div class="mb-3">
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
                    <button type="button" class="btn btn-outline-soft" data-bs-dismiss="modal">Close</button>
                    <asp:Button ID="btnSaveProfile" runat="server" CssClass="btn-success-soft" Text="Save Changes" OnClick="btnSaveProfile_Click" />
                </div>
            </div>
        </div>
    </div>

    <!-- RESERVATIONS MODAL -->
    <div class="modal fade" id="allResModal" tabindex="-1">
        <div class="modal-dialog modal-xl modal-dialog-scrollable modal-dialog-centered">
            <div class="modal-content">

                <div class="modal-header">
                    <h5 class="modal-title" style="font-weight:900;">All Reservations</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>

                <div class="modal-body">

                    <ul class="nav nav-tabs mb-3" role="tablist">
                        <li class="nav-item" role="presentation">
                            <button class="nav-link active" data-bs-toggle="tab" data-bs-target="#tabApproved" type="button" role="tab">Approved</button>
                        </li>
                        <li class="nav-item" role="presentation">
                            <button class="nav-link" data-bs-toggle="tab" data-bs-target="#tabPending" type="button" role="tab">Pending</button>
                        </li>
                        <li class="nav-item" role="presentation">
                            <button class="nav-link" data-bs-toggle="tab" data-bs-target="#tabRequests" type="button" role="tab">Requests</button>
                        </li>
                        <li class="nav-item" role="presentation">
                            <button class="nav-link" data-bs-toggle="tab" data-bs-target="#tabCancelled" type="button" role="tab">Cancelled</button>
                        </li>
                        <li class="nav-item" role="presentation">
                            <button class="nav-link" data-bs-toggle="tab" data-bs-target="#tabCompleted" type="button" role="tab">Completed</button>
                        </li>
                        <li class="nav-item" role="presentation">
    <button class="nav-link" data-bs-toggle="tab" data-bs-target="#tabRefunded" type="button" role="tab">Refunded</button>
</li>
                    </ul>

                    <div class="tab-content pt-1">

                        <!-- APPROVED -->
                        <div class="tab-pane fade show active" id="tabApproved" role="tabpanel">
                            <asp:Repeater ID="rptApproved" runat="server"
                                OnItemCommand="rptReservations_ItemCommand"
                                OnItemDataBound="rptReservations_ItemDataBound">
                                <ItemTemplate>
                                    <div class="res-clean">
                                        <div class="res-clean-top">
                                            <div>
                                                <div class="res-main-title">Court <%# Eval("CourtNumber") %></div>
                                                <div class="res-subline"><%# Eval("SportName") %></div>

                                                <div class="res-meta-row">
                                                    <span class="pill-chip pill-date">📅 <%# Eval("FriendlyDate") %></span>
                                                    <span class="pill-chip pill-time">🕒 <%# Eval("TimeRangeDisplay") %></span>
                                                    <span class='badge-status <%# Eval("StatusBadgeClass") %>'><%# Eval("ReservationStatusName") %></span>
                                                    <%# Convert.ToString(Eval("RequestStatus")).Trim().Length > 0
                                                        ? "<span class='badge-status badge-request'>Request: " + HttpUtility.HtmlEncode(Convert.ToString(Eval("RequestStatus"))) + "</span>"
                                                        : "" %>
                                                </div>
                                            </div>

                                            <div class="payment-box text-end">
                                                <div class="payment-label">Paid so far</div>
                                                <div class="payment-paid"><%# Eval("PaidAmountDisplay") %></div>
                                                <div class="payment-status-line"><%# Eval("PaymentStatusDisplay") %></div>
                                                <div class="payment-balance"><%# Eval("PaymentBalanceDisplay") %></div>
                                            </div>
                                        </div>

                                        <div class="extras-box">
                                            <div class="extras-title">Added items</div>
                                            <div class="extras-line"><%# Convert.ToString(Eval("ExtrasDisplay")) %></div>
                                        </div>

                                        <div class="res-actions">
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
                                    <div class="res-clean">
                                        <div class="res-clean-top">
                                            <div>
                                                <div class="res-main-title">Court <%# Eval("CourtNumber") %></div>
                                                <div class="res-subline"><%# Eval("SportName") %></div>

                                                <div class="res-meta-row">
                                                    <span class="pill-chip pill-date">📅 <%# Eval("FriendlyDate") %></span>
                                                    <span class="pill-chip pill-time">🕒 <%# Eval("TimeRangeDisplay") %></span>
                                                    <span class='badge-status <%# Eval("StatusBadgeClass") %>'><%# Eval("ReservationStatusName") %></span>
                                                    <%# Convert.ToString(Eval("RequestStatus")).Trim().Length > 0
                                                        ? "<span class='badge-status badge-request'>Request: " + HttpUtility.HtmlEncode(Convert.ToString(Eval("RequestStatus"))) + "</span>"
                                                        : "" %>
                                                </div>
                                            </div>

                                            <div class="payment-box text-end">
                                                <div class="payment-label">Paid so far</div>
                                                <div class="payment-paid"><%# Eval("PaidAmountDisplay") %></div>
                                                <div class="payment-status-line"><%# Eval("PaymentStatusDisplay") %></div>
                                                <div class="payment-balance"><%# Eval("PaymentBalanceDisplay") %></div>
                                            </div>
                                        </div>

                                        <div class="extras-box">
                                            <div class="extras-title">Added items</div>
                                            <div class="extras-line"><%# Convert.ToString(Eval("ExtrasDisplay")) %></div>
                                        </div>

                                        <div class="res-actions">
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
                                    <div class="res-clean">
                                        <div class="res-clean-top">
                                            <div>
                                                <div class="res-main-title">Court <%# Eval("CourtNumber") %></div>
                                                <div class="res-subline"><%# Eval("SportName") %></div>

                                                <div class="res-meta-row">
                                                    <span class="pill-chip pill-date">📅 <%# Eval("FriendlyDate") %></span>
                                                    <span class="pill-chip pill-time">🕒 <%# Eval("TimeRangeDisplay") %></span>
                                                    <span class='badge-status <%# Eval("StatusBadgeClass") %>'><%# Eval("ReservationStatusName") %></span>
                                                    <%# Convert.ToString(Eval("RequestStatus")).Trim().Length > 0
                                                        ? "<span class='badge-status badge-request'>Request: " + HttpUtility.HtmlEncode(Convert.ToString(Eval("RequestStatus"))) + "</span>"
                                                        : "" %>
                                                </div>
                                            </div>

                                            <div class="payment-box text-end">
                                                <div class="payment-label">Paid so far</div>
                                                <div class="payment-paid"><%# Eval("PaidAmountDisplay") %></div>
                                                <div class="payment-status-line"><%# Eval("PaymentStatusDisplay") %></div>
                                                <div class="payment-balance"><%# Eval("PaymentBalanceDisplay") %></div>
                                            </div>
                                        </div>

                                        <div class="extras-box">
                                            <div class="extras-title">Added items</div>
                                            <div class="extras-line"><%# Convert.ToString(Eval("ExtrasDisplay")) %></div>
                                        </div>

                                        <div class="res-actions">
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
                                    <div class="res-clean">
                                        <div class="res-clean-top">
                                            <div>
                                                <div class="res-main-title">Court <%# Eval("CourtNumber") %></div>
                                                <div class="res-subline"><%# Eval("SportName") %></div>

                                                <div class="res-meta-row">
                                                    <span class="pill-chip pill-date">📅 <%# Eval("FriendlyDate") %></span>
                                                    <span class="pill-chip pill-time">🕒 <%# Eval("TimeRangeDisplay") %></span>
                                                    <span class='badge-status <%# Eval("StatusBadgeClass") %>'><%# Eval("ReservationStatusName") %></span>
                                                    <%# Convert.ToString(Eval("RequestStatus")).Trim().Length > 0
                                                        ? "<span class='badge-status badge-request'>Request: " + HttpUtility.HtmlEncode(Convert.ToString(Eval("RequestStatus"))) + "</span>"
                                                        : "" %>
                                                </div>
                                            </div>

                                            <div class="payment-box text-end">
                                                <div class="payment-label">Paid so far</div>
                                                <div class="payment-paid"><%# Eval("PaidAmountDisplay") %></div>
                                                <div class="payment-status-line"><%# Eval("PaymentStatusDisplay") %></div>
                                                <div class="payment-balance"><%# Eval("PaymentBalanceDisplay") %></div>
                                            </div>
                                        </div>

                                        <div class="extras-box">
                                            <div class="extras-title">Added items</div>
                                            <div class="extras-line"><%# Convert.ToString(Eval("ExtrasDisplay")) %></div>
                                        </div>

                                        <div class="res-actions">
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
                                    <div class="res-clean">
                                        <div class="res-clean-top">
                                            <div>
                                                <div class="res-main-title">Court <%# Eval("CourtNumber") %></div>
                                                <div class="res-subline"><%# Eval("SportName") %></div>

                                                <div class="res-meta-row">
                                                    <span class="pill-chip pill-date">📅 <%# Eval("FriendlyDate") %></span>
                                                    <span class="pill-chip pill-time">🕒 <%# Eval("TimeRangeDisplay") %></span>
                                                    <span class='badge-status <%# Eval("StatusBadgeClass") %>'><%# Eval("ReservationStatusName") %></span>
                                                    <%# Convert.ToString(Eval("RequestStatus")).Trim().Length > 0
                                                        ? "<span class='badge-status badge-request'>Request: " + HttpUtility.HtmlEncode(Convert.ToString(Eval("RequestStatus"))) + "</span>"
                                                        : "" %>
                                                </div>
                                            </div>

                                            <div class="payment-box text-end">
                                                <div class="payment-label">Paid so far</div>
                                                <div class="payment-paid"><%# Eval("PaidAmountDisplay") %></div>
                                                <div class="payment-status-line"><%# Eval("PaymentStatusDisplay") %></div>
                                                <div class="payment-balance"><%# Eval("PaymentBalanceDisplay") %></div>
                                            </div>
                                        </div>

                                        <div class="extras-box">
                                            <div class="extras-title">Added items</div>
                                            <div class="extras-line"><%# Convert.ToString(Eval("ExtrasDisplay")) %></div>
                                        </div>

                                        <div class="res-actions">
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

                            <asp:Panel ID="pnlEmptyCompleted" runat="server" Visible="false">
                                <div class="muted">No completed reservations.</div>
                            </asp:Panel>
                        </div>

                        <!-- REDUNDED-->
                        <div class="tab-pane fade" id="tabRefunded" role="tabpanel">
    <asp:Repeater ID="rptRefunded" runat="server"
        OnItemCommand="rptReservations_ItemCommand"
        OnItemDataBound="rptReservations_ItemDataBound">
        <ItemTemplate>
            <div class="res-clean">
                <div class="res-clean-top">
                    <div>
                        <div class="res-main-title">Court <%# Eval("CourtNumber") %></div>
                        <div class="res-subline"><%# Eval("SportName") %></div>

                        <div class="res-meta-row">
                            <span class="pill-chip pill-date">📅 <%# Eval("FriendlyDate") %></span>
                            <span class="pill-chip pill-time">🕒 <%# Eval("TimeRangeDisplay") %></span>
                            <span class='badge-status <%# Eval("StatusBadgeClass") %>'><%# Eval("ReservationStatusName") %></span>
                        </div>
                    </div>

                    <div class="payment-box text-end">
                        <div class="payment-label">Previously paid</div>
                        <div class="payment-paid"><%# Eval("PaidAmountDisplay") %></div>
                        <div class="payment-status-line"><%# Eval("PaymentStatusDisplay") %></div>
                        <div class="payment-balance"><%# Eval("PaymentBalanceDisplay") %></div>
                    </div>
                </div>

                <div class="extras-box">
                    <div class="extras-title">Added items</div>
                    <div class="extras-line"><%# Convert.ToString(Eval("ExtrasDisplay")) %></div>
                </div>

                <asp:Label ID="lblRuleHint" runat="server" CssClass="muted small d-block mt-2" />
            </div>
        </ItemTemplate>
    </asp:Repeater>

    <asp:Panel ID="pnlEmptyRefunded" runat="server" Visible="false">
        <div class="muted">No refunded reservations.</div>
    </asp:Panel>
</div>

                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- PAYMENTS MODAL -->
    <div class="modal fade" id="paymentsModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-xl modal-dialog-scrollable modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" style="font-weight:900;">My Payments</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlPaymentsEmpty" runat="server" Visible="false">
                        <div class="muted">No payments yet.</div>
                    </asp:Panel>

                    <asp:Repeater ID="rptPayments" runat="server">
                        <ItemTemplate>
                            <div class="mb-4">
                                <div class="group-title"><%# Eval("GroupTitle") %></div>

                                <asp:Repeater ID="rptPaymentsInner" runat="server" DataSource='<%# Eval("Items") %>'>
                                    <HeaderTemplate>
                                        <div class="table-responsive">
                                            <table class="table table-sm align-middle payments-table">
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
                                            <td><%# Eval("PaymentDate") == null ? "—" : ((DateTime)Eval("PaymentDate")).ToString("dddd, MMM d, yyyy") %></td>
                                            <td style="font-weight:800; color:#1a187c;"><%# Eval("PaymentTypeName") %></td>
                                            <td class="muted">
                                                <div style="font-weight:800; color:#333;"><%# Eval("DetailsLine1") %></div>
                                                <div class="small muted"><%# Eval("DetailsLine2") %></div>
                                            </td>
                                            <td class="text-end" style="font-weight:900; color:#12115c;">₱ <%# Eval("AmountPhp","{0:N2}") %></td>
                                        </tr>
                                    </ItemTemplate>

                                    <FooterTemplate>
                                                </tbody>
                                            </table>
                                        </div>
                                    </FooterTemplate>
                                </asp:Repeater>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>

                <div class="modal-footer">
                    <button type="button" class="btn btn-outline-soft" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

    <!-- RENTALS MODAL -->
    <div class="modal fade" id="rentalsModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg modal-dialog-scrollable modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" style="font-weight:900;">My Rentals</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlRentalsEmpty" runat="server" Visible="false">
                        <div class="muted">No rentals yet.</div>
                    </asp:Panel>

                    <asp:Repeater ID="rptRentals" runat="server">
                        <ItemTemplate>
                            <div class="mini-card">
                                <div class="mini-card-title"><%# Eval("ItemName") %></div>
                                <div class="small muted mb-2"><%# Eval("ReservationInfo") %></div>

                                <div class="d-flex justify-content-between flex-wrap gap-2">
                                    <div class="small muted">
                                        Rented: <b><%# Eval("RentalDate","{0:dddd, MMM d, yyyy hh:mm tt}") %></b><br />
                                        Returned: <b><%# Eval("ReturnedAtDisplay") %></b>
                                    </div>

                                    <div class="text-end">
                                        <div class="small muted">Unit Price</div>
                                        <div class="mini-price">₱ <%# Eval("UnitPrice","{0:N2}") %></div>
                                        <div class="small muted">Paid: <b><%# Eval("IsPaidDisplay") %></b></div>
                                    </div>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>

                <div class="modal-footer">
                    <button type="button" class="btn btn-outline-soft" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

    <!-- CONSUMABLES MODAL -->
    <div class="modal fade" id="consumablesModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg modal-dialog-scrollable modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" style="font-weight:900;">My Consumables</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlConsumablesEmpty" runat="server" Visible="false">
                        <div class="muted">No consumable purchases yet.</div>
                    </asp:Panel>

                    <asp:Repeater ID="rptConsumables" runat="server">
                        <ItemTemplate>
                            <div class="mini-card">
                                <div class="mini-card-title"><%# Eval("ItemName") %></div>
                                <div class="small muted mb-2"><%# Eval("ReservationInfo") %></div>

                                <div class="d-flex justify-content-between flex-wrap gap-2">
                                    <div class="small muted">
                                        Purchased: <b><%# Eval("PurchaseDate","{0:dddd, MMM d, yyyy hh:mm tt}") %></b><br />
                                        Quantity: <b><%# Eval("Quantity") %></b>
                                    </div>

                                    <div class="text-end">
                                        <div class="small muted">Unit Price</div>
                                        <div class="mini-price">₱ <%# Eval("UnitPrice","{0:N2}") %></div>
                                        <div class="small muted">Total: <b>₱ <%# Eval("TotalPrice","{0:N2}") %></b></div>
                                        <div class="small muted">Paid: <b><%# Eval("IsPaidDisplay") %></b></div>
                                    </div>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>

                <div class="modal-footer">
                    <button type="button" class="btn btn-outline-soft" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

</asp:Content>