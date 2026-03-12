<%@ Page Title="Analytics" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_analytics.aspx.cs" Inherits="Smash_IT.adminpage.admin_analytics1" EnableEventValidation="false" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
    <link href="https://fonts.googleapis.com/css2?family=Poppins:wght@400;600;700;800&display=swap" rel="stylesheet">
    <style>
        /* ===== ANALYTICS THEME OVERRIDE ===== */
        .main-content {
            padding: 30px 40px;
            font-family: 'Poppins', sans-serif;
            color: #1e293b;
        }

        /* --- Header Section --- */
        .admin-intro {
            background: #ffffff;
            padding: 25px;
            border-radius: 12px;
            box-shadow: 0 4px 15px rgba(30, 58, 138, 0.05);
            margin-bottom: 25px;
            border-left: 6px solid #facc15;
        }
        .admin-intro h1 { font-size: 1.8rem; font-weight: 800; color: #1e3a8a; margin: 0; text-transform: uppercase; }
        .admin-intro p { color: #64748b; font-size: 0.95rem; margin-top: 5px; }

        .date-input {
            padding: 10px;
            border: 1px solid #cbd5e1;
            border-radius: 8px;
            font-family: inherit;
            outline: none;
        }

        /* --- KPI Grid --- */
        .analytics-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }

        .stat-card {
            background: #ffffff;
            padding: 25px;
            border-radius: 12px;
            display: flex;
            align-items: center;
            gap: 20px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.05);
        }

        .stat-card i {
            width: 60px;
            height: 60px;
            border-radius: 12px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.8rem;
            color: #fff;
        }

        /* Color Brands */
        .brand-blue i { background: linear-gradient(135deg, #1e3a8a, #3b82f6); }
        .brand-yellow i { background: linear-gradient(135deg, #f59e0b, #facc15); color: #1e3a8a; }
        .brand-red i { background: linear-gradient(135deg, #be123c, #e11d48); }

        .stat-info .label { font-size: 0.75rem; color: #64748b; font-weight: 700; text-transform: uppercase; letter-spacing: 1px; }
        .stat-info .value { font-size: 1.8rem; font-weight: 800; color: #1e3a8a; display: block; }

        /* --- Secondary Data Boxes --- */
        .secondary-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }

        .data-box {
            background: #ffffff;
            padding: 25px;
            border-radius: 12px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.05);
            border-top: 5px solid #1e3a8a;
        }

        .data-box h3 {
            font-size: 1rem;
            font-weight: 800;
            color: #1e3a8a;
            text-transform: uppercase;
            margin: 0 0 20px 0;
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .u-item {
            display: flex;
            justify-content: space-between;
            padding: 10px 0;
            border-bottom: 1px solid #f1f5f9;
            font-size: 0.9rem;
        }

        /* --- Table Styling --- */
        .table-card {
            background: #fff;
            border-radius: 12px;
            padding: 20px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.05);
        }

        .custom-grid { width: 100%; border-collapse: collapse; }
        .custom-grid th { 
            background: #1e3a8a; color: #fff; padding: 15px; 
            text-align: left; font-size: 0.8rem; text-transform: uppercase; 
        }
        .custom-grid td { padding: 15px; border-bottom: 1px solid #f1f5f9; font-size: 0.9rem; }

        /* Buttons */
        .action-btn {
            border: none;
            padding: 10px 20px;
            border-radius: 8px;
            font-weight: 700;
            font-family: inherit;
            cursor: pointer;
            text-decoration: none;
            display: inline-flex;
            align-items: center;
            gap: 8px;
            transition: 0.2s;
            font-size: 0.85rem;
        }
        .btn-excel { background: #15803d; color: #fff; }
        .btn-refresh { background: #1e3a8a; color: #fff; }
        .action-btn:hover { opacity: 0.9; transform: translateY(-1px); }

        .badge { padding: 4px 10px; border-radius: 6px; font-weight: 800; font-size: 0.7rem; text-transform: uppercase; }
        .bg-info { background: #eff6ff; color: #1e40af; }
        .bg-success { background: #dcfce7; color: #166534; }
        .bg-warn { background: #fef9c3; color: #854d0e; }
        .bg-neutral { background: #f1f5f9; color: #475569; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="main-content">
        <%-- 1. Header Section --%>
        <div class="admin-intro">
            <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 20px;">
                <div>
                    <h1><i class="fas fa-chart-pie" style="color: #facc15;"></i> Performance Analytics</h1>
                    <p>Metrics for: <strong><asp:Label ID="lblDisplayDate" runat="server"></asp:Label></strong></p>
                </div>
                <div style="display: flex; gap: 10px; align-items: center;">
                    <asp:TextBox ID="txtAnalyticsDate" runat="server" TextMode="Date" AutoPostBack="true" OnTextChanged="DateChanged" CssClass="date-input"></asp:TextBox>
                    <asp:LinkButton ID="btnExportExcel" runat="server" OnClick="btnExportExcel_Click" CssClass="action-btn btn-excel">
                        <i class="fas fa-file-excel"></i> Export
                    </asp:LinkButton>
                    <asp:LinkButton ID="btnRefresh" runat="server" OnClick="RefreshButton_Click" CssClass="action-btn btn-refresh">
                        <i class="fas fa-sync-alt"></i>
                    </asp:LinkButton>
                </div>
            </div>
        </div>

        <%-- 2. KPI Row --%>
        <div class="analytics-grid">
            <div class="stat-card brand-blue">
                <i class="fas fa-walking"></i>
                <div class="stat-info">
                    <span class="label">Walk-In Courts</span>
                    <asp:Label ID="lblQueueWalkIn" runat="server" Text="0" CssClass="value"></asp:Label>
                </div>
            </div>
            <div class="stat-card brand-yellow">
                <i class="fas fa-calendar-check"></i>
                <div class="stat-info">
                    <span class="label">Reservations</span>
                    <asp:Label ID="lblQueueRes" runat="server" Text="0" CssClass="value"></asp:Label>
                </div>
            </div>
            <div class="stat-card brand-red">
                <i class="fas fa-users"></i>
                <div class="stat-info">
                    <span class="label">Total Check-ins</span>
                    <asp:Label ID="lblQueuePaid" runat="server" Text="0" CssClass="value"></asp:Label>
                </div>
            </div>
        </div>

        <%-- 3. Detailed Data --%>
        <div class="secondary-grid">
            <div class="data-box">
                <h3><i class="fas fa-wallet" style="color: #16a34a;"></i> Revenue Breakdown</h3>
                <div style="margin-bottom: 20px;">
                    <span class="label" style="font-size: 0.7rem; color: #64748b;">Gross Income</span>
                    <asp:Label ID="lblRevenue" runat="server" Text="₱0.00" Style="display: block; font-size: 2rem; font-weight: 800; color: #1e3a8a;"></asp:Label>
                </div>
                <div class="u-item"><span>Court Usage Fees</span><asp:Label ID="lblRevCourts" runat="server" Text="₱0.00" Font-Bold="true"></asp:Label></div>
                <div class="u-item"><span>Equipment Rentals</span><asp:Label ID="lblRevRentals" runat="server" Text="₱0.00" Font-Bold="true"></asp:Label></div>
                <div class="u-item"><span>Consumables (Shuttlecocks/Balls)</span><asp:Label ID="lblRevConsumables" runat="server" Text="₱0.00" Font-Bold="true"></asp:Label></div>
                <div class="u-item"><span>Play-All-You-Can (PAYC)</span><asp:Label ID="lblRevPAYC" runat="server" Text="₱0.00" Font-Bold="true"></asp:Label></div>
            </div>

            <div class="data-box">
                <h3><i class="fas fa-tasks" style="color: #3b82f6;"></i> Booking Status</h3>
                <div class="u-item"><span>Confirmed & Completed</span><asp:Label ID="lblApproved" runat="server" CssClass="badge bg-success" Text="0"></asp:Label></div>
                <div class="u-item"><span>Pending Review</span><asp:Label ID="lblPending" runat="server" CssClass="badge bg-info" Text="0"></asp:Label></div>
                <div class="u-item"><span>Void / Cancelled</span><asp:Label ID="lblCancelled" runat="server" CssClass="badge brand-red" Text="0" Style="background: #fee2e2; color: #b91c1c;"></asp:Label></div>
            </div>

            <div class="data-box">
                <h3><i class="fas fa-fire" style="color: #f59e0b;"></i> Sport Popularity</h3>
                <div class="u-item"><span>Badminton</span><asp:Label ID="lblBadmintonCount" runat="server" CssClass="badge bg-info" Text="0"></asp:Label></div>
                <div class="u-item"><span>Pickleball</span><asp:Label ID="lblPickleballCount" runat="server" CssClass="badge bg-warn" Text="0"></asp:Label></div>
            </div>
        </div>

        <%-- 4. Recent Transactions --%>
        <div class="table-card">
            <h3 style="font-size: 1rem; font-weight: 800; color: #1e3a8a; text-transform: uppercase; margin-bottom: 20px;"><i class="fas fa-receipt" style="color:#facc15"></i> Recent Ledger Entries</h3>
            <div style="overflow-x: auto;">
                <asp:GridView ID="gvRecentPayments" runat="server" AutoGenerateColumns="false" CssClass="custom-grid" GridLines="None" EmptyDataText="No transactions recorded for this date.">
                    <Columns>
                        <asp:BoundField DataField="PaymentDate" HeaderText="Time" DataFormatString="{0:hh:mm tt}" />
                        <asp:BoundField DataField="PayerName" HeaderText="Customer" HeaderStyle-CssClass="fw-bold" />
                        <asp:BoundField DataField="PaymentTypeName" HeaderText="Type" />
                        <asp:BoundField DataField="Amount" HeaderText="Amount" DataFormatString="₱{0:N2}" ItemStyle-Font-Bold="true" ItemStyle-ForeColor="#1e3a8a" />
                        <asp:TemplateField HeaderText="Status">
                            <ItemTemplate>
                                <span class="badge <%# Eval("PaymentStatus").ToString() == "Completed" ? "bg-success" : (Eval("PaymentStatus").ToString() == "Pending" ? "bg-warn" : "bg-neutral") %>">
                                    <%# Eval("PaymentStatus") %>
                                </span>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </div>
</asp:Content>