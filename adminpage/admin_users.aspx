<%@ Page Title="Staff Management" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_users.aspx.cs" Inherits="Smash_IT.adminpage.admin_users" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href='<%= ResolveUrl("~/css/admin-staff.css?v=" + DateTime.Now.Ticks) %>' rel="stylesheet" type="text/css" />
    <style>
        /* Popup Modal Styles */
        .modal-overlay { position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.7); display: none; z-index: 10001; align-items: center; justify-content: center; }
        .details-modal { background: #fff; width: 450px; border-radius: 15px; overflow: hidden; box-shadow: 0 15px 30px rgba(0,0,0,0.3); animation: fadeIn 0.3s; }
        .modal-header-smash { background: #1e3a8a; color: white; padding: 20px; display: flex; justify-content: space-between; align-items: center; }
        .modal-body-smash { padding: 20px; }
        .stat-item { display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #eee; font-family: 'Poppins', sans-serif; }
        .stat-item:last-child { border-bottom: none; }
        .stat-label { color: #64748b; font-weight: 600; }
        .stat-value { color: #1e3a8a; font-weight: 800; }
        .name-link { color: #1e3a8a; font-weight: bold; cursor: pointer; text-decoration: none; }
        .name-link:hover { color: #facc15; text-decoration: underline; }
        @keyframes fadeIn { from { opacity: 0; transform: scale(0.9); } to { opacity: 1; transform: scale(1); } }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="staff-page-content">
        <div class="staff-container">
            
            <div class="stats-container">
                <div class="stat-card">
                    <div class="stat-icon"><i class="fas fa-user-tie"></i></div>
                    <div class="stat-info">
                        <span class="stat-label">Total Registered Staff</span><br/>
                        <asp:Label ID="lblTotalStaff" runat="server" CssClass="stat-number">0</asp:Label>
                    </div>
                </div>
            </div>

            <div class="header-flex">
                <div class="search-box">
                    <asp:TextBox ID="txtSearch" runat="server" placeholder="Search name, email, or username..." CssClass="search-input"></asp:TextBox>
                    <asp:LinkButton ID="btnSearch" runat="server" OnClick="btnSearch_Click" CssClass="search-btn"><i class="fas fa-search"></i></asp:LinkButton>
                </div>
                <asp:Button ID="btnShowAdd" runat="server" Text="+ Add New Staff" OnClick="btnShowAdd_Click" CssClass="add-btn" />
            </div>

            <asp:Label ID="lblMsg" runat="server" CssClass="status-msg"></asp:Label>

            <asp:Panel ID="pnlInput" runat="server" Visible="false" CssClass="input-card">
                <h3 style="margin-top: 0; margin-bottom: 20px; color: #1e3a8a;"><i class="fas fa-user-edit" style="color: #facc15; margin-right: 10px;"></i>Staff Details</h3>
                <asp:HiddenField ID="hfStaffID" runat="server" />
                <div class="form-grid">
                    <div class="input-group"><label>First Name</label><asp:TextBox ID="txtFirstName" runat="server" placeholder="e.g. John" /></div>
                    <div class="input-group"><label>Last Name</label><asp:TextBox ID="txtLastName" runat="server" placeholder="e.g. Doe" /></div>
                    <div class="input-group"><label>Email Address</label><asp:TextBox ID="txtEmail" runat="server" TextMode="Email" /></div>
                    <div class="input-group">
                        <label>Staff Role</label>
                        <asp:DropDownList ID="ddlRole" runat="server" CssClass="form-control">
                            <asp:ListItem Value="receptionist" Text="Receptionist"></asp:ListItem>
                            <asp:ListItem Value="admin" Text="Admin"></asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="input-group"><label>Username</label><asp:TextBox ID="txtUsername" runat="server" /></div>
                    <div class="input-group"><label>Password <small>(Edit to change)</small></label><asp:TextBox ID="txtPassword" runat="server" TextMode="Password" /></div>
                    <div class="input-group">
                        <label>Profile Image</label>
                        <asp:FileUpload ID="fileAvatar" runat="server" />
                        <asp:Image ID="imgPreview" runat="server" Visible="false" CssClass="preview-img" style="max-width: 100px; margin-top: 10px; border-radius: 8px;" />
                    </div>
                </div>
                <div class="btn-group">
                    <asp:Button ID="btnSave" runat="server" Text="Save Staff" OnClick="btnSave_Click" CssClass="save-btn" />
                    <asp:Button ID="btnCancel" runat="server" Text="Cancel" OnClick="btnCancel_Click" CssClass="cancel-btn" />
                </div>
            </asp:Panel>

            <div class="grid-card">
                <asp:GridView ID="gvStaff" runat="server" AutoGenerateColumns="False" DataKeyNames="StaffID" 
                    OnRowCommand="gvStaff_RowCommand" CssClass="custom-grid" GridLines="None" AllowSorting="True" OnSorting="gvStaff_Sorting">
                    <Columns>
                        <asp:BoundField DataField="StaffID" HeaderText="ID" SortExpression="StaffID" ItemStyle-CssClass="col-id" />
                        <asp:TemplateField HeaderText="Avatar">
                            <ItemTemplate>
                                <asp:Image ID="imgAvatar" runat="server" ImageUrl='<%# "~/" + Eval("ImgPath") %>' CssClass="avatar-img" style="width: 40px; height: 40px; border-radius: 50%; object-fit: cover;" />
                            </ItemTemplate>
                        </asp:TemplateField>
                        
                        <asp:TemplateField HeaderText="Staff Name" SortExpression="FullName">
                            <ItemTemplate>
                                <a class="name-link" href="javascript:void(0);" onclick="showDetails('<%# Eval("StaffID") %>', '<%# Eval("FullName") %>')">
                                    <%# Eval("FullName") %>
                                </a>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:BoundField DataField="Email" HeaderText="Email" SortExpression="Email" />
                        <asp:TemplateField HeaderText="Role" SortExpression="StaffRole">
                            <ItemTemplate>
                                <span style="text-transform: capitalize;"><%# Eval("StaffRole") %></span>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="Username" HeaderText="Username" SortExpression="Username" />
                        <asp:TemplateField HeaderText="Actions" ItemStyle-Width="180px">
                            <ItemTemplate>
                                <asp:LinkButton runat="server" CommandName="EditStaff" CommandArgument='<%# Eval("StaffID") %>' CssClass="edit-link"><i class="fas fa-edit"></i> Edit</asp:LinkButton>
                                <asp:LinkButton runat="server" CommandName="DeleteStaff" CommandArgument='<%# Eval("StaffID") %>' CssClass="delete-link" OnClientClick="return confirm('Are you sure you want to delete this staff member?');"><i class="fas fa-trash"></i> Delete</asp:LinkButton>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </div>

    <%-- PLAYER DETAILS POPUP MODAL --%>
    <div id="popupDetails" class="modal-overlay">
        <div class="details-modal">
            <div class="modal-header-smash">
                <h3 id="popName" style="margin:0;">Name</h3>
                <span onclick="closePopup()" style="cursor:pointer; font-size: 24px;">&times;</span>
            </div>
            <div class="modal-body-smash">
                <div id="popLoading" style="text-align:center;">
                    <i class="fas fa-circle-notch fa-spin"></i> Fetching stats...
                </div>
                <div id="popContent" style="display:none;">
                    <div class="stat-item"><span class="stat-label">Total Payments</span><span id="statPayments" class="stat-value">0</span></div>
                    <div class="stat-item"><span class="stat-label">Reservations</span><span id="statRes" class="stat-value">0</span></div>
                    <div class="stat-item"><span class="stat-label">Games Played (Queue)</span><span id="statGames" class="stat-value">0</span></div>
                    <div class="stat-item"><span class="stat-label">Equipment Rentals</span><span id="statRentals" class="stat-value">0</span></div>
                    <div class="stat-item"><span class="stat-label">Play-All-You-Can</span><span id="statPayc" class="stat-value">0</span></div>
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
            if (pageUrl === "") pageUrl = "admin_users.aspx";

            $.ajax({
                type: "POST",
                url: pageUrl + "/GetPlayerStats",
                data: JSON.stringify({ userId: id }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    var data = response.d;
                    $("#statPayments").text("₱" + data.TotalPaid.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                    $("#statRes").text(data.Reservations);
                    $("#statGames").text(data.Games);
                    $("#statRentals").text(data.Rentals);
                    $("#statPayc").text(data.PayAllYouCan);

                    $("#popLoading").hide();
                    $("#popContent").show();
                },
                error: function (xhr, status, error) {
                    console.error(xhr.responseText);
                    alert("Error retrieving data. Check console for details.");
                    closePopup();
                }
            });
        }

        function closePopup() {
            $("#popupDetails").hide();
        }

        window.onclick = function (event) {
            var modal = document.getElementById('popupDetails');
            if (event.target == modal) {
                closePopup();
            }
        }
    </script>
</asp:Content>