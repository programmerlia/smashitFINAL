<%@ Page Title="Admin Dashboard" Language="C#" 
    MasterPageFile="~/adminpage/admin.master"
    AutoEventWireup="true" 
    CodeBehind="admin_dashboard.aspx.cs" 
    Inherits="Smash_IT.adminpage.admin_dashboard" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" />
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/admin-dashboard.css") %>' />

    <div class="main-content">

        <%-- 1. Welcome Header --%>
        <div class="admin-intro">
            <h1>Hi Staff!</h1>
            <p>Welcome to Smash-It Court Operations. Monitor live activity and manage platform logistics below.</p>
            <ul>
                <li><i class="fas fa-check-circle"></i> Live Court Tracking</li>
                <li><i class="fas fa-check-circle"></i> Booking Management</li>
                <li><i class="fas fa-check-circle"></i> Account Controls</li>
            </ul>
        </div>

        <%-- 2. Court Live Monitor (Based on your provided Images) --%>
        <h2 class="section-title">Live Court Operations</h2>
        <div class="court-monitor-grid">
            <asp:Repeater ID="rptCourts" runat="server">
                <ItemTemplate>
                    <div class='<%# "court-card " + Eval("StatusClass") %>'>
                        <div class="court-header">
                            <span class="court-name"><%# Eval("CourtName") %></span>
                            <span class="sport-type"><%# Eval("Sport") %></span>
                        </div>
                        <div class="court-body">
                            <i class='<%# Eval("StatusIcon") %>'></i>
                            <span class="status-text"><%# Eval("StatusText") %></span>
                            <small class="player-name"><%# Eval("CurrentPlayer") %></small>
                        </div>
                        <div class="court-footer">
                            <asp:LinkButton ID="btnToggle" runat="server" CssClass="power-btn"><i class="fas fa-power-off"></i></asp:LinkButton>
                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </div>

        <%-- 3. Quick Action Navigation --%>
        <h2 class="section-title">Quick Management</h2>
        <div class="admin-actions">
            
            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_court.aspx") %>';">
                <i class="fas fa-table-tennis"></i>
                <h3>Court Setup</h3>
                <p>Configure court availability and switch between Badminton/Pickleball modes.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_reservation.aspx") %>';">
                <i class="fas fa-calendar-check"></i>
                <h3>Reservations</h3>
                <p>Monitor, approve, or cancel bookings and schedules efficiently.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_customer_account.aspx") %>';">
                <i class="fas fa-user-friends"></i>
                <h3>Customers</h3>
                <p>Manage registered players, view their history, and handle account issues.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_users.aspx") %>';">
                <i class="fas fa-users-cog"></i>
                <h3>Staff Accounts</h3>
                <p>Add or edit staff members and manage administrative access.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_analytics.aspx") %>';">
                <i class="fas fa-chart-line"></i>
                <h3>Analytics</h3>
                <p>Track court utilization and revenue performance insights.</p>
            </div>

            <div class="action-card" onclick="window.location.href='<%= ResolveUrl("~/adminpage/admin_announcement.aspx") %>';">
                <i class="fas fa-bullhorn"></i>
                <h3>Announcements</h3>
                <p>Publish updates and notifications to keep players informed.</p>
            </div>
        </div>
    </div>
</asp:Content>