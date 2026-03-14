<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CourtLive.ascx.cs" Inherits="Smash_IT.controls.CourtLive" %>

<link href="https://fonts.googleapis.com/css2?family=Poppins:wght@400;600;700;800&display=swap" rel="stylesheet">
<link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet" />

<style>
    :root {
        --smash-blue: #1e3a8a;
        --smash-blue-dark: #172554;
        --electric-yellow: #facc15;
        --bg-gray: #f8fafc;
        --white: #ffffff;
        --success: #22c55e;
        --success-soft: #dcfce7;
        --danger: #e11d48;
        --danger-soft: #ffe4e6;
        --slate: #64748b;
        --line: rgba(255,255,255,0.95);
        --shadow: 0 12px 30px rgba(30, 58, 138, 0.10);
        --court-green-1: #4d7c0f;
        --court-green-2: #65a30d;
        --court-green-3: #3f6212;
    }

    .court-page {
        padding: 30px 40px;
        font-family: 'Poppins', sans-serif;
        background:
            radial-gradient(circle at top left, #eff6ff 0%, #f8fafc 45%, #f8fafc 100%);
        color: #1e293b;
    }

    .top-strip {
        background: rgba(255,255,255,0.96);
        border-radius: 20px;
        padding: 18px 22px;
        box-shadow: var(--shadow);
        display: flex;
        justify-content: space-between;
        align-items: center;
        flex-wrap: wrap;
        gap: 12px;
        margin-bottom: 26px;
        border: 1px solid #e2e8f0;
    }

    .live-badge {
        display: inline-flex;
        align-items: center;
        gap: 10px;
        padding: 10px 16px;
        border-radius: 999px;
        background: #fee2e2;
        color: #be123c;
        font-size: 0.82rem;
        font-weight: 800;
        text-transform: uppercase;
        letter-spacing: 0.4px;
    }

    .live-badge i {
        font-size: 0.7rem;
        animation: pulseDot 1.3s infinite;
    }

    @keyframes pulseDot {
        0% { opacity: 0.35; transform: scale(0.9); }
        50% { opacity: 1; transform: scale(1.12); }
        100% { opacity: 0.35; transform: scale(0.9); }
    }

    .btn {
        padding: 11px 18px;
        border-radius: 12px;
        font-weight: 800;
        cursor: pointer;
        border: none;
        text-transform: uppercase;
        font-size: 0.82rem;
        background: linear-gradient(135deg, var(--smash-blue) 0%, var(--smash-blue-dark) 100%);
        color: white;
        box-shadow: 0 10px 20px rgba(30, 58, 138, 0.16);
        transition: transform 0.18s ease, box-shadow 0.18s ease;
    }

    .btn:hover {
        transform: translateY(-1px);
        box-shadow: 0 14px 24px rgba(30, 58, 138, 0.20);
    }

    .court-grid {
        display: grid;
        grid-template-columns: 240px repeat(4, minmax(220px, 1fr));
        grid-template-areas:
            "court6 court4 court3 court2 court1"
            "court5 .      .      .      .";
        gap: 24px;
        align-items: stretch;
    }

    .court-slot {
        min-width: 0;
    }

    .slot-court-1 { grid-area: court1; }
    .slot-court-2 { grid-area: court2; }
    .slot-court-3 { grid-area: court3; }
    .slot-court-4 { grid-area: court4; }
    .slot-court-5 { grid-area: court5; }
    .slot-court-6 { grid-area: court6; }

    .court-card {
        position: relative;
        border-radius: 24px;
        overflow: hidden;
        min-height: 370px;
        box-shadow: var(--shadow);
        border: 4px solid #0f172a;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        background:
            linear-gradient(135deg, rgba(255,255,255,0.06), rgba(255,255,255,0.01)),
            radial-gradient(circle at 30% 20%, rgba(255,255,255,0.09), transparent 35%),
            linear-gradient(135deg, var(--court-green-1), var(--court-green-2) 55%, var(--court-green-3));
    }

    .court-card::before {
        content: "";
        position: absolute;
        inset: 16px;
        border: 3px solid var(--line);
        border-radius: 2px;
        pointer-events: none;
    }

    .court-card::after {
        content: "";
        position: absolute;
        left: 50%;
        top: 16px;
        transform: translateX(-50%);
        width: 3px;
        height: calc(100% - 32px);
        background: var(--line);
        opacity: 0.96;
        pointer-events: none;
    }

    .court-card.horizontal {
        min-height: 205px;
    }

    .court-card.horizontal::after {
        top: 50%;
        left: 16px;
        width: calc(100% - 32px);
        height: 3px;
        transform: translateY(-50%);
    }

    .court-inner {
        position: relative;
        z-index: 2;
        padding: 18px;
        height: 100%;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        backdrop-filter: saturate(1.05);
    }

    .court-lines-vertical,
    .court-lines-horizontal {
        position: absolute;
        inset: 16px;
        pointer-events: none;
        z-index: 1;
    }

    .court-lines-vertical .short-line {
        position: absolute;
        width: 26%;
        left: 37%;
        border: 3px solid var(--line);
        border-top: none;
        border-bottom: none;
    }

    .court-lines-vertical .short-line.top { top: 16px; height: 30%; }
    .court-lines-vertical .short-line.bottom { bottom: 16px; height: 30%; }

    .court-lines-vertical .service-left,
    .court-lines-vertical .service-right {
        position: absolute;
        top: 34%;
        width: 3px;
        height: 28%;
        background: var(--line);
    }

    .court-lines-vertical .service-left { left: 24%; }
    .court-lines-vertical .service-right { right: 24%; }

    .court-lines-vertical .mid-top,
    .court-lines-vertical .mid-bottom {
        position: absolute;
        left: 16px;
        right: 16px;
        height: 3px;
        background: var(--line);
    }

    .court-lines-vertical .mid-top { top: 34%; }
    .court-lines-vertical .mid-bottom { bottom: 34%; }

    .court-lines-horizontal .short-line {
        position: absolute;
        height: 26%;
        top: 37%;
        border: 3px solid var(--line);
        border-left: none;
        border-right: none;
    }

    .court-lines-horizontal .short-line.left { left: 16px; width: 30%; }
    .court-lines-horizontal .short-line.right { right: 16px; width: 30%; }

    .court-lines-horizontal .service-top,
    .court-lines-horizontal .service-bottom {
        position: absolute;
        left: 34%;
        width: 28%;
        height: 3px;
        background: var(--line);
    }

    .court-lines-horizontal .service-top { top: 24%; }
    .court-lines-horizontal .service-bottom { bottom: 24%; }

    .court-lines-horizontal .mid-left,
    .court-lines-horizontal .mid-right {
        position: absolute;
        top: 16px;
        bottom: 16px;
        width: 3px;
        background: var(--line);
    }

    .court-lines-horizontal .mid-left { left: 34%; }
    .court-lines-horizontal .mid-right { right: 34%; }

    .court-topbar {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 10px;
        margin-bottom: 14px;
    }

    .court-label {
        color: white;
        text-shadow: 0 2px 10px rgba(0,0,0,0.28);
    }

    .court-label .court-no {
        font-size: 1.22rem;
        font-weight: 800;
        line-height: 1.1;
        text-transform: uppercase;
        letter-spacing: 0.5px;
        display: block;
    }

    .court-label .court-mode {
        display: inline-block;
        margin-top: 6px;
        font-size: 0.72rem;
        font-weight: 800;
        text-transform: uppercase;
        letter-spacing: 0.5px;
        padding: 6px 10px;
        border-radius: 999px;
        background: rgba(15, 23, 42, 0.42);
        color: #f8fafc;
        backdrop-filter: blur(6px);
    }

    .status-pill {
        padding: 8px 12px;
        border-radius: 999px;
        font-size: 0.72rem;
        font-weight: 800;
        text-transform: uppercase;
        letter-spacing: 0.45px;
        box-shadow: 0 8px 18px rgba(0,0,0,0.14);
        white-space: nowrap;
    }

    .pill-available {
        background: var(--success-soft);
        color: #166534;
    }

    .pill-occupied {
        background: #dbeafe;
        color: #1d4ed8;
    }

    .pill-closed {
        background: var(--danger-soft);
        color: #be123c;
    }

    .court-info-wrap {
        display: grid;
        gap: 12px;
    }

    .info-box {
        background: rgba(255,255,255,0.92);
        border-radius: 16px;
        padding: 14px 14px 13px;
        box-shadow: 0 8px 20px rgba(15, 23, 42, 0.10);
        border: 1px solid rgba(255,255,255,0.72);
        backdrop-filter: blur(4px);
    }

    .info-box.primary {
        border-left: 5px solid var(--smash-blue);
    }

    .info-box.secondary {
        border-left: 5px solid var(--electric-yellow);
    }

    .info-head {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-bottom: 6px;
        font-size: 0.72rem;
        text-transform: uppercase;
        font-weight: 800;
        color: #64748b;
        letter-spacing: 0.35px;
    }

    .info-value {
        display: block;
        color: #0f172a;
        font-size: 0.96rem;
        font-weight: 800;
        line-height: 1.25;
        margin-bottom: 4px;
        word-break: break-word;
    }

    .info-sub {
        color: #64748b;
        font-size: 0.8rem;
        font-weight: 600;
    }

    .court-footer {
        margin-top: 14px;
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 10px;
        color: rgba(255,255,255,0.95);
        font-size: 0.75rem;
        font-weight: 700;
        text-shadow: 0 1px 8px rgba(0,0,0,0.25);
    }

    .court-footer .mini-tag {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        padding: 7px 10px;
        border-radius: 999px;
        background: rgba(15, 23, 42, 0.35);
        backdrop-filter: blur(4px);
    }

    @media (max-width: 1220px) {
        .court-grid {
            grid-template-columns: repeat(2, minmax(280px, 1fr));
            grid-template-areas: none;
        }

        .court-slot {
            grid-area: auto !important;
        }

        .court-card.horizontal {
            min-height: 370px;
        }

        .court-card.horizontal::after {
            left: 50%;
            top: 16px;
            transform: translateX(-50%);
            width: 3px;
            height: calc(100% - 32px);
        }

        .court-lines-horizontal {
            display: none;
        }

        .court-lines-vertical.mobile-show {
            display: block;
        }
    }

    @media (max-width: 768px) {
        .court-page {
            padding: 20px 14px;
        }

        .court-grid {
            grid-template-columns: 1fr;
        }

        .court-card {
            min-height: 345px;
        }

        .top-strip {
            padding: 16px;
        }

        .court-topbar {
            flex-direction: column;
            align-items: flex-start;
        }
    }
</style>

<div class="court-page">
    <asp:UpdatePanel ID="upLive" runat="server" UpdateMode="Conditional">
        <ContentTemplate>
            <asp:Timer ID="tmrLive" runat="server" Interval="60000" OnTick="tmrLive_Tick"></asp:Timer>

            <div class="top-strip">
                <div class="live-badge">
                    <i class="fas fa-circle"></i> Live Feed Active
                </div>

                <div>
                    <asp:Button ID="btnRefresh" runat="server" Text="Refresh Now" CssClass="btn" OnClick="btnRefresh_Click" />
                </div>
            </div>

            <div class="court-grid">
                <asp:Repeater ID="rptLiveCourts" runat="server">
                    <ItemTemplate>
                        <div class='<%# "court-slot " + Eval("LayoutCssClass") %>'>
                            <div class='<%# "court-card " + Eval("OrientationCssClass") %>'>

                                <asp:Panel runat="server" Visible='<%# Eval("OrientationCssClass").ToString() != "horizontal" %>'>
                                    <div class="court-lines-vertical">
                                        <div class="short-line top"></div>
                                        <div class="short-line bottom"></div>
                                        <div class="service-left"></div>
                                        <div class="service-right"></div>
                                        <div class="mid-top"></div>
                                        <div class="mid-bottom"></div>
                                    </div>
                                </asp:Panel>

                                <asp:Panel runat="server" Visible='<%# Eval("OrientationCssClass").ToString() == "horizontal" %>'>
                                    <div class="court-lines-horizontal">
                                        <div class="short-line left"></div>
                                        <div class="short-line right"></div>
                                        <div class="service-top"></div>
                                        <div class="service-bottom"></div>
                                        <div class="mid-left"></div>
                                        <div class="mid-right"></div>
                                    </div>
                                </asp:Panel>

                                <div class="court-inner">
                                    <div class="court-topbar">
                                        <div class="court-label">
                                            <span class="court-no">Court <%# Eval("CourtNumber") %></span>
                                            <span class="court-mode"><%# Eval("CurrentMode") %></span>
                                        </div>

                                        <span class='<%# "status-pill pill-" + Eval("StatusCssClass") %>'>
                                            <%# Eval("StatusDisplay") %>
                                        </span>
                                    </div>

                                    <div class="court-info-wrap">
                                        <div class="info-box primary">
                                            <div class="info-head">
                                                <i class='<%# Eval("CurrentMode").ToString() == "Queue" ? "fas fa-flag-checkered" : "fas fa-user-check" %>'></i>
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

                                    <div class="court-footer">
                                        <span class="mini-tag">
                                            <i class="fas fa-location-dot"></i>
                                            Physical Court Layout
                                        </span>

                                        <span class="mini-tag">
                                            <i class="fas fa-bolt"></i>
                                            Live
                                        </span>
                                    </div>
                                </div>

                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>
</div>