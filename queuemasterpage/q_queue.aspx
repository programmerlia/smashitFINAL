 <%@ Page Title="Queue" Language="C#" MasterPageFile="~/queuemasterpage/queuemaster.master" AutoEventWireup="true" CodeBehind="q_queue.aspx.cs" Inherits="Smash_IT.queuemasterpage.q_queue" EnableEventValidation="false" %>


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
        .badge-event { background: #dbeafe; color: #1e3a8a; }

        .edit-link { color: #1e3a8a; font-weight: 600; padding: 6px 12px; background: #eff6ff; border-radius: 6px; text-decoration: none; display: inline-flex; align-items: center; gap: 6px; cursor: pointer; border: none; outline: none; }
        .edit-link:hover { background: #dbeafe; }

        /* ===== SIDEBAR / INPUT PANEL ===== */
        .sidebar-sticky { position: sticky; top: 20px; }
        .input-card { background: #ffffff; padding: 25px; border-radius: 12px; box-shadow: 0 10px 25px rgba(30, 58, 138, 0.08); border-top: 5px solid #1e3a8a; }
        .input-group { margin-bottom: 15px; }
        .input-group label { display: block; font-size: 0.8rem; font-weight: 700; color: #1e3a8a; margin-bottom: 6px; text-transform: uppercase; }
        .input-group select, .input-group input { width: 100%; height: 45px; padding: 0 15px; border: 1px solid #e2e8f0; border-radius: 8px; font-family: inherit; font-size: 0.9rem; background: #f8fafc; }
        .input-group select:focus, .input-group input:focus { border-color: #facc15; outline: none; box-shadow: 0 0 0 3px rgba(250, 204, 21, 0.1); background: #ffffff; }
        
        .event-checkbox-list { width: 100%; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 10px; }
        .event-checkbox-list td { padding: 6px 8px; width: 50%; }
        .event-checkbox-list label { margin-left: 8px; font-weight: 600; color: #1e293b; font-size: 0.85rem; cursor: pointer; }

        /* BUTTONS */
        .save-btn { background: #1e3a8a; color: white; height: 45px; width: 100%; border: none; border-radius: 8px; font-weight: 700; cursor: pointer; margin-bottom: 10px; }
        .save-btn:hover { background: #172554; }
        .delete-btn { background: #fff1f2; color: #e11d48; height: 45px; width: 100%; border: none; border-radius: 8px; font-weight: 700; cursor: pointer; margin-bottom: 10px; }
        .remove-slot-btn { background: #fef9c3; color: #a16207; height: 45px; width: 100%; border: 1px solid #fde047; border-radius: 8px; font-weight: 700; cursor: pointer; }
        .btn-today { background: #f8fafc; border: 1px solid #cbd5e1; padding: 0 15px; height: 45px; border-radius: 8px; color: #1e3a8a; font-weight: 700; cursor: pointer; }

        .event-summary-card { background: #f8fafc; border-left: 5px solid #facc15; padding: 12px; margin-bottom: 10px; border-radius: 8px; border-right: 1px solid #e2e8f0; border-top: 1px solid #e2e8f0; border-bottom: 1px solid #e2e8f0; }
        .event-summary-court { font-size: 0.7rem; font-weight: 800; color: #64748b; text-transform: uppercase; margin-bottom: 3px; }
        .event-summary-players { font-size: 0.9rem; font-weight: 700; color: #0f172a; }
        .event-summary-winner { font-size: 0.8rem; font-weight: 700; color: #166534; margin-top: 4px; }
        
        .rec-box { background: #f0fdf4; border: 1px solid #bbf7d0; color: #166534; padding: 10px; border-radius: 8px; font-size: 0.8rem; margin-bottom: 15px; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    
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
                        <h2>Event Master Schedule</h2>
                        <p>Manage tournaments, group events, and block off courts.</p>
                    </div>
                    <div class="search-box-container">
                        <asp:TextBox ID="txtDate" runat="server" TextMode="Date" AutoPostBack="true" OnTextChanged="txtDate_TextChanged" CssClass="search-box" style="padding:0 15px; border:1px solid #cbd5e1; width: 150px;" />
                        <asp:Button ID="btnToday" runat="server" Text="Today" OnClick="btnToday_Click" CssClass="btn-today" style="width: auto; margin: 0; padding: 0 15px;" />
                        
                        <div style="display: flex; align-items: center; gap: 8px; margin-left: 10px; border-left: 2px solid #cbd5e1; padding-left: 15px;">
                            <asp:CheckBox ID="chkHideCancelled" runat="server" AutoPostBack="true" OnCheckedChanged="chkHideCancelled_CheckedChanged" />
                            <label style="font-size: 0.85rem; font-weight: 600; color: #475569; cursor: pointer;">Hide Cancelled Slots</label>
                        </div>
                    </div>
                </div>

                <div class="event-workspace-grid">
                    
                    <%-- MAIN BOARD --%>
                    <div class="event-main-board">
                        
                        <div class="event-legend-container">
                            <span><span class="event-legend-dot dot-white"></span>Open Slot</span>
                            <span><span class="event-legend-dot dot-gray"></span>Closed/Overlap</span>
                            <span><span class="event-legend-dot dot-purple"></span>Pending Res.</span>
                            <span><span class="event-legend-dot dot-green"></span>Badminton Res.</span>
                            <span><span class="event-legend-dot dot-orange"></span>Pickleball Res.</span>
                            <span><span class="event-legend-dot dot-blue"></span>Tournament/Event</span>
                            <span><span class="event-legend-dot dot-red"></span>Cancelled</span>
                        </div>

                        <%-- SCHEDULE VISUALIZER --%>
                        <div class="event-scroll-container">
                            <asp:GridView ID="gvScheduleGrid" runat="server" CssClass="event-table-schedule" OnRowDataBound="gvScheduleGrid_RowDataBound" GridLines="None" ShowHeader="true"></asp:GridView>
                        </div>

                        <%-- DATA GRID VIEW (EVENTS ONLY) --%>
                        <div class="grid-card">
                            <h4 style="color: #1e3a8a; font-weight: 800; margin-bottom: 15px;">All Active Events on <asp:Label ID="lblDgvDate" runat="server" /></h4>
                            <asp:GridView ID="gvAllEvents" runat="server" AutoGenerateColumns="False" CssClass="custom-grid" DataKeyNames="EventID" OnRowCommand="gvAllEvents_RowCommand" GridLines="None">
                                <Columns>
                                    <asp:TemplateField HeaderText="Event ID">
                                        <ItemTemplate>
                                            <div style="font-weight: 700; color: #1e3a8a;">#<%# Eval("EventID") %></div>
                                            <div style="font-size: 0.75rem; color: #64748b; text-transform: uppercase;">EVT</div>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Event Info">
                                        <ItemTemplate>
                                            <div style="font-weight: 700; color: #0f172a;"><%# Eval("Title") %></div>
                                            <div style="font-size: 0.75rem; color: #64748b;"><i class="fas fa-users"></i> Max Players: <%# Eval("MaxPlayers") %></div>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Sport">
                                        <ItemTemplate>
                                            <span class='event-badge-status <%# Eval("SportName").ToString().ToUpper().Contains("PICKLE") ? "badge-pickleball" : "badge-badminton" %>'>
                                                <%# Eval("SportName") %>
                                            </span>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Entry Fee">
                                        <ItemTemplate>
                                            <div style="font-weight: 700; color: #166534;">₱<%# Convert.ToDecimal(Eval("RegistrationFee")).ToString("0.00") %></div>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField ItemStyle-HorizontalAlign="Right">
                                        <ItemTemplate>
                                            <asp:LinkButton ID="btnEditEvt" runat="server" CommandName="EditEvent" CommandArgument='<%# Eval("EventID") %>' CssClass="edit-link">
                                                <i class="fas fa-cog"></i> Configure
                                            </asp:LinkButton>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                                <EmptyDataTemplate>
                                    <div style="text-align: center; padding: 40px; color: #94a3b8;">
                                        <i class="fas fa-calendar-times" style="font-size: 2rem; margin-bottom: 10px;"></i>
                                        <h5 style="margin: 0; font-weight: 700;">No events scheduled for this date.</h5>
                                        <span style="font-size: 0.85rem;">Click an open slot on the schedule above to create one.</span>
                                    </div>
                                </EmptyDataTemplate>
                            </asp:GridView>
                        </div>
                    </div>

                    <%-- SIDEBAR CONFIGURATION --%>
                    <div class="sidebar-sticky">
                        <asp:Panel ID="pnlSidebarWrapper" runat="server" CssClass="input-card">
                            <h3 style="color: #1e3a8a; font-weight: 800; margin-bottom: 20px;">
                                <asp:Literal ID="litSidebarHeader" runat="server" Text="Event Configuration" />
                            </h3>

                            <asp:Panel ID="pnlEmptyState" runat="server" style="text-align: center; padding: 30px 0; color: #94a3b8;">
                                <i class="fas fa-mouse-pointer" style="font-size: 2.5rem; color: #cbd5e1; margin-bottom: 15px;"></i>
                                <p style="margin: 0; font-weight: 600; font-size: 0.9rem;">Click an open slot on the schedule or select an event to view details.</p>
                            </asp:Panel>

                            <asp:Panel ID="pnlEventForm" runat="server" Visible="false">
                                <asp:HiddenField ID="hfIsEdit" runat="server" />
                                <asp:HiddenField ID="hfEventID" runat="server" />

                                <div class="input-group">
                                    <label>Event Title</label>
                                    <asp:TextBox ID="txtEventTitle" runat="server" placeholder="e.g. Summer Smash Tournament" />
                                </div>

                                <div class="input-group">
                                    <label>Sport Category</label>
                                    <asp:DropDownList ID="ddlSport" runat="server">
                                        <asp:ListItem Text="Badminton" Value="BADMINTON" />
                                        <asp:ListItem Text="Pickleball" Value="PICKLEBALL" />
                                    </asp:DropDownList>
                                </div>

                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-bottom: 15px;">
                                    <div class="input-group" style="margin-bottom: 0;">
                                        <label>Reg. Fee (₱)</label>
                                        <asp:TextBox ID="txtFee" runat="server" TextMode="Number" step="0.01" placeholder="100.00" />
                                    </div>
                                    <div class="input-group" style="margin-bottom: 0;">
                                        <label>Max Players</label>
                                        <asp:TextBox ID="txtMaxPlayers" runat="server" TextMode="Number" placeholder="12" onkeyup="updateRecommendations()" onchange="updateRecommendations()" />
                                    </div>
                                </div>

                                <div class="input-group">
                                    <label>Assigned Courts</label>
                                    <asp:CheckBoxList ID="cblCourts" runat="server" RepeatColumns="2" RepeatDirection="Horizontal" CssClass="event-checkbox-list">
                                        <asp:ListItem Value="1">Court 1</asp:ListItem>
                                        <asp:ListItem Value="2">Court 2</asp:ListItem>
                                        <asp:ListItem Value="3">Court 3</asp:ListItem>
                                        <asp:ListItem Value="4">Court 4</asp:ListItem>
                                        <asp:ListItem Value="5">Court 5</asp:ListItem>
                                        <asp:ListItem Value="6">Court 6</asp:ListItem>
                                    </asp:CheckBoxList>
                                </div>

                                <div id="recommendationBox" class="rec-box">
                                    Select courts to see capacity recommendations.
                                </div>

                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 15px;">
                                    <div class="input-group">
                                        <label>Start Time</label>
                                        <asp:DropDownList ID="ddlStartTime" runat="server" />
                                    </div>
                                    <div class="input-group">
                                        <label>End Time</label>
                                        <asp:DropDownList ID="ddlEndTime" runat="server" />
                                    </div>
                                </div>

                                <div style="margin-top: 15px;">
                                    <asp:Button ID="btnSaveEvent" runat="server" Text="Save Configuration" CssClass="save-btn" OnClick="btnSaveEvent_Click" />
                                    
                                    <%-- Targeted Cancel specific to the clicked court block --%>
                                    <asp:Button ID="btnRemoveSlot" runat="server" Text="Cancel This Court Session" CssClass="remove-slot-btn" OnClick="btnRemoveSlot_Click" OnClientClick="return confirm('Remove the court you just clicked from this event?');" Visible="false" />
                                    
                                    <asp:Button ID="btnCancelEvent" runat="server" Text="Delete Entire Event" CssClass="delete-btn" OnClick="btnCancelEvent_Click" OnClientClick="return confirm('Delete entire event and ALL matches?');" />
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlMatchSummary" runat="server" Visible="false" style="margin-top: 25px; padding-top: 20px; border-top: 2px solid #f1f5f9;">
                                <h4 style="font-size: 0.9rem; font-weight: 800; color: #1e3a8a; text-transform: uppercase; margin-bottom: 15px;">Live Match Summary</h4>
                                <asp:Repeater ID="rptSummary" runat="server">
                                    <ItemTemplate>
                                        <div class="event-summary-card">
                                            <div class="event-summary-court">Court <%# Eval("CourtNumber") %> &bull; Match <%# Eval("MatchOrder") %></div>
                                            <div class="event-summary-players"><%# Eval("P1Name") %> <span style="color:#94a3b8; font-weight:400; font-size:0.8rem;">vs</span> <%# Eval("P2Name").ToString() == "" ? "BYE" : Eval("P2Name") %></div>
                                            <div class="event-summary-winner"><span style="color:#64748b; font-size:0.75rem;">WINNER:</span> <%# Eval("WinnerName").ToString() == "" ? "Pending" : Eval("WinnerName") %></div>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                                <asp:Literal ID="litNoSummary" runat="server" Visible="false" Text="<div style='text-align:center; color:#94a3b8; font-size:0.85rem; font-weight:600; padding:10px;'>No matches completed yet.</div>"></asp:Literal>
                            </asp:Panel>
                        </asp:Panel>
                    </div>

                </div>

            </ContentTemplate>
        </asp:UpdatePanel>
    </div>

    <asp:HiddenField ID="hfActionType" runat="server" />
    <asp:HiddenField ID="hfActionCourt" runat="server" />
    <asp:HiddenField ID="hfActionTime" runat="server" />
    <asp:HiddenField ID="hfActionEventID" runat="server" />
    <asp:Button ID="btnHiddenTrigger" runat="server" OnClick="btnHiddenTrigger_Click" Style="display: none;" />

    <script>
        function triggerSidebar(type, cID, timeVal, eID) {
            if (type === 'BLOCKED') return;
            if (type === 'RES') {
                Swal.fire({ icon: 'info', title: 'Standard Reservation', text: 'This slot is reserved by a standard customer. Please manage it via the Reservations tab.', confirmButtonColor: '#1e3a8a' });
                return;
            }
            document.getElementById('<%= hfActionType.ClientID %>').value = type;
            document.getElementById('<%= hfActionCourt.ClientID %>').value = cID;
            document.getElementById('<%= hfActionTime.ClientID %>').value = timeVal;
            document.getElementById('<%= hfActionEventID.ClientID %>').value = eID;
            document.getElementById('<%= btnHiddenTrigger.ClientID %>').click();
        }

        function showSweetAlert(icon, title, text) {
            Swal.fire({ icon: icon, title: title, text: text, confirmButtonColor: '#1e3a8a' });
        }

        // Smart Calculator for Player Capacity
        function updateRecommendations() {
            var txtMax = document.getElementById('<%= txtMaxPlayers.ClientID %>');
            var cbl = document.getElementById('<%= cblCourts.ClientID %>');
            if(!txtMax || !cbl) return;

            var maxP = parseInt(txtMax.value) || 0;
            var checkboxes = cbl.querySelectorAll('input[type="checkbox"]:checked');
            var courtCount = checkboxes.length;
            var recDiv = document.getElementById('recommendationBox');

            if(courtCount > 0) {
                var recMax = courtCount * 12; // Maximum is 12 per court
                var recMin = courtCount * 4;  // Minimum is 4 per court
                
                recDiv.innerHTML = "<i class='fas fa-lightbulb'></i> Capacity for " + courtCount + " court(s): <b>" + recMin + " to " + recMax + " players</b>.";
                
                if(maxP > recMax) {
                    recDiv.innerHTML += "<br/><span style='color:#b91c1c; font-weight:bold;'><i class='fas fa-exclamation-triangle'></i> Warning: You are exceeding the max limit of 12 players per court.</span>";
                    recDiv.style.backgroundColor = "#fef2f2";
                    recDiv.style.borderColor = "#fecaca";
                    recDiv.style.color = "#991b1b";
                } else {
                    recDiv.style.backgroundColor = "#f0fdf4";
                    recDiv.style.borderColor = "#bbf7d0";
                    recDiv.style.color = "#166534";
                }
            } else {
                recDiv.innerHTML = "Select courts to see capacity recommendations.";
                recDiv.style.backgroundColor = "#f8fafc";
                recDiv.style.borderColor = "#e2e8f0";
                recDiv.style.color = "#64748b";
            }
        }

        // Ensure JS runs after UpdatePanel async postbacks
        function pageLoad() {
            var cbl = document.getElementById('<%= cblCourts.ClientID %>');
            if (cbl) {
                cbl.addEventListener('click', updateRecommendations);
            }
            updateRecommendations();
        }

        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function (sender, args) {
            if (args.get_error() != undefined) {
                showSweetAlert('error', 'System Error', args.get_error().message);
                args.set_errorHandled(true);
            }
        });
    </script>
</asp:Content>