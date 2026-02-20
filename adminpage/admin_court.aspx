<%@ Page Title="Court Management" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_court.aspx.cs" Inherits="Smash_IT.adminpage.admin_court" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" />

    <link rel="stylesheet" href="../css/court-layout.css" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="sm1" runat="server" />
    <div class="staff-container">
        <div class="court-admin-header">
            <h2>Court Operations</h2>
            <div class="legend">
                <div class="legend-item"><span class="dot available"></span> Available</div>
                <div class="legend-item"><span class="dot playing"></span> Playing</div>
                <div class="legend-item"><span class="dot closed"></span> Closed</div>
                <div class="legend-item"><span class="dot pickle"></span> Pickleball</div>
            </div>
        </div>

        <asp:UpdatePanel ID="upCourts" runat="server">
            <ContentTemplate>
                <%-- Page updates every 30 minutes --%>
                <asp:Timer ID="Timer1" runat="server" Interval="1800000" OnTick="TimerRefresh_Tick"></asp:Timer>
                
                <div class="court-map">
                    <asp:Repeater ID="rptCourts" runat="server" OnItemCommand="rptCourts_ItemCommand">
                        <ItemTemplate>
                            <%-- Dynamic Card Classes --%>
                            <div class='<%# "court-card " + Eval("CourtStatus").ToString().ToLower() + 
                                         (Eval("Sports").ToString() == "Pickleball" ? " pickleball" : "") +
                                         (Eval("CourtID").ToString() == "5" || Eval("CourtID").ToString() == "6" ? " horizontal" : "") %>'>
                                
                                <%-- Court Header --%>
                                <div class="court-info">
                                    <span class="court-number">COURT <%# Eval("CourtID") %></span>
                                    <span class="court-type"><%# Eval("Sports") %></span>
                                </div>

                                <%-- Central Player Display --%>
                                <div class="occupant-display">
                                    <div class="now-playing">
                                        <span class="player-name">
                                            <i class="fas fa-user-circle"></i> 
                                            <%# Eval("OccupantName") %>
                                        </span>
                                        
                                        <%-- Clean Time Formatting: Only shows if reservation exists --%>
                                        <asp:PlaceHolder runat="server" Visible='<%# Eval("DisplayStart") != DBNull.Value %>'>
                                            <span class="current-time">
                                                (<%# Eval("DisplayStart") %> - <%# Eval("DisplayEnd") %>)
                                            </span>
                                        </asp:PlaceHolder>
                                    </div>

                                    <%-- Up Next Section: Future Reservations Only --%>
                                    <asp:PlaceHolder runat="server" Visible='<%# Eval("NextPlayerName") != DBNull.Value %>'>
                                        <div class="next-up">
                                            <span class="next-label">Up Next</span>
                                            <span class="next-player"><%# Eval("NextPlayerName") %></span>
                                            <small class="next-time">(<%# Eval("NextPlayerTime") %>)</small>
                                        </div>
                                    </asp:PlaceHolder>
                                </div>

                                <%-- Action Buttons --%>
                                <div class="court-actions">
                                    <asp:LinkButton ID="btnStatus" runat="server" 
                                        CommandName="ToggleStatus" 
                                        CommandArgument='<%# Eval("CourtID") %>' 
                                        CssClass="btn-action" 
                                        ToolTip="Toggle Status">
                                        <i class="fas fa-power-off"></i>
                                    </asp:LinkButton>
                                    
                                    <asp:LinkButton ID="btnSwap" runat="server" 
                                        CommandName="SwitchSport" 
                                        CommandArgument='<%# Eval("CourtID") %>' 
                                        CssClass="btn-action" 
                                        Visible='<%# Eval("Mode").ToString() == "Switchable" %>' 
                                        ToolTip="Switch Sport">
                                        <i class="fas fa-random"></i>
                                    </asp:LinkButton>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </ContentTemplate>
        </asp:UpdatePanel>
    </div>
</asp:Content>