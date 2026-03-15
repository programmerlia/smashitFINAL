<%@ Page Language="C#" AutoEventWireup="true"
    MasterPageFile="~/queuemasterpage/queuemaster.Master"
    CodeBehind="q_dashboard.aspx.cs"
    Inherits="Smash_IT.queuemasterpage.q_dashboard" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
    <style>
        /* ===== QUEUE MASTER DASHBOARD STYLES ===== */
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

        /* --- Dashboard Cards Grid --- */
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

        <!-- Welcome Header -->
        <div class="admin-intro">
        <h1>Hi, <asp:Literal ID="litStaffName" runat="server" />!</h1>
            <h1>Queueing Dashboard</h1>
            <p>
                Welcome to the Smash-IT Queue Command Center. See live courts, manage active queues,
                and oversee match flow from one place.
            </p>
        </div>

        <h2 class="section-title">
            <i class="fas fa-th-large" style="color: #facc15;"></i>
            Queue Management Hub
        </h2>

        <!-- Action Cards -->
        <div class="admin-actions">

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/queuemasterpage/q_dashboard.aspx") %>';">
                <i class="fas fa-home"></i>
                <h3>Dashboard</h3>
                <p>Open the queue master overview and access all queue operations quickly.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/queuemasterpage/q_live_court.aspx") %>';">
                <i class="fas fa-list-ol"></i>
                <h3>Live Court</h3>
                <p>Track court activity in real time and monitor the current live match status.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/queuemasterpage/q_queue.aspx") %>';">
                <i class="fas fa-table-tennis-paddle-ball"></i>
                <h3>Queue</h3>
                <p>View and manage the active queue to keep player flow organized and efficient.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/queuemasterpage/q_manage_queue.aspx") %>';">
                <i class="fas fa-people-arrows"></i>
                <h3>Matches</h3>
                <p>Manage player match assignments, rotations, and queue-to-court movement.</p>
            </div>

        </div>
    </div>
</asp:Content>