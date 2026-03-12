<%@ Page Title="Customer Management" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_customer_account.aspx.cs" Inherits="Smash_IT.adminpage.admin_customer_account" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href='<%= ResolveUrl("~/css/admin-staff.css?v=" + DateTime.Now.Ticks) %>' rel="stylesheet" type="text/css" />
    <style>
        /* ===== UPGRADED POPUP MODAL STYLES ===== */
        .modal-overlay { position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(15, 23, 42, 0.8); display: none; z-index: 10001; align-items: center; justify-content: center; backdrop-filter: blur(4px); }
        .details-modal { background: #f8fafc; width: 550px; border-radius: 20px; overflow: hidden; box-shadow: 0 20px 40px rgba(0,0,0,0.4); animation: popIn 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275); display: flex; flex-direction: column; }
        
        .modal-header-smash { background: #ffffff; padding: 20px 25px; border-bottom: 1px solid #e2e8f0; display: flex; justify-content: space-between; align-items: center; }
        .modal-header-smash h3 { margin: 0; color: #1e3a8a; font-weight: 800; font-size: 1.4rem; display: flex; align-items: center; gap: 10px; }
        .modal-header-smash h3 i { color: #facc15; }
        .close-btn { background: #f1f5f9; color: #64748b; width: 32px; height: 32px; border-radius: 50%; display: flex; align-items: center; justify-content: center; cursor: pointer; font-weight: bold; transition: all 0.2s; }
        .close-btn:hover { background: #fee2e2; color: #ef4444; }

        .modal-body-smash { padding: 25px; }

        /* Financial Section */
        .financial-card { background: linear-gradient(135deg, #1e3a8a 0%, #162e70 100%); border-radius: 16px; padding: 25px; text-align: center; margin-bottom: 20px; box-shadow: 0 8px 20px rgba(30,58,138,0.25); position: relative; overflow: hidden; }
        .financial-card::after { content: '\f0d6'; font-family: 'Font Awesome 6 Free'; font-weight: 900; position: absolute; font-size: 8rem; color: rgba(255,255,255,0.03); right: -10px; bottom: -20px; }
        .fin-total .lbl { display: block; color: rgba(255,255,255,0.7); font-size: 0.8rem; text-transform: uppercase; font-weight: 700; letter-spacing: 1px; margin-bottom: 5px; }
        .fin-total .val { display: block; color: #facc15; font-size: 2.5rem; font-weight: 800; line-height: 1; }
        
        .fin-sub-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-top: 20px; border-top: 1px solid rgba(255,255,255,0.15); padding-top: 20px; }
        .fin-sub .lbl { display: block; color: rgba(255,255,255,0.6); font-size: 0.75rem; text-transform: uppercase; font-weight: 600; }
        .fin-sub .val { display: block; color: #ffffff; font-size: 1.3rem; font-weight: 700; }

        /* Activity Section */
        .section-title { font-size: 0.85rem; font-weight: 800; color: #475569; text-transform: uppercase; margin-bottom: 15px; display: block; letter-spacing: 0.5px; }
        .activity-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 15px; }
        .act-box { background: #ffffff; border: 1px solid #e2e8f0; border-radius: 12px; padding: 15px 10px; text-align: center; box-shadow: 0 2px 5px rgba(0,0,0,0.02); transition: transform 0.2s; }
        .act-box:hover { transform: translateY(-3px); border-color: #cbd5e1; box-shadow: 0 5px 15px rgba(0,0,0,0.05); }
        .act-box i { font-size: 1.4rem; color: #3b82f6; margin-bottom: 8px; display: block; }
        .act-box .val { font-size: 1.4rem; font-weight: 800; color: #0f172a; display: block; line-height: 1; margin-bottom: 4px; }
        .act-box .lbl { font-size: 0.7rem; color: #64748b; font-weight: 600; text-transform: uppercase; line-height: 1.2; }

        /* Specific icon colors */
        .ic-res { color: #8b5cf6 !important; }
        .ic-game { color: #f59e0b !important; }
        .ic-event { color: #ef4444 !important; }
        .ic-rent { color: #10b981 !important; }
        .ic-buy { color: #06b6d4 !important; }
        .ic-payc { color: #8b5cf6 !important; }

        .name-link { color: #1e3a8a; font-weight: bold; cursor: pointer; text-decoration: none; }
        .name-link:hover { color: #facc15; text-decoration: underline; }

        @keyframes popIn { from { opacity: 0; transform: scale(0.9) translateY(20px); } to { opacity: 1; transform: scale(1) translateY(0); } }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="staff-page-content">
        <div class="staff-container">
            
            <%-- Stats Overview Section --%>
            <div class="stats-container">
                <div class="stat-card">
                    <div class="stat-icon"><i class="fas fa-users"></i></div>
                    <div class="stat-info">
                        <span class="stat-label">Total Registered Players</span>
                        <asp:Label ID="lblTotalPlayers" runat="server" CssClass="stat-number">0</asp:Label>
                    </div>
                </div>
            </div>

            <%-- Header and Search Section --%>
            <div class="header-flex">
                <div class="search-box">
                    <asp:TextBox ID="txtSearch" runat="server" placeholder="Search name, email, or username..." CssClass="search-input"></asp:TextBox>
                    <asp:LinkButton ID="btnSearch" runat="server" OnClick="btnSearch_Click" CssClass="search-btn"><i class="fas fa-search"></i></asp:LinkButton>
                </div>
                <asp:Button ID="btnShowAdd" runat="server" Text="+ Add New Customer" OnClick="btnShowAdd_Click" CssClass="add-btn" />
            </div>

            <asp:Label ID="lblMsg" runat="server" CssClass="status-msg"></asp:Label>

            <%-- Input/CRUD Section (Panel) --%>
            <asp:Panel ID="pnlInput" runat="server" Visible="false" CssClass="input-card">
                <h3 style="margin-top: 0; margin-bottom: 20px; color: #1e3a8a;"><i class="fas fa-user-edit" style="color: #facc15; margin-right: 10px;"></i>Customer Details</h3>
                <asp:HiddenField ID="hfUserID" runat="server" />
                <div class="form-grid">
                    <div class="input-group"><label>First Name</label><asp:TextBox ID="txtFirstName" runat="server" placeholder="e.g. John" /></div>
                    <div class="input-group"><label>Last Name</label><asp:TextBox ID="txtLastName" runat="server" placeholder="e.g. Doe" /></div>
                    <div class="input-group"><label>Email Address</label><asp:TextBox ID="txtEmail" runat="server" TextMode="Email" /></div>
                    <div class="input-group"><label>Phone Number</label><asp:TextBox ID="txtPhone" runat="server" /></div>
                    <div class="input-group"><label>Username</label><asp:TextBox ID="txtUsername" runat="server" /></div>
                    <div class="input-group"><label>Password <small>(Edit to change)</small></label><asp:TextBox ID="txtPassword" runat="server" TextMode="Password" /></div>
                    <div class="input-group">
                        <label>Profile Image</label>
                        <asp:FileUpload ID="fileAvatar" runat="server" />
                        <asp:Image ID="imgPreview" runat="server" Visible="false" CssClass="preview-img" />
                    </div>
                </div>
                <div class="btn-group">
                    <asp:Button ID="btnSave" runat="server" Text="Save Customer" OnClick="btnSave_Click" CssClass="save-btn" />
                    <asp:Button ID="btnCancel" runat="server" Text="Cancel" OnClick="btnCancel_Click" CssClass="cancel-btn" />
                </div>
            </asp:Panel>

            <%-- Customer Data Grid --%>
            <div class="grid-card">
                <asp:GridView ID="gvCustomers" runat="server" AutoGenerateColumns="False" DataKeyNames="UserID" 
                    OnRowCommand="gvCustomers_RowCommand" CssClass="custom-grid" GridLines="None" AllowSorting="True" OnSorting="gvCustomers_Sorting">
                    <Columns>
                        <asp:BoundField DataField="UserID" HeaderText="ID" SortExpression="UserID" ItemStyle-CssClass="col-id" />
                        <asp:TemplateField HeaderText="Avatar">
                            <ItemTemplate>
                                <asp:Image ID="imgAvatar" runat="server" ImageUrl='<%# "~/" + Eval("ImgPath") %>' CssClass="avatar-img" style="width: 40px; height: 40px; border-radius: 50%; object-fit: cover; border: 2px solid #e2e8f0;"/>
                            </ItemTemplate>
                        </asp:TemplateField>
                        
                        <%-- CLICKABLE NAME COLUMN --%>
                        <asp:TemplateField HeaderText="Customer Name" SortExpression="FullName">
                            <ItemTemplate>
                                <a class="name-link" href="javascript:void(0);" onclick="showDetails('<%# Eval("UserID") %>', '<%# Eval("FullName") %>')">
                                    <%# Eval("FullName") %>
                                </a>
                            </ItemTemplate>
                        </asp:TemplateField>
                        
                        <asp:BoundField DataField="Email" HeaderText="Email" SortExpression="Email" />
                        <asp:BoundField DataField="PhoneNumber" HeaderText="Phone" SortExpression="PhoneNumber" />
                        <asp:BoundField DataField="Username" HeaderText="Username" SortExpression="Username" />
                        <asp:BoundField DataField="CreatedAt" HeaderText="Joined" DataFormatString="{0:MMM dd, yyyy}" SortExpression="CreatedAt" />
                        
                        <asp:TemplateField HeaderText="Actions" ItemStyle-Width="180px">
                            <ItemTemplate>
                                <asp:LinkButton runat="server" CommandName="EditCustomer" CommandArgument='<%# Eval("UserID") %>' CssClass="edit-link"><i class="fas fa-edit"></i> Edit</asp:LinkButton>
                                <asp:LinkButton runat="server" CommandName="DeleteCustomer" CommandArgument='<%# Eval("UserID") %>' CssClass="delete-link" OnClientClick="return confirm('Are you sure you want to delete this account?');"><i class="fas fa-trash"></i> Delete</asp:LinkButton>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </div>

    <%-- UPGRADED PLAYER DETAILS DASHBOARD POPUP --%>
    <div id="popupDetails" class="modal-overlay">
        <div class="details-modal">
            <div class="modal-header-smash">
                <h3><i class="fas fa-address-card"></i> <span id="popName">Player Name</span></h3>
                <div class="close-btn" onclick="closePopup()"><i class="fas fa-times"></i></div>
            </div>
            
            <div class="modal-body-smash">
                <div id="popLoading" style="text-align:center; padding: 40px 0; color: #64748b;">
                    <i class="fas fa-circle-notch fa-spin fa-2x" style="margin-bottom: 15px; color: #1e3a8a;"></i><br />
                    <b>Fetching Player Dashboard...</b>
                </div>

                <div id="popContent" style="display:none;">
                    
                    <%-- FINANCIALS --%>
                    <div class="financial-card">
                        <div class="fin-total">
                            <span class="lbl">Total Spent (All Time)</span>
                            <span class="val" id="statTotalPaid">₱0.00</span>
                        </div>
                        <div class="fin-sub-grid">
                            <div class="fin-sub">
                                <span class="lbl">Spent This Month</span>
                                <span class="val" id="statPaidMonth">₱0.00</span>
                            </div>
                            <div class="fin-sub">
                                <span class="lbl">Spent This Week</span>
                                <span class="val" id="statPaidWeek">₱0.00</span>
                            </div>
                        </div>
                    </div>

                    <%-- ACTIVITY GRID --%>
                    <span class="section-title">Player Activity & Engagements</span>
                    <div class="activity-grid">
                        <div class="act-box">
                            <i class="fas fa-calendar-check ic-res"></i>
                            <span class="val" id="statRes">0</span>
                            <span class="lbl">Reservations</span>
                        </div>
                        <div class="act-box">
                            <i class="fas fa-table-tennis-paddle-ball ic-game"></i>
                            <span class="val" id="statQueue">0</span>
                            <span class="lbl">Queue Games</span>
                        </div>
                        <div class="act-box">
                            <i class="fas fa-trophy ic-event"></i>
                            <span class="val" id="statEvents">0</span>
                            <span class="lbl">Events Joined</span>
                        </div>
                        <div class="act-box">
                            <i class="fas fa-box-open ic-rent"></i>
                            <span class="val" id="statRented">0</span>
                            <span class="lbl">Items Rented</span>
                        </div>
                        <div class="act-box">
                            <i class="fas fa-shopping-cart ic-buy"></i>
                            <span class="val" id="statBought">0</span>
                            <span class="lbl">Items Bought</span>
                        </div>
                        <div class="act-box">
                            <i class="fas fa-stopwatch ic-payc"></i>
                            <span class="val" id="statPayc">0</span>
                            <span class="lbl">PAYC Sessions</span>
                        </div>
                    </div>

                </div>
            </div>
        </div>
    </div>

    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script>
        function showDetails(id, name) {
            $("#popName").text(name);
            $("#popupDetails").css("display", "flex");
            $("#popLoading").show();
            $("#popContent").hide();

            var pageUrl = window.location.pathname.split('/').pop();
            if (pageUrl === "") pageUrl = "admin_customer_account.aspx";

            $.ajax({
                type: "POST",
                url: pageUrl + "/GetPlayerStats",
                data: JSON.stringify({ userId: id }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    var data = response.d;

                    // Format currencies safely
                    const formatCur = (num) => "₱" + num.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

                    $("#statTotalPaid").text(formatCur(data.TotalPaid));
                    $("#statPaidMonth").text(formatCur(data.PaidThisMonth));
                    $("#statPaidWeek").text(formatCur(data.PaidThisWeek));

                    // Format numbers
                    $("#statRented").text(data.ItemsRented.toLocaleString());
                    $("#statBought").text(data.ItemsBought.toLocaleString());
                    $("#statRes").text(data.Reservations.toLocaleString());
                    $("#statQueue").text(data.QueueGames.toLocaleString());
                    $("#statEvents").text(data.EventsJoined.toLocaleString());
                    $("#statPayc").text(data.PaycCount.toLocaleString());

                    $("#popLoading").hide();
                    $("#popContent").fadeIn(200);
                },
                error: function (xhr, status, error) {
                    console.error(xhr.responseText);
                    alert("Error retrieving data. Check console for details.");
                    closePopup();
                }
            });
        }

        function closePopup() {
            $("#popupDetails").fadeOut(150);
        }

        window.onclick = function (event) {
            var modal = document.getElementById('popupDetails');
            if (event.target == modal) {
                closePopup();
            }
        }
    </script>
</asp:Content>