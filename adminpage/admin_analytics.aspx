<%@ Page Title="Analytics" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_analytics.aspx.cs" Inherits="Smash_IT.adminpage.admin_analytics1" %>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" />
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/admin-dashboard.css") %>' />

    <div class="main-content">
        <div class="admin-intro">
            <h1>Analytics Overview</h1>
            <p>Real-time insights for <strong><%= DateTime.Now.ToString("MMMM dd, yyyy") %></strong></p>
            <asp:Button ID="btnRefresh" runat="server" Text="Refresh Data" OnClick="Button1_Click" CssClass="refresh-btn" style="padding: 8px 15px; background: #1e3a8a; color: white; border: none; border-radius: 4px; cursor: pointer;" />
        </div>

        <h2 class="section-title">Daily Reservations Summary</h2>
        <div class="analytics-grid">
            <div class="stat-card blue">
                <i class="fas fa-clock"></i>
                <div class="stat-info">
                    <span class="label">Pending</span>
                    <asp:Label ID="lblPending" runat="server" Text="0" CssClass="value"></asp:Label>
                </div>
            </div>
            <div class="stat-card yellow">
                <i class="fas fa-money-bill-wave"></i>
                <div class="stat-info">
                    <span class="label">Paid</span>
                    <asp:Label ID="lblPaid" runat="server" Text="0" CssClass="value"></asp:Label>
                </div>
            </div>
            <div class="stat-card blue">
                <i class="fas fa-play-circle"></i>
                <div class="stat-info">
                    <span class="label">Happening</span>
                    <asp:Label ID="lblHappening" runat="server" Text="0" CssClass="value"></asp:Label>
                </div>
            </div>
            <div class="stat-card cancelled">
                <i class="fas fa-times-circle"></i>
                <div class="stat-info">
                    <span class="label">Cancelled</span>
                    <asp:Label ID="lblCancelled" runat="server" Text="0" CssClass="value"></asp:Label>
                </div>
            </div>
        </div>

        <div class="analytics-grid" style="margin-top: 20px;">
            <div class="stat-card yellow">
                <i class="fas fa-cash-register"></i>
                <div class="stat-info">
                    <span class="label">Total Revenue Today</span>
                    <asp:Label ID="lblRevenue" runat="server" Text="₱0.00" CssClass="value"></asp:Label>
                </div>
            </div>
            <div class="stat-card blue">
                <i class="fas fa-user-tie"></i>
                <div class="stat-info">
                    <span class="label">Staff Operations</span>
                    <asp:Label ID="lblStaffCount" runat="server" Text="0" CssClass="value"></asp:Label>
                </div>
            </div>
        </div>

        <div class="secondary-grid" style="margin-top: 20px;">
            <div class="data-box">
                <h3><i class="fas fa-list-ol"></i> Court Queue Status</h3>
                <div class="queue-stats">
                    <div class="q-item"><span>Waiting:</span> <asp:Label ID="lblWaiting" runat="server" Text="0"></asp:Label></div>
                    <div class="q-item"><span>Playing:</span> <asp:Label ID="lblPlaying" runat="server" Text="0"></asp:Label></div>
                    <div class="q-item"><span>Done:</span> <asp:Label ID="lblDone" runat="server" Text="0"></asp:Label></div>
                </div>
            </div>

            <div class="data-box highlight">
                <h3><i class="fas fa-walking"></i> Walk-In Players</h3>
                <div class="walkin-content">
                    <span class="big-num"><asp:Label ID="lblWalkInToday" runat="server" Text="0"></asp:Label></span>
                    <p>Total players for today</p>
                </div>
            </div>
        </div>

        <div class="secondary-grid" style="margin-top: 20px;">
            <div class="data-box">
                <h3><i class="fas fa-layer-group"></i> Court Utilization</h3>
                <div class="utilization-container">
                    <p>Total Courts: <asp:Label ID="lblTotalCourts" runat="server" Text="0"></asp:Label></p>
                    <div class="q-item"><span>Occupied:</span> <asp:Label ID="lblOccupied" runat="server" Text="0"></asp:Label></div>
                    <div class="q-item"><span>Available:</span> <asp:Label ID="lblAvailable" runat="server" Text="0"></asp:Label></div>
                </div>
            </div>

            <div class="data-box">
                <h3><i class="fas fa-trophy"></i> Frequent Players</h3>
                <asp:Repeater ID="rptTopPlayers" runat="server">
                    <HeaderTemplate><table style="width:100%; font-size: 0.9em; border-collapse: collapse;"></HeaderTemplate>
                    <ItemTemplate>
                        <tr style="border-bottom: 1px solid #eee;">
                            <td style="padding: 8px 0;"><%# Eval("FullName") %></td>
                            <td style="text-align: right; font-weight: bold;"><%# Eval("BookingCount") %> Visits</td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate></table></FooterTemplate>
                </asp:Repeater>
            </div>
        </div>

        <div class="data-box" style="margin-top: 20px;">
            <h3><i class="fas fa-history"></i> Recent Payments</h3>
            <asp:GridView ID="gvRecentPayments" runat="server" AutoGenerateColumns="false" CssClass="styled-table" GridLines="None" Width="100%">
                <Columns>
                    <asp:BoundField DataField="PaymentDate" HeaderText="Time" DataFormatString="{0:hh:mm tt}" />
                    <asp:BoundField DataField="FullName" HeaderText="Customer" />
                    <asp:BoundField DataField="Amount" HeaderText="Amount" DataFormatString="₱{0:N2}" />
                    <asp:BoundField DataField="PaymentStatus" HeaderText="Status" />
                </Columns>
            </asp:GridView>
        </div>
    </div>
</asp:Content>