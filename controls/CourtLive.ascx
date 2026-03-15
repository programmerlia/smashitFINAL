<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CourtLive.ascx.cs" Inherits="Smash_IT.controls.CourtLive" %>

<link href="https://fonts.googleapis.com/css2?family=Poppins:wght@400;600;700;800&display=swap" rel="stylesheet">
<link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet" />

<style>:root{--smash-blue:#1e3a8a;--smash-blue-dark:#172554;--electric-yellow:#facc15;--white:#ffffff;--success:#22c55e;--success-soft:#dcfce7;--danger:#e11d48;--danger-soft:#ffe4e6;--shadow:0 12px 30px rgba(30,58,138,.10);--court-green-1:#828fbe;--court-green-2:#5f71ac;--court-green-3:#7e96e4;--placeholder-gray:#d4d4d8;}
.court-page{padding:30px 40px;font-family:'Poppins',sans-serif;background:radial-gradient(circle at top left,#eff6ff 0%,#f8fafc 45%,#f8fafc 100%);color:#1e293b;}
.court-grid{display:grid;grid-template-columns:repeat(5,minmax(180px,1fr));grid-template-rows:repeat(12,44px);column-gap:4px;row-gap:8px;align-items:stretch;}
.court-slot,.blank-tile{min-width:0;min-height:0;}
.slot-1{grid-column:5;grid-row:1/span 7;}
.slot-2{grid-column:4;grid-row:1/span 7;}
.slot-3{grid-column:3;grid-row:1/span 7;}
.slot-4{grid-column:2;grid-row:1/span 7;}
.slot-5{grid-column:1;grid-row:5/span 8;}
.slot-6{grid-column:2/span 2;grid-row:8/span 5;}
.blank-a{grid-column:1;grid-row:1/span 3;background:transparent;border-radius:0;}
.blank-b{grid-column:5/span 1;grid-row:9/span 4;background:transparent;border-radius:12px;display:flex;align-items:flex-end;justify-content:flex-end;padding:18px;}


.live-panel-inner{display:flex;flex-direction:column;align-items:flex-end;justify-content:flex-end;gap:14px;width:100%;height:100%;}
.live-badge{display:inline-flex;align-items:center;gap:10px;padding:10px 16px;border-radius:999px;background:#fee2e2;color:#be123c;font-size:.82rem;font-weight:800;text-transform:uppercase;letter-spacing:.4px;}
.live-badge i{font-size:.7rem;animation:pulseDot 1.3s infinite;}
@keyframes pulseDot{0%{opacity:.35;transform:scale(.9);}50%{opacity:1;transform:scale(1.12);}100%{opacity:.35;transform:scale(.9);}}
.btn{padding:11px 18px;border-radius:12px;font-weight:800;cursor:pointer;border:none;text-transform:uppercase;font-size:.82rem;background:linear-gradient(135deg,var(--smash-blue) 0%,var(--smash-blue-dark) 100%);color:#fff;box-shadow:0 10px 20px rgba(30,58,138,.16);transition:transform .18s ease,box-shadow .18s ease;}
.btn:hover{transform:translateY(-1px);box-shadow:0 14px 24px rgba(30,58,138,.20);}
.court-card{position:relative;border-radius:15px;overflow:hidden;height:100%;width:100%;box-shadow:var(--shadow);border:1px solid #0f172a;display:flex;flex-direction:column;justify-content:space-between;background:linear-gradient(135deg,rgba(255,255,255,.06),rgba(255,255,255,.01)),radial-gradient(circle at 30% 20%,rgba(255,255,255,.09),transparent 35%),linear-gradient(135deg,var(--court-green-1),var(--court-green-2) 55%,var(--court-green-3));}
.court-card.horizontal{height:100%;}
.court-inner{position:relative;z-index:2;padding:18px;height:100%;display:flex;flex-direction:column;justify-content:space-between;backdrop-filter:saturate(1.05);}
.slot-6 .court-inner{padding:16px 18px;}
.court-topbar{display:flex;justify-content:space-between;align-items:flex-start;gap:10px;margin-bottom:14px;}
.court-label{color:#fff;text-shadow:0 2px 10px rgba(0,0,0,.28);}
.court-label .court-no{font-size:1.22rem;font-weight:800;line-height:1.1;text-transform:uppercase;letter-spacing:.5px;display:block;}
.status-pill{padding:8px 12px;border-radius:999px;font-size:.72rem;font-weight:800;text-transform:uppercase;letter-spacing:.45px;box-shadow:0 8px 18px rgba(0,0,0,.14);white-space:nowrap;}
.pill-available{background:var(--success-soft);color:#166534;}
.pill-occupied{background:#dbeafe;color:#1d4ed8;}
.pill-closed{background:var(--danger-soft);color:#be123c;}
.court-info-wrap{display:grid;gap:12px;}
.slot-6 .court-info-wrap{grid-template-columns:minmax(0,1fr) minmax(0,1fr);gap:12px;align-items:stretch;}
.info-box{background:rgba(255,255,255,.92);border-radius:16px;padding:14px 14px 13px;box-shadow:0 8px 20px rgba(15,23,42,.10);border:1px solid rgba(255,255,255,.72);backdrop-filter:blur(4px);}
.slot-6 .info-box{height:100%;}
.info-box.primary{border-left:5px solid var(--smash-blue);}
.info-box.secondary{border-left:5px solid var(--electric-yellow);}
.info-head{display:flex;align-items:center;gap:8px;margin-bottom:6px;font-size:.72rem;text-transform:uppercase;font-weight:800;color:#64748b;letter-spacing:.35px;}
.info-value{display:block;color:#0f172a;font-size:.96rem;font-weight:800;line-height:1.25;margin-bottom:4px;word-break:break-word;}
.info-sub{color:#64748b;font-size:.8rem;font-weight:600;line-height:1.45;}
.slot-6 .info-value{font-size:1rem;}
.slot-6 .info-sub{font-size:.82rem;}
@media (max-width:1220px){.court-grid{grid-template-columns:repeat(2,minmax(280px,1fr));grid-template-rows:auto;gap:18px;}.court-slot,.blank-tile{grid-column:auto!important;grid-row:auto!important;}.blank-a{display:none;}.blank-b{min-height:180px;}.court-card,.court-card.horizontal{min-height:300px;}.slot-6 .court-info-wrap{grid-template-columns:1fr;}}
@media (max-width:768px){.court-page{padding:20px 14px;}.court-grid{grid-template-columns:1fr;}.blank-b{min-height:160px;}.court-card,.court-card.horizontal{min-height:280px;}.court-topbar{flex-direction:column;align-items:flex-start;}.live-panel-inner{align-items:stretch;}}
</style>

<div class="court-page">
    <asp:UpdatePanel ID="upLive" runat="server" UpdateMode="Conditional">
        <ContentTemplate>
            <asp:Timer ID="tmrLive" runat="server" Interval="300000" OnTick="tmrLive_Tick"></asp:Timer>
<div class="court-grid">
    <div class="blank-tile blank-a"></div>

    <asp:Repeater ID="rptLiveCourts" runat="server">
        <ItemTemplate>
            <div class='<%# "court-slot slot-" + Eval("CourtNumber") %>'>
                <div class='<%# "court-card " + Eval("OrientationCssClass") %>'>
                    <div class="court-inner">
                        <div class="court-topbar">
                            <div class="court-label">
                                <span class="court-no">Court <%# Eval("CourtNumber") %></span>
                            </div>
                            <span class='<%# "status-pill pill-" + Eval("StatusCssClass") %>'>
                                <%# Eval("StatusDisplay") %>
                            </span>
                        </div>

                        <div class="court-info-wrap">
                            <div class="info-box primary">
                                <div class="info-head">
                                    <i class='<%#
                                        Eval("CurrentMode").ToString() == "Queue" ? "fas fa-people-line" :
                                        Eval("CurrentMode").ToString() == "Reservation" ? "fas fa-calendar-check" :
                                        Eval("CurrentMode").ToString() == "Closed" ? "fas fa-lock" :
                                        "fas fa-user-check"
                                    %>'></i>
                                    <span><%# Eval("CurrentLabel") %></span>
                                </div>
                                <span class="info-value"><%# Eval("CurrentPlayerName") %></span>
                                <span class="info-sub"><%# Eval("CurrentTimeRange") %></span>
                            </div>

                            <div class="info-box secondary">
                                <div class="info-head">
                                    <i class="fas fa-forward-step"></i>
                                    <span>Next Up</span>
                                </div>
                                <span class="info-value"><%# Eval("NextPlayerName") %></span>
                                <span class="info-sub"><%# Eval("NextTimeRange") %></span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </ItemTemplate>
    </asp:Repeater>

    <div class="blank-tile blank-b">
        <div class="live-panel-inner">
            <div class="live-badge">
                <i class="fas fa-circle"></i> Live Feed Active
            </div>
            <asp:Button ID="btnRefresh" runat="server" Text="Refresh Now" CssClass="btn" OnClick="btnRefresh_Click" />
        </div>
    </div>
</div>
        </ContentTemplate>
    </asp:UpdatePanel>
</div>