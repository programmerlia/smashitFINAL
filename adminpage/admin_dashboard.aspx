<%@ Page Title="Dashboard | Smash-IT" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_dashboard.aspx.cs" Inherits="Smash_IT.adminpage.admin_dashboard" %>
<%@ MasterType VirtualPath="~/adminpage/admin.master" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
    <style>
        /* ===== DASHBOARD SPECIFIC STYLES ===== */
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
            border-left: 6px solid #facc15; /* Yellow Accent */
        }

        .admin-intro h1 {
            margin: 0;
            color: #1e3a8a; /* Deep Blue */
            font-size: 2rem;
            font-weight: 800;
            text-transform: uppercase;
        }

        .admin-intro p {
            margin: 10px 0 0;
            color: #64748b;
            font-size: 1rem;
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

        /* --- Management Hub Grid --- */
        .admin-actions {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
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
            min-height: 250px;
        }

        .action-card:hover {
            transform: translateY(-10px);
            box-shadow: 0 12px 25px rgba(30, 58, 138, 0.12);
            border-color: #facc15;
        }

        /* Icon Styling */
        .action-card i {
            font-size: 3.5rem;
            color: #1e3a8a;
            margin-bottom: 20px;
            transition: transform 0.3s ease;
        }

        .action-card:hover i {
            color: #facc15;
            transform: scale(1.1);
        }

        /* Text inside cards */
        .action-card h3 {
            margin: 0 0 12px 0;
            color: #1e3a8a;
            font-size: 1.4rem;
            font-weight: 700;
            text-transform: uppercase;
        }

        .action-card p {
            margin: 0;
            color: #64748b;
            font-size: 0.95rem;
            line-height: 1.6;
            max-width: 240px;
        }

        /* Responsive Tweak */
        @media (max-width: 768px) {
            .dashboard-container { padding: 20px; }
            .admin-actions { grid-template-columns: 1fr; }
        }
    </style>
</asp:Content>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="dashboard-container">
        
        <%-- 1. Welcome Header --%>
        <div class="admin-intro">
            <h1>Hi, <asp:Literal ID="litStaffName" runat="server" />!</h1>
            <p>Welcome to the Smash-It Command Center. Manage your courts, track players, and monitor system activity below.</p>
        </div>

        <h2 class="section-title"><i class="fas fa-th-large" style="color: #facc15;"></i>Management Hub</h2>

        <%-- 2. Management Hub Cards --%>
        <div class="admin-actions">
            
            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_court.aspx") %>';">
                <i class="fas fa-table-tennis-paddle-ball"></i>
                <h3>Manage Courts</h3>
                <p>Manage court availability and schedule time slots.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_playallyoucan.aspx") %>';">
                <i class="fas fa-person-running"></i>
                <h3>Play-All-You-Can</h3>
                <p>Configure open play sessions and pricing.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_queue.aspx") %>';">
                <i class="fas fa-list-ol"></i>
                <h3>Manage Event</h3>
                <p>Organize tournaments and special events.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_manage_queue.aspx") %>';">
                <i class="fas fa-users-line"></i>
                <h3>Manage Queue</h3>
                <p>Monitor and adjust the active player queue.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_reservation.aspx") %>';">
                <i class="fas fa-calendar-check"></i>
                <h3>Reservations</h3>
                <p>View and approve upcoming bookings.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_transactions.aspx") %>';">
                <i class="fas fa-file-invoice-dollar"></i>
                <h3>Transactions</h3>
                <p>Track payments and financial records.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_inventory.aspx") %>';">
                <i class="fas fa-boxes-stacked"></i>
                <h3>Inventory</h3>
                <p>Manage equipment and retail stock levels.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/Content_Manager.aspx") %>';">
                <i class="fas fa-bullhorn"></i>
                <h3>Announcements</h3>
                <p>Post announcements or maintenance alerts.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_customer_account.aspx") %>';">
                <i class="fas fa-users"></i>
                <h3>Customers</h3>
                <p>Manage player profiles and membership data.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_users.aspx") %>';">
                <i class="fas fa-user-shield"></i>
                <h3>Staff Team</h3>
                <p>Control staff access and administrative roles.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_analytics.aspx") %>';">
                <i class="fas fa-chart-line"></i>
                <h3>Analytics</h3>
                <p>Track revenue trends and peak court utilization.</p>
            </div>

        </div>
    </div>
</asp:Content>