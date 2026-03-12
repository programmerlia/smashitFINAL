<%@ Page Title="Match Command Center | Smash-IT" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_manage_queue.aspx.cs" Inherits="Smash_IT.adminpage.admin_manage_queue" MaintainScrollPositionOnPostback="true" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" />
    <link href="https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600;700;800&display=swap" rel="stylesheet">
    
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>

    <style>
        :root {
            --smash-blue: #1e3a8a; --smash-blue-light: #eff6ff; --electric-yellow: #facc15;
            --court-green: #10b981; --danger-red: #ef4444; --border-color: #e2e8f0; --bg-body: #f8fafc;
        }

        *, *::before, *::after { box-sizing: border-box; }
        body { font-family: 'Poppins', sans-serif; background-color: var(--bg-body); overflow-x: hidden; }
        .staff-container { padding: 30px; min-height: 100vh; }

        .header-flex { display: flex; justify-content: space-between; align-items: center; margin-bottom: 25px; background: #ffffff; padding: 20px 30px; border-radius: 16px; box-shadow: 0 4px 15px rgba(30, 58, 138, 0.05); border-left: 8px solid var(--electric-yellow); }
        .header-title h2 { color: var(--smash-blue); font-weight: 800; margin: 0; font-size: 1.6rem; text-transform: uppercase; }
        
        .event-selector { display: flex; background: var(--bg-body); border: 2px solid var(--border-color); border-radius: 12px; padding: 0 15px; height: 45px; align-items: center; width: 350px; }
        .event-dropdown { border: none !important; outline: none !important; font-size: 0.9rem; background: transparent; font-family: inherit; color: var(--smash-blue); font-weight: 700; width: 100%; }

        .mq-workspace-grid { display: grid; grid-template-columns: 420px minmax(0, 1fr); gap: 25px; align-items: start; }
        @media (max-width: 1250px) { .mq-workspace-grid { grid-template-columns: 1fr; } }

        .mq-card { background: #fff; border-radius: 16px; box-shadow: 0 4px 15px rgba(0,0,0,0.04); overflow: hidden; border: 1px solid var(--border-color); }
        .sidebar-sticky { position: sticky; top: 20px; }
        .mq-card-header { background: var(--smash-blue); color: #fff; padding: 15px 25px; display: flex; justify-content: space-between; align-items: center; }
        .mq-card-header h5 { margin: 0; font-weight: 700; text-transform: uppercase; font-size: 1rem; }

        .crown-bar { background: linear-gradient(135deg, #fef08a 0%, #facc15 100%); padding: 15px; text-align: center; border-bottom: 1px solid #eab308; }
        .crown-bar h4 { margin: 0; font-weight: 800; color: #854d0e; font-size: 1.1rem; }
        .crown-bar p { margin: 0; font-weight: 600; color: #a16207; font-size: 0.85rem; }

        .event-stats-bar { background: #f8fafc; padding: 15px; border-bottom: 1px solid var(--border-color); display: flex; justify-content: space-between; font-size: 0.75rem; font-weight: 700; color: #64748b; text-transform: uppercase; }
        .event-stats-bar span { display: flex; flex-direction: column; text-align: center; }
        .event-stats-bar strong { color: var(--smash-blue); font-size: 1.1rem; }

        .roster-actions { padding: 20px; border-bottom: 1px solid var(--border-color); background: #ffffff; }
        .mq-input { width: 100%; padding: 10px 15px; border: 2px solid var(--border-color); border-radius: 8px; font-family: inherit; font-size: 0.85rem; margin-bottom: 10px; transition: 0.2s; }
        
        .roster-grid-container { max-height: 500px; overflow-y: auto; }
        .roster-table { width: 100%; border-collapse: collapse; }
        .roster-table th { background: #f8fafc; padding: 12px 20px; font-size: 0.7rem; text-transform: uppercase; font-weight: 800; color: #64748b; position: sticky; top: 0; z-index: 5; box-shadow: 0 2px 5px rgba(0,0,0,0.05);}
        .roster-table td { padding: 12px 20px; border-bottom: 1px solid #f1f5f9; font-size: 0.85rem; font-weight: 600; vertical-align: middle; }

        .status-badge { padding: 4px 8px; border-radius: 6px; font-size: 0.65rem; font-weight: 800; text-transform: uppercase; }
        .status-paid { background: #dcfce7; color: #15803d; border: 1px solid #bbf7d0; }
        .status-pend { background: #fef9c3; color: #a16207; border: 1px solid #fde047; }

        .btn-circle { width: 30px; height: 30px; border-radius: 8px; display: inline-flex; align-items: center; justify-content: center; color: white; border: none; font-size: 0.8rem; margin-left: 4px; }
        .btn-pay { background: var(--court-green); }
        .btn-gear { background: var(--smash-blue); }
        .btn-del { background: #fee2e2; color: var(--danger-red); }

        .director-bar { background: #fff; padding: 15px 25px; border-radius: 16px; border: 1px solid var(--border-color); margin-bottom: 20px; display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 10px; }
        .btn-director { border: none; padding: 10px 15px; border-radius: 8px; font-weight: 800; text-transform: uppercase; font-size: 0.75rem; display: inline-flex; align-items: center; gap: 6px; }
        .btn-bracket { background: var(--electric-yellow); color: var(--smash-blue); }
        .btn-manual { background: var(--smash-blue); color: #fff; }
        .btn-danger-outline { background: #fff1f2; color: var(--danger-red); border: 1px solid #fecaca; }

        /* Court Display */
        .court-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 20px; margin-bottom: 30px; }
        .court-card { background: #fff; border-radius: 16px; border: 1px solid var(--border-color); overflow: hidden; display: flex; flex-direction: column; }
        .court-header { background: var(--smash-blue); color: #fff; padding: 12px 15px; font-weight: 800; display: flex; justify-content: space-between; align-items: center; font-size: 0.9rem; }
        .match-type-badge { background: rgba(255,255,255,0.2); padding: 3px 8px; border-radius: 6px; font-size: 0.65rem; text-transform: uppercase; }
        .phase-badge { background: #facc15; color: #854d0e; padding: 3px 8px; border-radius: 6px; font-size: 0.65rem; text-transform: uppercase; margin-right: 5px; }

        .match-area { padding: 20px; background: #ffffff; border-bottom: 1px solid var(--border-color); }
        .team-box { background: var(--bg-body); border: 1px solid var(--border-color); padding: 12px; border-radius: 10px; text-align: center; font-weight: 700; color: var(--smash-blue); width: 100%; margin-bottom: 10px;}
        .team-player { display: block; font-size: 0.85rem;}
        .team-player + .team-player { border-top: 1px dashed #cbd5e1; margin-top: 5px; padding-top: 5px; }
        .vs-divider { font-weight: 900; color: #ef4444; font-size: 0.75rem; background: #fee2e2; padding: 4px 10px; border-radius: 12px; margin: 0 auto 10px auto; display: table; }

        .up-next-section { padding: 15px; background: #f8fafc; flex-grow: 1;}
        .up-next-item { font-size: 0.75rem; font-weight: 600; padding: 8px; background: #ffffff; border: 1px solid var(--border-color); border-radius: 8px; margin-bottom: 5px; display: flex; justify-content: space-between; align-items: center;}

        /* Modals */
        .modal-custom .modal-content { border-radius: 20px; border: none; }
        .modal-head { background: var(--smash-blue); color: white; padding: 15px; text-align: center; border-radius: 20px 20px 0 0; }
        .kiosk-label { font-size: 0.7rem; font-weight: 800; color: #64748b; text-transform: uppercase; margin-bottom: 4px; display: block; }
        .kiosk-control { width: 100%; border: 2px solid var(--border-color); border-radius: 8px; padding: 8px; font-weight: 600; font-size: 0.85rem; margin-bottom: 15px; }
        .btn-full-action { background: var(--electric-yellow); color: var(--smash-blue); border: none; padding: 12px; border-radius: 10px; font-weight: 800; text-transform: uppercase; cursor: pointer; width: 100%; transition: 0.2s;}

        .rental-item { background: #eff6ff; border: 1px solid #bfdbfe; padding: 10px; border-radius: 8px; margin-bottom: 8px; display: flex; justify-content: space-between; align-items: center; font-size: 0.8rem; font-weight: 600; color: #1e3a8a;}
        .consumable-item { background: #f0fdf4; border: 1px solid #bbf7d0; padding: 10px; border-radius: 8px; margin-bottom: 8px; display: flex; justify-content: space-between; align-items: center; font-size: 0.8rem; font-weight: 600; color: #166534;}
        .btn-return { background: var(--court-green); color: #fff; border: none; padding: 6px 12px; border-radius: 6px; font-size: 0.75rem; font-weight: 700; text-decoration: none; cursor: pointer; transition: 0.2s;}
        .btn-return:hover { background: #15803d; color: #fff;}

        .history-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-top: 10px; }
        @media (max-width: 992px) { .history-grid { grid-template-columns: 1fr; } }
        
        .grid-phase-style { font-size: 0.7rem; color: #64748b; text-transform: uppercase; font-weight: 700; }
        .grid-wins-style { font-weight: 800; color: #1e3a8a; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="smQueue" runat="server" />
    
    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="upMain">
        <ProgressTemplate>
            <div style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(255,255,255,0.6); z-index: 9999; display: flex; justify-content: center; align-items: center;">
                <div style="background: var(--smash-blue); color: white; padding: 20px 40px; border-radius: 12px; font-weight: 800;">
                    <i class="fas fa-sync fa-spin me-3" style="color: var(--electric-yellow);"></i> Syncing Server...
                </div>
            </div>
        </ProgressTemplate>
    </asp:UpdateProgress>

    <div class="staff-container">
        
        <div class="header-flex">
            <div class="header-title">
                <h2>Match Command Center</h2>
                <p>Orchestrate brackets, manage doubles matches, and track equipment.</p>
            </div>
            <div>
                <label style="font-size: 0.7rem; font-weight: 800; color: #64748b; text-transform: uppercase; margin-bottom: 5px; display: block;">Switch Active Event</label>
                <div class="event-selector">
                    <i class="fas fa-medal me-3" style="color: var(--electric-yellow);"></i>
                    <asp:DropDownList ID="ddlActiveEvents" runat="server" CssClass="event-dropdown" AutoPostBack="true" OnSelectedIndexChanged="ddlActiveEvents_SelectedIndexChanged"></asp:DropDownList>
                </div>
            </div>
        </div>

        <asp:UpdatePanel ID="upMain" runat="server">
            <ContentTemplate>
                <asp:Panel ID="pnlEventWorkspace" runat="server" Visible="false">
                    <div class="mq-workspace-grid">
                        
                        <%-- SIDEBAR --%>
                        <div class="sidebar-sticky">
                            <div class="mq-card">
                                <asp:Panel ID="pnlCrown" runat="server" Visible="false" CssClass="crown-bar">
                                    <h4><i class="fas fa-crown"></i> <asp:Literal ID="litChampName" runat="server" /></h4>
                                    <p>Runner-Up: <asp:Literal ID="litRunnerUpName" runat="server" /></p>
                                </asp:Panel>

                                <div class="mq-card-header">
                                    <h5><i class="fas fa-users me-2"></i> Event Roster</h5>
                                </div>
                                
                                <div class="event-stats-bar">
                                    <span>Limit <strong><asp:Literal ID="litMaxPlayers" runat="server" Text="0" /></strong></span>
                                    <span>Total <strong><asp:Literal ID="litJoined" runat="server" Text="0" /></strong></span>
                                    <span>Paid <strong><asp:Literal ID="litPaid" runat="server" Text="0" /></strong></span>
                                    <span>Rev. <strong>₱<asp:Literal ID="litTotalRevenue" runat="server" Text="0" /></strong></span>
                                </div>

                                <div class="roster-actions">
                                    <label class="kiosk-label">Add System Member</label>
                                    <div class="d-flex gap-2 mb-3">
                                        <asp:DropDownList ID="ddlRegisteredUsers" runat="server" CssClass="mq-input" style="margin-bottom:0;" />
                                        <asp:Button ID="btnAddMember" runat="server" Text="Add" CssClass="btn-director btn-bracket" OnClick="btnAddMember_Click" />
                                    </div>
                                    <label class="kiosk-label">Quick Walk-in Queue</label>
                                    <asp:TextBox ID="txtMassPlayers" runat="server" TextMode="MultiLine" CssClass="mq-input" style="height: 50px;" placeholder="One full name per line..." />
                                    <asp:Button ID="btnAddMassPlayers" runat="server" Text="Bulk Queue" CssClass="btn-director btn-casual" style="width: 100%; justify-content: center;" OnClick="btnAddMassPlayers_Click" />
                                </div>
                                
                                <div class="roster-grid-container">
                                    <asp:GridView ID="gvParticipants" runat="server" AutoGenerateColumns="False" CssClass="roster-table" GridLines="None" DataKeyNames="EventParticipantID,WalkInID,UserID" OnRowCommand="gvParticipants_RowCommand" OnRowDataBound="gvParticipants_RowDataBound">
                                        <Columns>
                                            <asp:BoundField DataField="OrderNo" HeaderText="#" ItemStyle-Width="20px" />
                                            <asp:BoundField DataField="PlayerName" HeaderText="Player" />
                                            <asp:TemplateField HeaderText="Status" ItemStyle-Width="60px">
                                                <ItemTemplate><asp:Literal ID="litStatus" runat="server" /></ItemTemplate>
                                            </asp:TemplateField>
                                            <asp:TemplateField HeaderText="Actions" ItemStyle-CssClass="text-end" ItemStyle-Width="90px">
                                                <ItemTemplate>
                                                    <asp:LinkButton ID="btnPay" runat="server" CommandName="Pay" CommandArgument="<%# Container.DataItemIndex %>" CssClass="btn-circle btn-pay" ToolTip="Mark Paid" Visible='<%# Eval("StatusName").ToString() != "Completed" %>'><i class="fas fa-check"></i></asp:LinkButton>
                                                    <asp:LinkButton ID="btnRent" runat="server" CommandName="Rent" CommandArgument="<%# Container.DataItemIndex %>" CssClass="btn-circle btn-gear" ToolTip="Manage Gear"><i class="fas fa-shopping-cart"></i></asp:LinkButton>
                                                    <asp:LinkButton ID="btnRemove" runat="server" CommandName="RemovePlayer" CommandArgument="<%# Container.DataItemIndex %>" CssClass="btn-circle btn-del" OnClientClick="return confirm('Remove this player?');"><i class="fas fa-times"></i></asp:LinkButton>
                                                </ItemTemplate>
                                            </asp:TemplateField>
                                        </Columns>
                                    </asp:GridView>
                                </div>
                            </div>
                        </div>

                        <%-- MAIN CONTENT --%>
                        <div class="director-pane">
                            <div class="director-bar">
                                <div class="d-flex gap-2">
                                    <button type="button" class="btn-director btn-manual" onclick="showMatchModal()"><i class="fas fa-plus"></i> Match Builder</button>
                                    <button type="button" class="btn-director btn-bracket" onclick="showDraftModal()"><i class="fas fa-robot"></i> Auto-Draft</button>
                                </div>
                                <div class="d-flex gap-2">
                                    <button type="button" class="btn-director btn-bracket" style="background:#fef08a; color:#854d0e;" onclick="showChampModal()"><i class="fas fa-trophy"></i> Set Winners</button>
                                    <asp:Button ID="btnClearBoard" runat="server" Text="Reset Board" CssClass="btn-director btn-danger-outline" OnClientClick="return confirm('WARNING: This will delete ALL match history and reset the entire leaderboard for this event. Continue?');" OnClick="btnClearBoard_Click" />
                                </div>
                            </div>

                            <asp:Panel ID="pnlNoMatches" runat="server" Visible="false" style="background: #fff; border: 2px dashed #cbd5e1; border-radius: 16px; padding: 40px; text-align: center; margin-bottom: 20px;">
                                <h5 style="color: #94a3b8; font-weight: 800; margin:0;">No Active Court Sessions</h5>
                                <p style="color: #cbd5e1; font-size:0.85rem; margin-top:5px;">Open the Match Builder or Auto-Draft to begin.</p>
                            </asp:Panel>

                            <div class="court-grid">
                                <asp:Repeater ID="rptCourts" runat="server" OnItemDataBound="rptCourts_ItemDataBound">
                                    <ItemTemplate>
                                        <div class="court-card">
                                            <div class="court-header">
                                                <span>COURT <%# Eval("CourtNumber") %></span>
                                                <div>
                                                    <asp:Label ID="lblPhase" runat="server" CssClass="phase-badge"></asp:Label>
                                                    <asp:Label ID="lblMatchType" runat="server" CssClass="match-type-badge"></asp:Label>
                                                </div>
                                            </div>
                                            
                                            <%-- ACTIVE MATCH --%>
                                            <asp:Panel ID="pnlActiveMatch" runat="server" CssClass="match-area">
                                                <div class="team-box">
                                                    <span class="team-player"><asp:Literal ID="litT1P1" runat="server" /></span>
                                                    <asp:Panel ID="pnlT1P2" runat="server" Visible="false" CssClass="team-player"><asp:Literal ID="litT1P2" runat="server" /></asp:Panel>
                                                </div>
                                                <span class="vs-divider">VS</span>
                                                <div class="team-box">
                                                    <span class="team-player"><asp:Literal ID="litT2P1" runat="server" /></span>
                                                    <asp:Panel ID="pnlT2P2" runat="server" Visible="false" CssClass="team-player"><asp:Literal ID="litT2P2" runat="server" /></asp:Panel>
                                                </div>
                                                
                                                <asp:HiddenField ID="hfActiveMatchID" runat="server" />
                                                
                                                <%-- SCORE INPUTS --%>
                                                <div class="row w-100 mt-2 mb-2">
                                                    <div class="col-6 pe-1">
                                                        <asp:TextBox ID="txtScoreT1" runat="server" CssClass="kiosk-control text-center mb-0" placeholder="T1 Score" style="padding: 6px; font-size: 0.75rem;" />
                                                    </div>
                                                    <div class="col-6 ps-1">
                                                        <asp:TextBox ID="txtScoreT2" runat="server" CssClass="kiosk-control text-center mb-0" placeholder="T2 Score" style="padding: 6px; font-size: 0.75rem;" />
                                                    </div>
                                                </div>

                                                <asp:DropDownList ID="ddlActiveWinner" runat="server" CssClass="kiosk-control" style="margin:0;" AutoPostBack="true" OnSelectedIndexChanged="ddlActiveWinner_SelectedIndexChanged"></asp:DropDownList>
                                            </asp:Panel>
                                            
                                            <asp:Panel ID="pnlNoActive" runat="server" Visible="false" style="padding: 30px; text-align: center; background: #ffffff; border-bottom: 1px solid var(--border-color);">
                                                <span style="font-weight: 800; color: #cbd5e1; display:block; margin-bottom: 10px;">OPEN</span>
                                                <button type="button" class="btn btn-sm btn-outline-primary" style="font-weight:600;" onclick="openMatchModalForCourt('<%# Eval("CourtID") %>')"><i class="fas fa-plus"></i> Assign</button>
                                            </asp:Panel>

                                            <%-- UP NEXT QUEUE --%>
                                            <div class="up-next-section">
                                                <div class="up-next-title"><i class="fas fa-list-ol me-1"></i> Up Next Queue</div>
                                                <asp:Repeater ID="rptUpNext" runat="server">
                                                    <ItemTemplate>
                                                        <div class="up-next-item">
                                                            <span style="color:var(--smash-blue);"><%# Eval("Team1") %></span>
                                                            <span style="color:#cbd5e1; font-size:0.6rem; margin:0 5px;">vs</span>
                                                            <span style="color:var(--danger-red);"><%# Eval("Team2") %></span>
                                                        </div>
                                                    </ItemTemplate>
                                                </asp:Repeater>
                                                <asp:Literal ID="litNoQueue" runat="server" Visible="false" Text="<div style='color:#cbd5e1; font-size:0.75rem; font-style:italic;'>Queue is empty.</div>" />
                                            </div>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>

                            <%-- MATCH HISTORY & LEADERBOARD --%>
                            <div class="history-grid">
                                <div class="mq-card">
                                    <div class="mq-card-header"><h5>Leaderboard (Teams)</h5></div>
                                    <div class="roster-grid-container" style="max-height: 350px;">
                                        <asp:GridView ID="gvLeaderboard" runat="server" AutoGenerateColumns="False" CssClass="roster-table" GridLines="None">
                                            <Columns>
                                                <asp:BoundField DataField="PlayerName" HeaderText="Team / Player" />
                                                <asp:BoundField DataField="Wins" HeaderText="Wins" ItemStyle-CssClass="grid-wins-style" />
                                            </Columns>
                                        </asp:GridView>
                                    </div>
                                </div>
                                <div class="mq-card">
                                    <div class="mq-card-header"><h5>Match Records</h5></div>
                                    <div class="roster-grid-container" style="max-height: 350px;">
                                        <asp:GridView ID="gvMatchSummary" runat="server" AutoGenerateColumns="False" CssClass="roster-table" GridLines="None">
                                            <Columns>
                                                <asp:BoundField DataField="Phase" HeaderText="Phase" ItemStyle-CssClass="grid-phase-style" />
                                                <asp:BoundField DataField="MatchDesc" HeaderText="Versus" />
                                                <asp:BoundField DataField="Score" HeaderText="Score" />
                                                <asp:BoundField DataField="WinnerName" HeaderText="Winner" ItemStyle-ForeColor="#10b981" />
                                            </Columns>
                                        </asp:GridView>
                                    </div>
                                </div>
                            </div>
                            
                        </div>
                    </div>
                </asp:Panel>

                <%-- MODALS --%>

                <%-- 1. Gear Kiosk --%>
                <div class="modal fade modal-custom" id="equipmentModal" tabindex="-1" data-bs-backdrop="static">
                    <div class="modal-dialog modal-dialog-centered" style="max-width: 450px;">
                        <div class="modal-content">
                            <div class="modal-head"><h4>Gear Kiosk</h4></div>
                            <div class="modal-body" style="max-height: 70vh; overflow-y: auto;">
                                <asp:HiddenField ID="hfRentWalkInID" runat="server" />
                                <asp:HiddenField ID="hfRentUserID" runat="server" />
                                
                                <div style="background: var(--smash-blue-light); padding: 12px; border-radius: 10px; display: flex; align-items: center; gap: 12px; margin-bottom: 15px; border: 1px solid #bfdbfe;">
                                    <i class="fas fa-user-circle fs-2 text-primary"></i>
                                    <div><span class="kiosk-label">Account Profile</span><span class="fw-bold" style="color:var(--smash-blue);"><asp:Literal ID="litRentPlayerName" runat="server" /></span></div>
                                </div>

                                <%-- Active Rentals Section --%>
                                <asp:Panel ID="pnlActiveRentals" runat="server" Visible="false" style="margin-bottom: 20px;">
                                    <label class="kiosk-label text-warning"><i class="fas fa-stopwatch"></i> Currently Held Rentals</label>
                                    <asp:Repeater ID="rptActiveRentals" runat="server" OnItemCommand="rptActiveRentals_ItemCommand">
                                        <ItemTemplate>
                                            <div class="rental-item">
                                                <span><i class="fas fa-box-open text-primary me-2"></i><%# Eval("EquipmentType") %> (ID: <%# Eval("ItemID") %>)</span>
                                                <asp:LinkButton ID="btnReturn" runat="server" CommandName="ReturnItem" CommandArgument='<%# Eval("RentalID") %>' CssClass="btn-return" OnClientClick="return confirm('Return item and officially charge the account?');">
                                                    Return & Pay ₱<%# Convert.ToDecimal(Eval("UnitPrice")).ToString("0.00") %>
                                                </asp:LinkButton>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </asp:Panel>

                                <%-- Purchased Consumables Section --%>
                                <asp:Panel ID="pnlConsumables" runat="server" Visible="false" style="margin-bottom: 20px;">
                                    <label class="kiosk-label text-success"><i class="fas fa-check-circle"></i> Purchased Consumables (Digital Receipt)</label>
                                    <asp:Repeater ID="rptConsumables" runat="server">
                                        <ItemTemplate>
                                            <div class="consumable-item">
                                                <span><i class="fas fa-shopping-bag me-2"></i><%# Eval("Quantity") %>x <%# Eval("EquipmentType") %></span>
                                                <span class="fw-bold">Paid ₱<%# Convert.ToDecimal(Eval("Total")).ToString("0.00") %></span>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </asp:Panel>

                                <asp:PlaceHolder ID="phEquipmentInputs" runat="server">
                                    <hr style="border-top: 1px dashed var(--border-color); margin: 20px 0;" />
                                    <label class="kiosk-label">New Issue / Purchase</label>
                                    <asp:DropDownList ID="ddlEquipment" runat="server" CssClass="kiosk-control" />
                                    <label class="kiosk-label">Quantity</label>
                                    <asp:TextBox ID="txtRentQty" runat="server" TextMode="Number" Text="1" min="1" CssClass="kiosk-control" />
                                    <asp:Button ID="btnSaveEquipment" runat="server" Text="Process Transaction" CssClass="btn-full-action" OnClick="btnSaveEquipment_Click" OnClientClick="return confirmTransaction();" />
                                </asp:PlaceHolder>
                            </div>
                            <div class="p-3 text-center"><button type="button" class="btn btn-secondary btn-sm" onclick="hideEqModal()">Close</button></div>
                        </div>
                    </div>
                </div>

                <%-- 2. Match Builder (Manual / Tournament) --%>
                <div class="modal fade modal-custom" id="matchModal" tabindex="-1" data-bs-backdrop="static">
                    <div class="modal-dialog modal-dialog-centered" style="max-width: 500px;">
                        <div class="modal-content">
                            <div class="modal-head"><h4>Match Builder</h4></div>
                            <div class="modal-body">
                                <div class="row">
                                    <div class="col-6">
                                        <label class="kiosk-label">Assign Court</label>
                                        <asp:DropDownList ID="ddlManualCourt" runat="server" CssClass="kiosk-control"></asp:DropDownList>
                                    </div>
                                    <div class="col-6">
                                        <label class="kiosk-label">Bracket Phase</label>
                                        <asp:DropDownList ID="ddlManualPhase" runat="server" CssClass="kiosk-control">
                                            <asp:ListItem Value="Casual">Casual Match</asp:ListItem>
                                            <asp:ListItem Value="Winners Bracket">Winners Bracket</asp:ListItem>
                                            <asp:ListItem Value="Losers Bracket">Losers Bracket</asp:ListItem>
                                            <asp:ListItem Value="Finals">Finals</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>
                                
                                <label class="kiosk-label">Match Format</label>
                                <asp:DropDownList ID="ddlManualType" runat="server" CssClass="kiosk-control" onchange="toggleDoubles(this.value)">
                                    <asp:ListItem Value="Singles">Singles (1 vs 1)</asp:ListItem>
                                    <asp:ListItem Value="Doubles">Doubles (2 vs 2)</asp:ListItem>
                                </asp:DropDownList>

                                <div style="background: var(--bg-body); padding: 15px; border-radius: 12px; margin-bottom: 15px; border: 1px solid var(--border-color);">
                                    <label class="kiosk-label text-primary">Team 1</label>
                                    <asp:DropDownList ID="ddlT1P1" runat="server" CssClass="kiosk-control" style="margin-bottom:10px;"></asp:DropDownList>
                                    <div id="divT1P2" style="display:none;"><asp:DropDownList ID="ddlT1P2" runat="server" CssClass="kiosk-control" style="margin-bottom:0;"></asp:DropDownList></div>
                                </div>

                                <div style="background: #fff1f2; padding: 15px; border-radius: 12px; border: 1px solid #fecaca;">
                                    <label class="kiosk-label text-danger">Team 2</label>
                                    <asp:DropDownList ID="ddlT2P1" runat="server" CssClass="kiosk-control" style="margin-bottom:10px;"></asp:DropDownList>
                                    <div id="divT2P2" style="display:none;"><asp:DropDownList ID="ddlT2P2" runat="server" CssClass="kiosk-control" style="margin-bottom:0;"></asp:DropDownList></div>
                                </div>

                                <asp:Button ID="btnSaveManualMatch" runat="server" Text="Push to Queue" CssClass="btn-full-action" OnClick="btnSaveManualMatch_Click" />
                            </div>
                            <div class="p-3 text-center"><button type="button" class="btn btn-secondary btn-sm" onclick="hideMatchModal()">Cancel</button></div>
                        </div>
                    </div>
                </div>

                <%-- 3. Auto Draft Generator (Casual ONLY) --%>
                <div class="modal fade modal-custom" id="draftModal" tabindex="-1" data-bs-backdrop="static">
                    <div class="modal-dialog modal-dialog-centered" style="max-width: 400px;">
                        <div class="modal-content">
                            <div class="modal-head"><h4 style="background:var(--electric-yellow); color:var(--smash-blue); padding:10px; border-radius:10px;"><i class="fas fa-robot"></i> Auto-Draft</h4></div>
                            <div class="modal-body text-center">
                                <p style="font-size:0.85rem; color:#64748b; margin-bottom:20px;">Automatically distribute all available PAID players into casual matches across empty courts.</p>
                                
                                <label class="kiosk-label text-start">Draft Type</label>
                                <asp:DropDownList ID="ddlDraftMode" runat="server" CssClass="kiosk-control" Enabled="false">
                                    <asp:ListItem Value="Casual">Casual Mix</asp:ListItem>
                                </asp:DropDownList>

                                <label class="kiosk-label text-start">Format</label>
                                <asp:DropDownList ID="ddlDraftFormat" runat="server" CssClass="kiosk-control">
                                    <asp:ListItem Value="Singles">Singles</asp:ListItem>
                                    <asp:ListItem Value="Doubles">Doubles</asp:ListItem>
                                </asp:DropDownList>

                                <asp:Button ID="btnExecuteDraft" runat="server" Text="Generate Matchups" CssClass="btn-full-action" OnClick="btnExecuteDraft_Click" />
                            </div>
                            <div class="p-3 text-center"><button type="button" class="btn btn-secondary btn-sm" onclick="hideDraftModal()">Cancel</button></div>
                        </div>
                    </div>
                </div>

                <%-- 4. Set Champions --%>
                <div class="modal fade modal-custom" id="champModal" tabindex="-1" data-bs-backdrop="static">
                    <div class="modal-dialog modal-dialog-centered" style="max-width: 400px;">
                        <div class="modal-content">
                            <div class="modal-head" style="background:#854d0e;"><h4><i class="fas fa-trophy"></i> Event Winners</h4></div>
                            <div class="modal-body">
                                <label class="kiosk-label text-warning">Event Champion (1st Place)</label>
                                <asp:DropDownList ID="ddlChampion" runat="server" CssClass="kiosk-control" style="border-color:#facc15;"></asp:DropDownList>
                                
                                <label class="kiosk-label text-secondary">Runner Up (2nd Place)</label>
                                <asp:DropDownList ID="ddlRunnerUp" runat="server" CssClass="kiosk-control"></asp:DropDownList>

                                <asp:Button ID="btnSaveWinners" runat="server" Text="Publish Results" CssClass="btn-full-action" OnClick="btnSaveWinners_Click" />
                            </div>
                            <div class="p-3 text-center"><button type="button" class="btn btn-secondary btn-sm" onclick="hideChampModal()">Close</button></div>
                        </div>
                    </div>
                </div>

            </ContentTemplate>
        </asp:UpdatePanel>
    </div>

    <script>
        // Extreme Modal Cleanup to fix Scroll Bug permanently
        function forceCleanupModal() {
            setTimeout(function() {
                document.body.classList.remove('modal-open');
                document.body.style.overflow = 'auto';
                document.body.style.paddingRight = '';
                document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
            }, 150);
        }

        function showEqModal() { new bootstrap.Modal(document.getElementById('equipmentModal')).show(); }
        function hideEqModal() { bootstrap.Modal.getInstance(document.getElementById('equipmentModal'))?.hide(); setTimeout(forceCleanupModal, 200); }

        function showMatchModal() { 
            document.getElementById('<%= ddlManualType.ClientID %>').value = "Singles";
            toggleDoubles("Singles");
            new bootstrap.Modal(document.getElementById('matchModal')).show(); 
        }
        function hideMatchModal() { bootstrap.Modal.getInstance(document.getElementById('matchModal'))?.hide(); setTimeout(forceCleanupModal, 200); }
        function openMatchModalForCourt(courtId) { document.getElementById('<%= ddlManualCourt.ClientID %>').value = courtId; showMatchModal(); }

        function showDraftModal() { new bootstrap.Modal(document.getElementById('draftModal')).show(); }
        function hideDraftModal() { bootstrap.Modal.getInstance(document.getElementById('draftModal'))?.hide(); setTimeout(forceCleanupModal, 200); }

        function showChampModal() { new bootstrap.Modal(document.getElementById('champModal')).show(); }
        function hideChampModal() { bootstrap.Modal.getInstance(document.getElementById('champModal'))?.hide(); setTimeout(forceCleanupModal, 200); }

        function toggleDoubles(val) {
            var disp = val === 'Doubles' ? 'block' : 'none';
            document.getElementById('divT1P2').style.display = disp;
            document.getElementById('divT2P2').style.display = disp;
            if(val !== 'Doubles') {
                document.getElementById('<%= ddlT1P2.ClientID %>').value = "";
                document.getElementById('<%= ddlT2P2.ClientID %>').value = "";
            }
        }

        // Equipment JS Validation
        function confirmTransaction() {
            var qty = document.getElementById('<%= txtRentQty.ClientID %>').value;
            if (!qty || qty <= 0) {
                Swal.fire('Invalid Quantity', 'Please enter a quantity greater than 0.', 'warning');
                return false;
            }
            return confirm('Confirm processing this transaction for this account?');
        }

        // Seamless Scroll Position Retention
        var prm = Sys.WebForms.PageRequestManager.getInstance();
        var rosterScrollPos = 0;

        prm.add_beginRequest(function () {
            var grid = document.querySelector('.roster-grid-container');
            if (grid) rosterScrollPos = grid.scrollTop;
        });

        prm.add_endRequest(function () {
            var grid = document.querySelector('.roster-grid-container');
            if (grid) grid.scrollTop = rosterScrollPos;
            forceCleanupModal(); // Strip locks if postback happens while modal is active
        });
    </script>
</asp:Content>