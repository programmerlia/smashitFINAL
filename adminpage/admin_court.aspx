<%@ Page Title="Court Management" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_court.aspx.cs" Inherits="Smash_IT.adminpage.admin_court" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="https://fonts.googleapis.com/css2?family=Poppins:wght@400;600;700;800&display=swap" rel="stylesheet">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet" />
    <style>
        :root {
            --smash-blue: #1e3a8a;
            --electric-yellow: #facc15;
            --bg-gray: #f1f5f9;
            --white: #ffffff;
            --success: #22c55e;
            --danger: #e11d48;
        }

        .court-container { padding: 30px 40px; font-family: 'Poppins', sans-serif; color: #1e293b; }

        .admin-intro {
            background: var(--white);
            padding: 30px;
            border-radius: 12px;
            box-shadow: 0 4px 15px rgba(30, 58, 138, 0.05);
            margin-bottom: 30px;
            border-left: 6px solid var(--electric-yellow);
        }
        .admin-intro h1 { font-size: 1.8rem; font-weight: 800; color: var(--smash-blue); margin: 0; text-transform: uppercase; }
        .admin-intro p { color: #64748b; font-size: 0.95rem; margin-top: 8px; }

        .filter-container {
            background: var(--white); padding: 20px; border-radius: 12px; margin-bottom: 30px;
            display: flex; gap: 20px; align-items: flex-end; flex-wrap: wrap;
            box-shadow: 0 4px 15px rgba(30, 58, 138, 0.05);
        }
        .form-label { display: block; font-size: 0.75rem; font-weight: 700; color: var(--smash-blue); text-transform: uppercase; margin-bottom: 5px; }
        .form-control-inline { padding: 10px 15px; border: 1px solid #cbd5e1; border-radius: 8px; font-family: inherit; font-size: 0.9rem; }

        .btn { padding: 11px 20px; border-radius: 8px; font-weight: 700; cursor: pointer; border: none; text-transform: uppercase; font-size: 0.8rem; transition: 0.2s; text-decoration:none; display:inline-block; }
        .btn-primary { background: var(--smash-blue); color: white; }
        .btn-secondary { background: #e2e8f0; color: #475569; }
        .btn-success { background: var(--electric-yellow); color: var(--smash-blue); }
        .btn:hover { transform: translateY(-1px); opacity: 0.9; }

        .court-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 20px; margin-bottom: 40px; }
        .slot-cell {
            background: var(--white); padding: 20px; border-radius: 15px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.05); border-top: 5px solid #cbd5e1;
        }
        .status-available { border-top-color: var(--success); }
        .status-occupied { border-top-color: var(--smash-blue); }
        .status-closed { border-top-color: var(--danger); }

        .court-title { font-size: 1.1rem; font-weight: 800; color: var(--smash-blue); margin: 0 0 15px 0; display: flex; justify-content: space-between; align-items: center; }
        .player-info { background: #f8fafc; padding: 12px; border-radius: 10px; margin-bottom: 10px; border-left: 3px solid #e2e8f0; }
        .player-info strong { font-size: 0.7rem; color: #64748b; text-transform: uppercase; display: block; margin-bottom: 2px; }
        .player-info span { display: block; font-size: 0.9rem; font-weight: 700; color: var(--smash-blue); }
        .player-info small { color: #94a3b8; font-weight: 500; font-size: 0.8rem; }

        .section-subtitle { font-size: 1.2rem; font-weight: 700; color: var(--smash-blue); text-transform: uppercase; margin: 50px 0 25px; display: flex; align-items: center; gap: 10px; }

        .setup-container { display: grid; grid-template-columns: 1fr 1.5fr; gap: 30px; align-items: start; }
        @media (max-width: 1024px) { .setup-container { grid-template-columns: 1fr; } }

        .setup-form { background: var(--white); padding: 25px; border-radius: 15px; box-shadow: 0 10px 25px rgba(30, 58, 138, 0.08); }
        .form-group { margin-bottom: 15px; }
        .form-group label { display: block; font-size: 0.75rem; font-weight: 700; color: var(--smash-blue); text-transform: uppercase; margin-bottom: 5px; }
        .form-control { width: 100%; padding: 12px; border: 1px solid #e2e8f0; border-radius: 8px; font-family: inherit; box-sizing: border-box; }

        .schedule-grid-container { background: white; border-radius: 15px; padding: 20px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); }
        .styled-table { width: 100%; border-collapse: collapse; }
        .styled-table th { background: var(--smash-blue); color: white; padding: 15px; text-align: left; font-size: 0.8rem; text-transform: uppercase; }
        .styled-table td { padding: 15px; border-bottom: 1px solid #f1f5f9; font-size: 0.9rem; }

        .mode-badge { padding: 4px 10px; border-radius: 6px; font-weight: 700; font-size: 0.7rem; text-transform: uppercase; background: #eff6ff; color: var(--smash-blue); }
        .btn-danger-sm { background: #fee2e2; color: var(--danger); padding: 6px 12px; border-radius: 6px; border: none; cursor: pointer; transition: 0.2s; }
        .btn-danger-sm:hover { background: var(--danger); color: white; }

        .section-divider { border: 0; border-top: 2px solid #e2e8f0; margin: 50px 0; }

        .note-text { color:#64748b; font-size:0.9rem; margin-bottom:20px; }
        .source-pill {
            display:inline-block;
            margin-top:6px;
            padding:3px 8px;
            border-radius:999px;
            font-size:0.68rem;
            font-weight:700;
            text-transform:uppercase;
            background:#eef2ff;
            color:#3730a3;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>

    <div class="court-container">

        <div class="admin-intro">
            <h1><i class="fas fa-satellite-dish" style="color: var(--electric-yellow); margin-right: 10px;"></i>Live Court Monitor</h1>
            <p>Track current matches, apply closed overrides, and control court sessions in real-time.</p>
        </div>

        <asp:UpdatePanel ID="upDashboard" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                <asp:Timer ID="CourtRefreshTimer" runat="server" Interval="60000" OnTick="CourtRefreshTimer_Tick"></asp:Timer>

                <div class="filter-container">
                    <div>
                        <label class="form-label">View Date</label>
                        <asp:TextBox ID="txtDate" runat="server" TextMode="Date" CssClass="form-control-inline"></asp:TextBox>
                    </div>
                    <div>
                        <label class="form-label">View Time</label>
                        <asp:DropDownList ID="ddlTime" runat="server" CssClass="form-control-inline"></asp:DropDownList>
                    </div>
                    <div>
                        <asp:Button ID="btnApplyTime" runat="server" Text="Check Time" OnClick="btnApplyTime_Click" CssClass="btn btn-primary" />
                        <asp:Button ID="btnLiveView" runat="server" Text="Live Feed" OnClick="btnLiveView_Click" CssClass="btn btn-secondary" />
                 <asp:Button ID="btnLiveView2" runat="server" Text="Open Live Court" CssClass="btn btn-secondary"
    OnClientClick="window.open('../live_courts.aspx', '_blank'); return false;" />
                    </div>
                </div>

                <div class="court-grid">
                    <asp:Repeater ID="rptCourts" runat="server" OnItemCommand="rptCourts_ItemCommand">
                        <ItemTemplate>
                            <div class='<%# "slot-cell status-" + Eval("StatusCssClass") %>'>
                                <div class="court-title">
                                    <span>
                                        Court <%# Eval("CourtNumber") %>
                                        <small style="font-size: 0.7rem; color: #64748b; font-weight: normal;">
                                            (<%# Eval("CurrentMode") %>)
                                        </small>
                                    </span>
                                    <i class="fas fa-circle"
                                       style='<%# "font-size:10px; color:" + (Eval("StatusCssClass").ToString() == "occupied" ? "#1e3a8a" : (Eval("StatusCssClass").ToString() == "closed" ? "#e11d48" : "#22c55e")) %>'>
                                    </i>
                                </div>

                                <div class="player-info">
                                    <strong>Current State</strong>
                                    <span><%# Eval("CurrentPlayerName") %></span>
                                    <small><%# Eval("CurrentTimeRange") %></small>
                                    <div class="source-pill"><%# Eval("SourceLabel") %></div>
                                </div>

                                <div class="player-info" style="margin-bottom:15px; border-left-color: var(--electric-yellow);">
                                    <strong>Scheduled Next</strong>
                                    <span><%# Eval("NextPlayerName") %></span>
                                    <small><%# Eval("NextTimeRange") %></small>
                                </div>

                                <div style="display: flex; gap: 5px; justify-content: space-between; border-top: 1px solid #e2e8f0; padding-top: 15px;">
                                    <asp:LinkButton ID="btnStart" runat="server"
                                        CommandName="StartSession"
                                        CommandArgument='<%# Eval("CourtID") %>'
                                        CssClass="btn btn-success"
                                        Style="padding: 6px 10px; font-size: 0.7rem;"
                                        Visible='<%# Eval("StatusCssClass").ToString() != "occupied" && Eval("StatusCssClass").ToString() != "closed" %>'>
                                        <i class="fas fa-play"></i> Start
                                    </asp:LinkButton>

                                    <asp:LinkButton ID="btnEnd" runat="server"
                                        CommandName="EndSession"
                                        CommandArgument='<%# Eval("CourtID") %>'
                                        CssClass="btn btn-primary"
                                        Style="padding: 6px 10px; font-size: 0.7rem;"
                                        Visible='<%# Eval("StatusCssClass").ToString() == "occupied" %>'>
                                        <i class="fas fa-stop"></i> End Early
                                    </asp:LinkButton>

                                    <asp:LinkButton ID="btnMaintenance" runat="server"
                                        CommandName="SetMaintenance"
                                        CommandArgument='<%# Eval("CourtID") %>'
                                        CssClass="btn"
                                        Style="background: #fee2e2; color: #e11d48; padding: 6px 10px; font-size: 0.7rem;"
                                        OnClientClick="return confirm('Lock this court for 2 hours?');">
                                        <i class="fas fa-tools"></i> Lock Court
                                    </asp:LinkButton>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </ContentTemplate>
        </asp:UpdatePanel>

        <div class="section-divider"></div>
    </div>
</asp:Content>