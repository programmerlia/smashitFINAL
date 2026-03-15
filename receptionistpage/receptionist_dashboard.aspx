<%@ Page Language="C#" AutoEventWireup="true"
    MasterPageFile="~/receptionistpage/receptionist.Master"
    CodeBehind="receptionist_dashboard.aspx.cs"
    Inherits="Smash_IT.receptionistpage.receptionist_dashboard" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
    <style>
        /* ===== RECEPTIONIST DASHBOARD STYLES ===== */
        .dashboard-container {
            padding: 30px 40px;
            font-family: 'Poppins', sans-serif;
            color: #1e293b;
        }

        /* --- Welcome Header --- */
        .admin-intro {
            background: #ffffff;
            padding: 30px;
            border-radius: 12px;
            box-shadow: 0 4px 15px rgba(30, 58, 138, 0.05);
            margin-bottom: 30px;
            border-left: 6px solid #facc15;
        }

        .admin-intro h1 {
            margin: 0;
            color: #1e3a8a;
            font-size: 2rem;
            font-weight: 800;
            text-transform: uppercase;
        }

        .admin-intro p {
            margin: 10px 0 0;
            color: #64748b;
            font-size: 1rem;
            line-height: 1.6;
        }

        /* --- Section Title --- */
        .section-title {
            font-size: 1.2rem;
            font-weight: 700;
            color: #1e3a8a;
            margin-bottom: 25px;
            text-transform: uppercase;
            letter-spacing: 1px;
            display: flex;
            align-items: center;
            gap: 10px;
        }

        /* --- Dashboard Grid --- */
        .admin-actions {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: 25px;
        }

        .action-card {
            background: #ffffff;
            padding: 40px 25px;
            border-radius: 15px;
            box-shadow: 0 4px 15px rgba(30, 58, 138, 0.05);
            text-align: center;
            cursor: pointer;
            transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
            border: 1px solid #e2e8f0;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            min-height: 240px;
        }

        .action-card:hover {
            transform: translateY(-10px);
            box-shadow: 0 12px 25px rgba(30, 58, 138, 0.12);
            border-color: #facc15;
        }

        .action-card i {
            font-size: 3.5rem;
            color: #1e3a8a;
            margin-bottom: 20px;
            transition: all 0.3s ease;
        }

        .action-card:hover i {
            color: #facc15;
            transform: scale(1.1);
        }

        .action-card h3 {
            margin: 0 0 12px 0;
            color: #1e3a8a;
            font-size: 1.35rem;
            font-weight: 700;
            text-transform: uppercase;
        }

        .action-card p {
            margin: 0;
            color: #64748b;
            font-size: 0.95rem;
            line-height: 1.6;
            max-width: 250px;
        }

        @media (max-width: 768px) {
            .dashboard-container {
                padding: 20px;
            }

            .admin-actions {
                grid-template-columns: 1fr;
            }

            .admin-intro h1 {
                font-size: 1.6rem;
            }
        }
    </style>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">
    <div class="dashboard-container">

        <div class="admin-intro">
            <h1>Hi, <asp:Literal ID="litStaffName" runat="server" />!</h1>
            <p>
                Welcome to the Smash-IT Reception Command Center. Manage walk-ins, reservations,
                queue flow, and live court activity from one place.
            </p>
        </div>

        <h2 class="section-title">
            <i class="fas fa-th-large" style="color: #facc15;"></i>
            Reception Hub
        </h2>

        <div class="admin-actions">

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/receptionistpage/receptionist_dashboard.aspx") %>';">
                <i class="fas fa-home"></i>
                <h3>Dashboard</h3>
                <p>Open the receptionist overview and access daily front desk operations quickly.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/receptionistpage/rec_pfa.aspx") %>';">
                <i class="fas fa-person-running"></i>
                <h3>Play For All</h3>
                <p>Handle open play registrations, sessions, and player access at the front desk.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/receptionistpage/rec_res.aspx") %>';">
                <i class="fas fa-calendar-check"></i>
                <h3>Reservations</h3>
                <p>Manage court bookings, review schedules, and assist customer reservations.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/receptionistpage/rec_queue.aspx") %>';">
                <i class="fas fa-list-ol"></i>
                <h3>Queue</h3>
                <p>View and manage player queues to keep court assignments organized and smooth.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/receptionistpage/rec_court_override.aspx") %>';">
                <i class="fas fa-table-tennis-paddle-ball"></i>
                <h3>Manage Court</h3>
                <p>Set court availability, apply overrides, and control front desk court scheduling.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/receptionistpage/rec_live_courts.aspx") %>';">
                <i class="fas fa-table-tennis-paddle-ball"></i>
                <h3>Live Court</h3>
                <p>Monitor current court usage and check live session activity in real time.</p>
            </div>

        </div>
    </div>
</asp:Content>