<%@ Page Title="Reservation Manager" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_reservation.aspx.cs" Inherits="Smash_IT.adminpage.admin_reservation1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600;700;800&display=swap" rel="stylesheet" />
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet" />
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    <style>
        /* ===== GLOBAL THEME ===== */
        *, *::before, *::after { box-sizing: border-box; }
        .staff-container { padding: 30px 40px; font-family: 'Poppins', sans-serif; color: #1e293b; max-width: 100%; }

        /* ===== HEADER ===== */
        .header-flex {
            display: flex; justify-content: space-between; align-items: center; margin-bottom: 25px;
            background: #ffffff; padding: 20px 25px; border-radius: 12px;
            box-shadow: 0 4px 15px rgba(30, 58, 138, 0.05);
            border-left: 6px solid #facc15; 
        }
        .header-title h2 { color: #1e3a8a; font-weight: 800; margin: 0; font-size: 1.8rem; }
        .header-title p { color: #64748b; margin: 5px 0 0; font-size: 0.9rem; }

        .search-box-container { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
        .search-box {
            display: flex; background: #ffffff; border: 1px solid #cbd5e1;
            border-radius: 8px; padding: 0 15px; height: 45px; align-items: center;
        }
        .search-input { border: none !important; outline: none !important; font-size: 0.9rem; background: transparent; font-family: inherit; color: #1e3a8a; font-weight: 600; width: 100%; }

        /* ===== WORKSPACE LAYOUT ===== */
        .event-workspace-grid { display: grid; grid-template-columns: minmax(0, 1fr) 380px; gap: 25px; align-items: start; }
        @media (max-width: 1200px) {
            .event-workspace-grid { grid-template-columns: 1fr; }
            .sidebar-sticky { order: -1; position: relative !important; top: 0 !important; }
        }

        /* ===== SCHEDULE GRID OVERRIDES ===== */
        .event-scroll-container { 
            overflow-x: auto; background: #ffffff; border-radius: 12px; padding: 15px;
            box-shadow: 0 4px 15px rgba(30, 58, 138, 0.05); margin-bottom: 25px;
        }
        .event-scroll-container::-webkit-scrollbar { height: 8px; }
        .event-scroll-container::-webkit-scrollbar-track { background: #f1f5f9; border-radius: 4px; }
        .event-scroll-container::-webkit-scrollbar-thumb { background: #cbd5e1; border-radius: 4px; }

        .event-table-schedule { width: max-content !important; min-width: 100%; border-spacing: 4px !important; border-collapse: separate !important; table-layout: fixed; }
        .event-table-schedule th, .event-table-schedule td { width: 130px !important; min-width: 130px !important; max-width: 130px !important; padding: 4px; }
        
        .event-table-schedule th { background: #1e3a8a !important; color: #ffffff !important; font-size: 0.8rem; font-weight: 700; padding: 12px 5px; border-radius: 6px; text-align: center; }
        .event-court-col {
            background: #f8fafc !important; color: #1e3a8a !important; font-weight: 800 !important; font-size: 0.95rem;
            border-radius: 6px; border: 1px solid #e2e8f0 !important; position: sticky; left: 0; z-index: 10; text-align: center; vertical-align: middle;
        }

        /* SLOTS */
        .event-slot-cell {
            height: 65px; border-radius: 6px !important; cursor: pointer; transition: transform 0.1s ease, box-shadow 0.1s ease;
            display: flex; justify-content: center; align-items: center; box-sizing: border-box; font-family: 'Poppins', sans-serif;
            position: relative;
        }
        .event-slot-cell:hover { transform: translateY(-2px); box-shadow: 0 4px 8px rgba(0,0,0,0.08); }
        .event-cell-info { width: 100%; height: 100%; display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 4px; }
        .event-cell-title {
            font-size: 0.75rem; font-weight: 800; text-transform: uppercase; text-align: center; line-height: 1.2;
            word-break: break-word; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;
        }
        .event-cell-sub { font-size: 0.65rem; font-weight: 600; opacity: 0.8; margin-top: 2px; }

        .bg-white { background: #ffffff !important; border: 2px dashed #cbd5e1 !important; color: #94a3b8 !important; }
        .bg-white:hover { border-color: #facc15 !important; background: #fefce8 !important; color: #854d0e !important; }
        .bg-gray { background: #f1f5f9 !important; border: 1px solid #e2e8f0 !important; color: #64748b !important; cursor: not-allowed !important; pointer-events: none; }
        .bg-purple { background: #f3e8ff !important; border: 1px solid #a855f7 !important; color: #6b21a8 !important; }
        .bg-green { background: #dcfce7 !important; border: 1px solid #22c55e !important; color: #166534 !important; } 
        .bg-orange { background: #fef3c7 !important; border: 1px solid #f59e0b !important; color: #92400e !important; } 
        .bg-blue { background: #dbeafe !important; border: 1px solid #1e3a8a !important; color: #1e3a8a !important; } 
        .bg-red { background: #fee2e2 !important; border: 1px solid #ef4444 !important; color: #991b1b !important; }

        /* ===== LEGEND ===== */
        .event-legend-container { display: flex; flex-wrap: wrap; gap: 20px; padding: 15px 20px; font-size: 0.8rem; font-weight: 600; color: #475569; margin-bottom: 15px; }
        .event-legend-dot { width: 14px; height: 14px; border-radius: 4px; display: inline-block; vertical-align: middle; margin-right: 5px; }
        .dot-white { background: #ffffff; border: 2px dashed #cbd5e1; }
        .dot-gray { background: #f1f5f9; border: 1px solid #e2e8f0; }
        .dot-purple { background: #f3e8ff; border: 1px solid #a855f7; }
        .dot-green { background: #dcfce7; border: 1px solid #22c55e; }
        .dot-orange { background: #fef3c7; border: 1px solid #f59e0b; }
        .dot-blue { background: #dbeafe; border: 1px solid #1e3a8a; }
        .dot-red { background: #fee2e2; border: 1px solid #ef4444; }

        /* ===== ALL EVENTS GRID ===== */
        .grid-card { background: #ffffff; border-radius: 12px; padding: 20px; box-shadow: 0 4px 15px rgba(30, 58, 138, 0.05); }
        .custom-grid { width: 100%; border-collapse: collapse; }
        .custom-grid th { background: #1e3a8a; color: white; padding: 15px; text-align: left; font-size: 0.85rem; text-transform: uppercase; }
        .custom-grid td { padding: 12px 15px; border-bottom: 1px solid #f1f5f9; font-size: 0.9rem; vertical-align: middle; }
        
        .event-badge-status { padding: 5px 10px; border-radius: 6px; font-size: 0.75rem; font-weight: 700; text-transform: uppercase; display: inline-block; text-align: center; }
        .badge-badminton { background: #dcfce7; color: #166534; }
        .badge-pickleball { background: #fef3c7; color: #92400e; }

        .badge-pending { background-color: #f3e8ff; color: #6b21a8; }
        .badge-approved { background-color: #dcfce7; color: #166534; }
        .badge-cancelled { background-color: #fee2e2; color: #991b1b; }
        .badge-completed { background-color: #dbeafe; color: #1e3a8a; }
        .badge-refunded { background-color: #d1fae5; color: #065f46; }
        
        .edit-link { color: #1e3a8a; font-weight: 600; padding: 6px 12px; background: #eff6ff; border-radius: 6px; text-decoration: none; display: inline-flex; align-items: center; gap: 6px; cursor: pointer; border: none; outline: none; }
        .edit-link:hover { background: #dbeafe; }

        /* ===== SIDEBAR / INPUT PANEL ===== */
        .sidebar-sticky { position: sticky; top: 20px; }
        .input-card { background: #ffffff; padding: 25px; border-radius: 12px; box-shadow: 0 10px 25px rgba(30, 58, 138, 0.08); border-top: 5px solid #1e3a8a; }
        .input-group { margin-bottom: 15px; }
        .input-group label { display: block; font-size: 0.8rem; font-weight: 700; color: #1e3a8a; margin-bottom: 6px; text-transform: uppercase; }
        .input-group select, .input-group input { width: 100%; height: 45px; padding: 0 15px; border: 1px solid #e2e8f0; border-radius: 8px; font-family: inherit; font-size: 0.9rem; background: #f8fafc; }
        .input-group select:focus, .input-group input:focus { border-color: #facc15; outline: none; box-shadow: 0 0 0 3px rgba(250, 204, 21, 0.1); background: #ffffff; }
        
        .details-box { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 15px; margin-bottom: 15px; }
        .details-row { display: flex; justify-content: space-between; margin-bottom: 8px; font-size: 0.85rem; }
        .details-label { color: #64748b; font-weight: 600; }
        .details-value { color: #0f172a; font-weight: 700; text-align: right; }

        /* BUTTONS */
        .btn-approve { background: #10b981; color: white; height: 45px; width: 100%; border: none; border-radius: 8px; font-weight: 700; cursor: pointer; margin-bottom: 10px; font-size: 0.95rem; }
        .btn-approve:hover { background: #059669; }
        .btn-primary { background: #1e3a8a; color: white; height: 45px; width: 100%; border: none; border-radius: 8px; font-weight: 700; cursor: pointer; margin-bottom: 10px; }
        .btn-primary:hover { background: #172554; }
        .btn-secondary { background: #f1f5f9; color: #475569; height: 45px; width: 100%; border: 1px solid #cbd5e1; border-radius: 8px; font-weight: 700; cursor: pointer; margin-bottom: 10px; }
        .btn-secondary:hover { background: #e2e8f0; }
        .btn-add { background: #10b981; color: white; height: 45px; padding: 0 20px; border: none; border-radius: 8px; font-weight: 700; cursor: pointer; }
        .btn-today { background: #f8fafc; border: 1px solid #cbd5e1; padding: 0 15px; height: 45px; border-radius: 8px; color: #1e3a8a; font-weight: 700; cursor: pointer; }
        .btn-outline { background: transparent; color: #1e3a8a; border: 2px solid #1e3a8a; height: 45px; width: 100%; border-radius: 8px; font-weight: 700; cursor: pointer; margin-bottom: 10px; }
        .btn-outline:hover { background: #eff6ff; }

        .badge-pay-full { background: #10b981; color: white; padding: 3px 8px; border-radius: 4px; font-size: 0.7rem; font-weight: bold; }
        .badge-pay-half { background: #8b5cf6; color: white; padding: 3px 8px; border-radius: 4px; font-size: 0.7rem; font-weight: bold; }
        .badge-pay-unpaid { background: #ef4444; color: white; padding: 3px 8px; border-radius: 4px; font-size: 0.7rem; font-weight: bold; cursor: pointer; text-decoration: none;}
        .badge-pay-unpaid:hover { background: #dc2626; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="sm1" runat="server" />

    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="upMain">
        <ProgressTemplate>
            <div style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(255,255,255,0.6); z-index: 9999; display: flex; justify-content: center; align-items: center; backdrop-filter: blur(2px);">
                <div style="background: #1e3a8a; color: white; padding: 20px 40px; border-radius: 12px; font-weight: bold; box-shadow: 0 10px 25px rgba(0,0,0,0.2); font-family: 'Poppins', sans-serif; font-size: 1.1rem;">
                    <i class="fas fa-circle-notch fa-spin" style="margin-right: 12px; color: #facc15;"></i> Processing...
                </div>
            </div>
        </ProgressTemplate>
    </asp:UpdateProgress>

    <div class="staff-container">
        <asp:UpdatePanel ID="upMain" runat="server">
            <ContentTemplate>
                
                <%-- HEADER --%>
                <div class="header-flex">
                    <div class="header-title">
                        <h2>Reservation Review Hub</h2>
                        <p>Visually track slots, approve bookings, and manage downpayments.</p>
                    </div>
                    <div class="search-box-container">
                        <asp:TextBox ID="txtDate" runat="server" TextMode="Date" AutoPostBack="true" OnTextChanged="txtDate_TextChanged" CssClass="search-box" style="padding:0 15px; border:1px solid #cbd5e1; width: 150px;" />
                        <asp:Button ID="btnToday" runat="server" Text="Today" OnClick="btnToday_Click" CssClass="btn-today" style="width: auto; margin: 0; padding: 0 15px;" />
                        
                        <div style="display: flex; align-items: center; gap: 8px; margin-left: 10px; border-left: 2px solid #cbd5e1; padding-left: 15px;">
                            <asp:CheckBox ID="chkHideCancelled" runat="server" AutoPostBack="true" OnCheckedChanged="chkHideCancelled_CheckedChanged" />
                            <label style="font-size: 0.85rem; font-weight: 600; color: #475569; cursor: pointer;">Hide Cancelled Slots</label>
                        </div>

                        <asp:Button ID="btnShowAddForm" runat="server" Text="+ New Booking" OnClick="btnShowAddForm_Click" CssClass="btn-add" style="margin-left: 10px;" />
                    </div>
                </div>

                <div class="event-workspace-grid">
                    
                    <%-- MAIN BOARD --%>
                    <div class="event-main-board">
                        
                        <div class="event-legend-container">
                            <span><span class="event-legend-dot dot-white"></span>Open Slot</span>
                            <span><span class="event-legend-dot dot-gray"></span>Closed/Overlap</span>
                            <span><span class="event-legend-dot dot-purple"></span>Pending</span>
                            <span><span class="event-legend-dot dot-green"></span>Badminton</span>
                            <span><span class="event-legend-dot dot-orange"></span>Pickleball</span>
                            <span><span class="event-legend-dot dot-blue"></span>Tournament</span>
                            <span><span class="event-legend-dot dot-red"></span>Cancelled</span>
                        </div>

                        <%-- SCHEDULE VISUALIZER --%>
                        <div class="event-scroll-container">
                            <asp:GridView ID="gvScheduleGrid" runat="server" CssClass="event-table-schedule" OnRowDataBound="gvScheduleGrid_RowDataBound" GridLines="None" ShowHeader="true"></asp:GridView>
                        </div>

                        <%-- DATA GRID VIEW (MASTER ACTIVITIES) --%>
                        <div class="grid-card">
                            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px; border-bottom: 2px solid #f1f5f9; padding-bottom: 10px; flex-wrap: wrap; gap: 10px;">
                                <h4 style="color: #1e3a8a; font-weight: 800; margin: 0;">Master Activities List for <asp:Label ID="lblDgvDate" runat="server" /></h4>
                                
                                <%-- SEARCH AND FILTER COMBOBOX --%>
                                <div style="display: flex; align-items: center; gap: 10px;">
                                    <div class="search-box" style="width: 200px; height: 35px;">
                                        <asp:TextBox ID="txtSearch" runat="server" placeholder="Search name/ID..." CssClass="search-input" />
                                        <asp:LinkButton ID="btnSearch" runat="server" OnClick="btnSearch_Click" style="color: #1e3a8a;"><i class="fas fa-search"></i></asp:LinkButton>
                                    </div>

                                    <span style="font-size: 0.8rem; font-weight: 700; color: #64748b; text-transform: uppercase;">Filter:</span>
                                    <asp:DropDownList ID="ddlFilterStatus" runat="server" CssClass="search-box" style="padding:0 15px; border:1px solid #cbd5e1; width: 140px; height: 35px; font-size: 0.85rem;" AutoPostBack="true" OnSelectedIndexChanged="ddlFilterStatus_SelectedIndexChanged">
                                        <asp:ListItem Text="All Status" Value="All"></asp:ListItem>
                                        <asp:ListItem Text="Pending" Value="Pending"></asp:ListItem>
                                        <asp:ListItem Text="Approved" Value="Approved"></asp:ListItem>
                                        <asp:ListItem Text="Completed" Value="Completed"></asp:ListItem>
                                        <asp:ListItem Text="Cancelled" Value="Cancelled"></asp:ListItem>
                                      <asp:ListItem Text="Refunded" Value="Refunded"></asp:ListItem>
                                    </asp:DropDownList>
                                </div>
                            </div>
                            
                            <asp:GridView ID="gvReservations" runat="server" AutoGenerateColumns="False" CssClass="custom-grid" OnRowCommand="gvReservations_RowCommand" GridLines="None">
                                <Columns>
                                    <asp:TemplateField HeaderText="ID / Type">
                                        <ItemTemplate>
                                            <div style="font-weight: 700; color: #1e3a8a;">#<%# Eval("RefID") %></div>
                                            <div style="font-size: 0.75rem; color: #64748b; text-transform: uppercase;"><%# Eval("RecordType") %></div>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Customer Info">
                                        <ItemTemplate>
                                            <div style="font-weight: 700; color: #0f172a;"><%# Eval("MainTitle") %></div>
                                            <div style="font-size: 0.75rem; color: #64748b;"><i class='fas fa-user text-blue-500'></i> <%# Eval("SubTitle") %></div>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Schedule & Sport">
                                        <ItemTemplate>
                                            <div style="font-weight: 600;">Court <%# Eval("CourtNumber") %> 
                                                <span class='event-badge-status <%# Eval("SportName").ToString().ToUpper().Contains("PICKLE") ? "badge-pickleball" : "badge-badminton" %>' style="margin-left: 5px;">
                                                    <%# Eval("SportName") %>
                                                </span>
                                            </div>
                                            <div style="font-size: 0.8rem; color: #475569; margin-top: 4px;">
                                                <i class="far fa-clock"></i>
                                                <%# DateTime.Today.Add((TimeSpan)Eval("StartTime")).ToString("hh:mm tt") %> - 
                                                <%# DateTime.Today.Add((TimeSpan)Eval("EndTime")).ToString("hh:mm tt") %>
                                            </div>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Payment Status">
                                        <ItemTemplate>
                                            <div style="font-weight: 700; color: #1e3a8a;">
                                                ₱<%# (Eval("PaymentStatus").ToString() == "HalfPaid" ? Convert.ToDecimal(Eval("RequiredAmount")) / 2 : Convert.ToDecimal(Eval("RequiredAmount"))).ToString("0.00") %>
                                            </div>
                                            <span class='<%# Eval("PaymentStatus").ToString() == "FullyPaid" ? "badge-pay-full" : (Eval("PaymentStatus").ToString() == "HalfPaid" ? "badge-pay-half" : "badge-pay-unpaid") %>'>
                                                <%# Eval("PaymentStatus") %>
                                            </span>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Status">
                                        <ItemTemplate>
                                     <span class='event-badge-status <%# Eval("StatusName").ToString() == "Approved" ? "badge-approved" : (Eval("StatusName").ToString() == "Pending" ? "badge-pending" : (Eval("StatusName").ToString() == "Completed" ? "badge-completed" : (Eval("StatusName").ToString() == "Refunded" ? "badge-refunded" : "badge-cancelled"))) %>'>
    <%# Eval("StatusName") %>
</span>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField ItemStyle-HorizontalAlign="Right">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="btnReview" runat="server" CommandName="ReviewRes" CommandArgument='<%# Eval("RefID") %>' CssClass="edit-link">
                                                <i class='fas fa-clipboard-check'></i> Edit
                                            </asp:LinkButton>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                                <EmptyDataTemplate>
                                    <div style="text-align: center; padding: 40px; color: #94a3b8;">
                                        <i class="fas fa-calendar-times" style="font-size: 2rem; margin-bottom: 10px;"></i>
                                        <h5 style="margin: 0; font-weight: 700;">No reservations match this criteria.</h5>
                                    </div>
                                </EmptyDataTemplate>
                            </asp:GridView>
                        </div>
                        <div class="grid-card" style="margin-top:25px;">
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:15px;border-bottom:2px solid #f1f5f9;padding-bottom:10px;flex-wrap:wrap;gap:10px;">
        <h4 style="color:#b91c1c;font-weight:800;margin:0;"><i class="fas fa-rotate-left" style="margin-right:8px;color:#ef4444;"></i>Refund Queue</h4>
        <div style="font-size:0.85rem;color:#64748b;font-weight:600;">Reservations cancelled but not yet fully refunded</div>
    </div>

    <asp:GridView ID="gvRefunds" runat="server" AutoGenerateColumns="False" CssClass="custom-grid" GridLines="None" OnRowCommand="gvRefunds_RowCommand">
        <Columns>
            <asp:TemplateField HeaderText="Reservation">
                <ItemTemplate>
                    <div style="font-weight:700;color:#1e3a8a;">#<%# Eval("ReservationID") %></div>
                    <div style="font-size:0.75rem;color:#64748b;">Ref: <%# Eval("ReferenceText") %></div>
                </ItemTemplate>
            </asp:TemplateField>

            <asp:TemplateField HeaderText="Customer">
                <ItemTemplate>
                    <div style="font-weight:700;color:#0f172a;"><%# Eval("CustomerName") %></div>
                    <div style="font-size:0.8rem;color:#475569;"><%# Eval("UsernameText") %></div>
                </ItemTemplate>
            </asp:TemplateField>

            <asp:TemplateField HeaderText="Contact">
                <ItemTemplate>
                    <div><i class="fas fa-envelope" style="color:#64748b;margin-right:6px;"></i><%# Eval("Email") %></div>
                    <div style="margin-top:4px;"><i class="fas fa-phone" style="color:#64748b;margin-right:6px;"></i><%# Eval("PhoneNumber") %></div>
                </ItemTemplate>
            </asp:TemplateField>

            <asp:TemplateField HeaderText="Payment Made">
                <ItemTemplate>
                    <div style="font-weight:700;color:#1e3a8a;">₱<%# Convert.ToDecimal(Eval("PaidAmount")).ToString("0.00") %></div>
                    <span class='<%# Eval("PaymentStatus").ToString() == "FullyPaid" ? "badge-pay-full" : (Eval("PaymentStatus").ToString() == "HalfPaid" ? "badge-pay-half" : "badge-pay-unpaid") %>'>
                        <%# Eval("PaymentStatus") %>
                    </span>
                </ItemTemplate>
            </asp:TemplateField>

            <asp:TemplateField HeaderText="Refund Amount">
                <ItemTemplate>
                    <div style="font-weight:800;color:#b91c1c;font-size:1rem;">₱<%# Convert.ToDecimal(Eval("RefundAmount")).ToString("0.00") %></div>
                </ItemTemplate>
            </asp:TemplateField>

            <asp:TemplateField HeaderText="Instruction">
                <ItemTemplate>
                    <div style="font-size:0.82rem;line-height:1.45;color:#475569;max-width:320px;">
                        Please contact <b><%# Eval("CustomerName") %></b> and arrange a refund of their
                        <b><%# Eval("PaymentStatus") %></b> reservation payment.
                    </div>
                </ItemTemplate>
            </asp:TemplateField>

            <asp:TemplateField ItemStyle-HorizontalAlign="Right">
                <ItemTemplate>
                    <asp:LinkButton ID="btnMarkRefunded" runat="server" CommandName="MarkRefunded" CommandArgument='<%# Eval("ReservationID") %>' CssClass="edit-link" style="background:#fee2e2;color:#b91c1c;">
                        <i class="fas fa-check-circle"></i> Mark Refunded
                    </asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>
            <div style="text-align:center;padding:35px;color:#94a3b8;">
                <i class="fas fa-wallet" style="font-size:2rem;margin-bottom:10px;"></i>
                <h5 style="margin:0;font-weight:700;">No pending refunds.</h5>
            </div>
        </EmptyDataTemplate>
    </asp:GridView>
</div>
                    </div>

                    <%-- SIDEBAR CONFIGURATION --%>
                    <div class="sidebar-sticky">
                        <asp:Panel ID="pnlSidebarWrapper" runat="server" CssClass="input-card">
                            <h3 style="color: #1e3a8a; font-weight: 800; margin-bottom: 20px;">
                                <i class="fas fa-tasks" style="color: #facc15; margin-right: 8px;"></i> Action Panel
                            </h3>

                            <%-- STATE 1: EMPTY --%>
                            <asp:Panel ID="pnlEmptyState" runat="server" style="text-align: center; padding: 30px 0; color: #94a3b8;">
                                <i class="fas fa-hand-pointer" style="font-size: 2.5rem; color: #cbd5e1; margin-bottom: 15px;"></i>
                                <p style="margin: 0; font-weight: 600; font-size: 0.9rem;">Click an open slot or a reservation to review/create.</p>
                            </asp:Panel>

                            <%-- STATE 2: EDIT/MANAGE EXISTING RESERVATION --%>
                            <asp:Panel ID="pnlManageForm" runat="server" Visible="false">
                                <asp:HiddenField ID="hfSelectedResID" runat="server" />

                                <div class="details-box">
                                    <div style="font-size: 0.75rem; color: #64748b; text-transform: uppercase; font-weight: 800; margin-bottom: 10px; border-bottom: 1px solid #e2e8f0; padding-bottom: 5px;">Reservation #<asp:Label ID="lblSideResID" runat="server" /></div>
                                    
                                    <div class="details-row" style="margin-top: 10px; padding-top: 10px; border-top: 1px dashed #cbd5e1;">
                                        <span class="details-label" style="color: #1e3a8a;">Grand Total Due</span>
                                        <span class="details-value" style="color: #1e3a8a; font-size: 1.1rem;">₱<asp:Label ID="lblSideAmount" runat="server" /></span>
                                    </div>
                                    <div class="details-row" style="margin-bottom: 0;">
                                        <span class="details-label" style="font-size: 0.7rem;">Ref ID</span>
                                        <span class="details-value" style="font-size: 0.7rem; font-family: monospace; color: #94a3b8;"><asp:Label ID="lblSideRef" runat="server" /></span>
                                    </div>
                                </div>

                                <div class="input-group">
                                    <label>Customer Type</label>
                                    <asp:DropDownList ID="ddlEditCustomerType" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlEditCustomerType_SelectedIndexChanged">
                                        <asp:ListItem Text="Registered User" Value="User"></asp:ListItem>
                                        <asp:ListItem Text="Walk-In" Value="WalkIn"></asp:ListItem>
                                    </asp:DropDownList>
                                </div>
                                <asp:PlaceHolder ID="phEditRegisteredUser" runat="server">
                                    <div class="input-group">
                                        <label>Select User</label>
                                        <asp:DropDownList ID="ddlEditUsers" runat="server"></asp:DropDownList>
                                    </div>
                                </asp:PlaceHolder>
                                <asp:PlaceHolder ID="phEditWalkIn" runat="server" Visible="false">
                                    <div class="input-group"><label>First Name</label><asp:TextBox ID="txtEditFirstName" runat="server" /></div>
                                    <div class="input-group"><label>Last Name</label><asp:TextBox ID="txtEditLastName" runat="server" /></div>
                                </asp:PlaceHolder>

                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px;">
                                    <div class="input-group">
                                        <label>Court</label>
                                        <asp:DropDownList ID="ddlEditCourt" runat="server"></asp:DropDownList>
                                    </div>
                                    <div class="input-group">
                                        <label>Sport</label>
                                        <asp:DropDownList ID="ddlEditSport" runat="server">
                                            <asp:ListItem Text="Badminton" Value="badminton"></asp:ListItem>
                                            <asp:ListItem Text="Pickleball" Value="pickleball"></asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>

                                <div class="input-group">
                                    <label>Reservation Date</label>
                                    <asp:TextBox ID="txtEditDate" runat="server" TextMode="Date" />
                                </div>

                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px;">
                                    <div class="input-group">
                                        <label>Start Time</label>
                                        <asp:DropDownList ID="ddlEditStartTime" runat="server"></asp:DropDownList>
                                    </div>
                                    <div class="input-group">
                                        <label>Duration</label>
                                        <asp:DropDownList ID="ddlEditDuration" runat="server">
                                            <asp:ListItem Text="1 Hour" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="2 Hours" Value="2"></asp:ListItem>
                                            <asp:ListItem Text="3 Hours" Value="3"></asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>

                                <div class="input-group">
                                    <label>Reservation Status</label>
                                    <asp:DropDownList ID="ddlStatus" runat="server">
                                        <asp:ListItem Text="Pending Approval" Value="Pending" />
                                        <asp:ListItem Text="Approved" Value="Approved" />
                                        <asp:ListItem Text="Completed" Value="Completed" />
                                        <asp:ListItem Text="Cancelled" Value="Cancelled" />
                                    </asp:DropDownList>
                                </div>
                                <div class="input-group">
                                    <label>Payment Tracking</label>
                                    <asp:DropDownList ID="ddlPayment" runat="server">
                                        <asp:ListItem Text="Unpaid (0%)" Value="Unpaid" />
                                        <asp:ListItem Text="Half Paid / DP (50%)" Value="HalfPaid" />
                                        <asp:ListItem Text="Fully Paid (100%)" Value="FullyPaid" />
                                    </asp:DropDownList>
                                </div>

                                <div style="margin-top: 25px;">
                                    <asp:Button ID="btnApproveRes" runat="server" Text="✓ Approve Reservation" CssClass="btn-approve" OnClick="btnApproveRes_Click" Visible="false" />
                                    <asp:Button ID="btnManageExtras" runat="server" Text="Manage Extras (Rentals/Water)" CssClass="btn-outline" OnClick="btnManageExtras_Click" />
                                    <asp:Button ID="btnSaveUpdate" runat="server" Text="Apply Updates" CssClass="btn-primary" OnClick="btnSaveUpdate_Click" />
                                    <asp:Button ID="btnCloseSidebar" runat="server" Text="Cancel" CssClass="btn-secondary" OnClick="btnCloseSidebar_Click" />
                                </div>
                            </asp:Panel>

                            <%-- STATE 3: CREATE NEW RESERVATION --%>
                            <asp:Panel ID="pnlInputForm" runat="server" Visible="false">
                                <div style="font-size: 0.85rem; font-weight: 700; color: #1e3a8a; margin-bottom: 15px; border-bottom: 2px solid #f1f5f9; padding-bottom: 10px;">
                                    <i class="fas fa-plus-circle" style="color: #10b981; margin-right: 5px;"></i> Book New Slot
                                </div>

                                <div class="input-group">
                                    <label>Customer Type</label>
                                    <asp:DropDownList ID="ddlCustomerType" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlCustomerType_SelectedIndexChanged">
                                        <asp:ListItem Text="Registered User" Value="User"></asp:ListItem>
                                        <asp:ListItem Text="Walk-In" Value="WalkIn"></asp:ListItem>
                                    </asp:DropDownList>
                                </div>

                                <asp:PlaceHolder ID="phRegisteredUser" runat="server">
                                    <div class="input-group">
                                        <label>Select User</label>
                                        <asp:DropDownList ID="ddlUsers" runat="server"></asp:DropDownList>
                                    </div>
                                </asp:PlaceHolder>

                                <asp:PlaceHolder ID="phWalkIn" runat="server" Visible="false">
                                    <div class="input-group"><label>First Name</label><asp:TextBox ID="txtFirstName" runat="server" placeholder="e.g. John" /></div>
                                    <div class="input-group"><label>Last Name</label><asp:TextBox ID="txtLastName" runat="server" placeholder="e.g. Doe" /></div>
                                </asp:PlaceHolder>

                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px;">
                                    <div class="input-group">
                                        <label>Court</label>
                                        <asp:DropDownList ID="ddlCourt" runat="server"></asp:DropDownList>
                                    </div>
                                    <div class="input-group">
                                        <label>Sport</label>
                                        <asp:DropDownList ID="ddlSport" runat="server">
                                            <asp:ListItem Text="Badminton" Value="badminton"></asp:ListItem>
                                            <asp:ListItem Text="Pickleball" Value="pickleball"></asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>

                                <div class="input-group">
                                    <label>Reservation Date</label>
                                    <asp:TextBox ID="txtNewResDate" runat="server" TextMode="Date" />
                                </div>

                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px;">
                                    <div class="input-group">
                                        <label>Start Time</label>
                                        <asp:DropDownList ID="ddlNewStartTime" runat="server"></asp:DropDownList>
                                    </div>
                                    <div class="input-group">
                                        <label>Duration</label>
                                        <asp:DropDownList ID="ddlDuration" runat="server">
                                            <asp:ListItem Text="1 Hour" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="2 Hours" Value="2"></asp:ListItem>
                                            <asp:ListItem Text="3 Hours" Value="3"></asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>

                                <div class="input-group">
                                    <label>Payment Collection</label>
                                    <asp:DropDownList ID="ddlNewPaymentStatus" runat="server">
                                        <asp:ListItem Text="Unpaid (0%)" Value="Unpaid"></asp:ListItem>
                                        <asp:ListItem Text="Half Paid / DP (50%)" Value="HalfPaid"></asp:ListItem>
                                        <asp:ListItem Text="Fully Paid (100%)" Value="FullyPaid"></asp:ListItem>
                                    </asp:DropDownList>
                                </div>

                                <div style="margin-top: 25px;">
                                    <asp:Button ID="btnSaveNewRes" runat="server" Text="Confirm Booking" CssClass="btn-primary" OnClick="btnSaveNewRes_Click" />
                                    <asp:Button ID="btnCancelNewRes" runat="server" Text="Cancel" CssClass="btn-secondary" OnClick="btnCloseSidebar_Click" />
                                </div>
                            </asp:Panel>

                            <%-- STATE 4: MANAGE EXTRAS (RENTALS/CONSUMABLES) --%>
                            <asp:Panel ID="pnlExtrasForm" runat="server" Visible="false">
                                <div style="font-size: 0.85rem; font-weight: 700; color: #1e3a8a; margin-bottom: 15px; border-bottom: 2px solid #f1f5f9; padding-bottom: 10px;">
                                    <i class="fas fa-shopping-cart" style="color: #f59e0b; margin-right: 5px;"></i> Add Extras to Res #<asp:Label ID="lblExtrasResID" runat="server" />
                                </div>

                                <div class="input-group">
                                    <label>Select Item to Add</label>
                                    <asp:DropDownList ID="ddlEquipmentModel" runat="server"></asp:DropDownList>
                                </div>
                                <div class="input-group">
                                    <label>Quantity</label>
                                    <asp:TextBox ID="txtExtraQty" runat="server" TextMode="Number" min="1" max="50" Text="1" />
                                </div>
                                
                                <asp:Button ID="btnAddExtra" runat="server" Text="+ Add to Reservation" CssClass="btn-approve" OnClick="btnAddExtra_Click" />
                                
                                <div style="margin-top: 20px; border-top: 2px dashed #cbd5e1; padding-top: 15px;">
                                    
                                    <%-- RENTALS GRID --%>
                                    <label style="display: block; font-size: 0.85rem; font-weight: 700; color: #1e3a8a; margin-bottom: 6px;">Rentals (Equipment)</label>
                                    <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden; margin-bottom: 15px;">
                                        <asp:GridView ID="gvRentals" runat="server" AutoGenerateColumns="False" CssClass="custom-grid" GridLines="None" OnRowCommand="gvRentals_RowCommand">
                                            <Columns>
                                                <asp:BoundField DataField="ItemName" HeaderText="Item" />
                                                <asp:BoundField DataField="Price" HeaderText="Price" DataFormatString="₱{0:0.00}" />
                                                <asp:TemplateField ItemStyle-HorizontalAlign="Right">
                                                    <ItemTemplate>
                                                        <asp:LinkButton ID="btnReturn" runat="server" CommandName="ReturnItem" CommandArgument='<%# Eval("ExtraID") %>' 
                                                            Visible='<%# Eval("ReturnedAt") == DBNull.Value %>' 
                                                            CssClass="badge-pay-unpaid">Return</asp:LinkButton>
                                                        
                                                        <asp:Label ID="lblReturned" runat="server" 
                                                            Visible='<%# Eval("ReturnedAt") != DBNull.Value %>' 
                                                            CssClass="badge-pay-full">Returned</asp:Label>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                            </Columns>
                                            <EmptyDataTemplate>
                                                <div style="font-size: 0.8rem; color: #94a3b8; text-align: center; padding: 15px;">No rentals added.</div>
                                            </EmptyDataTemplate>
                                        </asp:GridView>
                                    </div>

                                    <%-- CONSUMABLES GRID --%>
                                    <label style="display: block; font-size: 0.85rem; font-weight: 700; color: #1e3a8a; margin-bottom: 6px;">Consumables (Drinks, Shuttlecocks, etc.)</label>
                                    <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;">
                                        <asp:GridView ID="gvConsumables" runat="server" AutoGenerateColumns="False" CssClass="custom-grid" GridLines="None">
                                            <Columns>
                                                <asp:BoundField DataField="ItemName" HeaderText="Item" />
                                                <asp:BoundField DataField="Qty" HeaderText="Qty" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                                <asp:BoundField DataField="Price" HeaderText="Total" DataFormatString="₱{0:0.00}" />
                                            </Columns>
                                            <EmptyDataTemplate>
                                                <div style="font-size: 0.8rem; color: #94a3b8; text-align: center; padding: 15px;">No consumables added.</div>
                                            </EmptyDataTemplate>
                                        </asp:GridView>
                                    </div>

                                </div>

                                <div style="margin-top: 25px;">
                                    <asp:Button ID="btnBackToRes" runat="server" Text="Back to Details" CssClass="btn-secondary" OnClick="btnBackToRes_Click" />
                                </div>
                            </asp:Panel>

                        </asp:Panel>
                    </div>

                </div>

            </ContentTemplate>
        </asp:UpdatePanel>
    </div>

    <%-- Hidden trigger for Schedule cell clicks --%>
    <asp:HiddenField ID="hfActionType" runat="server" />
    <asp:HiddenField ID="hfActionCourt" runat="server" />
    <asp:HiddenField ID="hfActionTime" runat="server" />
    <asp:HiddenField ID="hfActionResID" runat="server" />
    <asp:Button ID="btnHiddenTrigger" runat="server" OnClick="btnHiddenTrigger_Click" Style="display: none;" />

    <script>
        function triggerSidebarOpen(courtId, startTime) {
            document.getElementById('<%= hfActionType.ClientID %>').value = 'OPEN';
            document.getElementById('<%= hfActionCourt.ClientID %>').value = courtId;
            document.getElementById('<%= hfActionTime.ClientID %>').value = startTime;
            document.getElementById('<%= btnHiddenTrigger.ClientID %>').click();
        }

        function triggerSidebarRes(resId) {
            document.getElementById('<%= hfActionType.ClientID %>').value = 'RES';
            document.getElementById('<%= hfActionResID.ClientID %>').value = resId;
            document.getElementById('<%= btnHiddenTrigger.ClientID %>').click();
        }

        function showSweetAlert(icon, title, text) {
            Swal.fire({ icon: icon, title: title, text: text, confirmButtonColor: '#1e3a8a' });
        }

        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function (sender, args) {
            if (args.get_error() != undefined) {
                showSweetAlert('error', 'System Error', args.get_error().message);
                args.set_errorHandled(true);
            }
        });
    </script>
</asp:Content>