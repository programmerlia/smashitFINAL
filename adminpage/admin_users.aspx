<%@ Page Title="Staff Management" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_users.aspx.cs" Inherits="Smash_IT.adminpage.admin_staff" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <title>Staff Management | Smash-It</title>
    <link href='<%= ResolveUrl("~/css/admin-staff.css") %>' rel="stylesheet" type="text/css" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="staff-container">
        
        <%-- 1. Stats Overview Section (Mirrored from Customer) --%>
        <div class="stats-container">
            <div class="stat-card">
                <div class="stat-icon"><i class="fas fa-user-shield"></i></div>
                <div class="stat-info">
                    <span class="stat-label">Total Staff Members</span>
                    <asp:Label ID="lblTotalStaff" runat="server" CssClass="stat-number">0</asp:Label>
                </div>
            </div>
        </div>

        <%-- 2. Header and Search Section (Mirrored from Customer) --%>
        <div class="header-flex">
            <div class="search-box">
                <asp:TextBox ID="txtSearch" runat="server" placeholder="Search name, role, or username..." CssClass="search-input"></asp:TextBox>
                <asp:LinkButton ID="btnSearch" runat="server" OnClick="btnSearch_Click" CssClass="search-btn">
                    <i class="fas fa-search"></i>
                </asp:LinkButton>
            </div>
            <asp:Button ID="btnShowAdd" runat="server" Text="+ Add New Staff" OnClick="btnShowAdd_Click" CssClass="add-btn" />
        </div>

        <asp:Label ID="lblMsg" runat="server" CssClass="status-msg"></asp:Label>

        <%-- 3. Input/CRUD Section (Panel) --%>
        <asp:Panel ID="pnlInput" runat="server" Visible="false" CssClass="input-card">
            <asp:HiddenField ID="hfStaffID" runat="server" />
            <div class="form-grid">
                <div class="input-group">
                    <label>Full Name</label>
                    <asp:TextBox ID="txtFullName" runat="server" placeholder="Enter Full Name"></asp:TextBox>
                </div>
                <div class="input-group">
                    <label>Role</label>
                    <asp:DropDownList ID="ddlRole" runat="server">
                        <asp:ListItem Text="Admin" Value="Admin"></asp:ListItem>
                        <asp:ListItem Text="Receptionist" Value="Receptionist"></asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="input-group">
                    <label>Username</label>
                    <asp:TextBox ID="txtUsername" runat="server" placeholder="Username"></asp:TextBox>
                </div>
                <div class="input-group">
                    <label>Password</label>
                    <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" placeholder="••••••••"></asp:TextBox>
                </div>
            </div>
            <div class="btn-group">
                <asp:Button ID="btnSave" runat="server" Text="Save Staff" OnClick="btnSave_Click" CssClass="save-btn" />
                <asp:Button ID="btnCancel" runat="server" Text="Cancel" OnClick="btnCancel_Click" CssClass="cancel-btn" />
            </div>
        </asp:Panel>

        <%-- 4. Staff Data Grid (With Sorting Enabled) --%>
        <div class="grid-card">
            <asp:GridView ID="gvStaff" runat="server" AutoGenerateColumns="False" DataKeyNames="StaffID" 
                OnRowCommand="gvStaff_RowCommand" CssClass="custom-grid" GridLines="None"
                AllowSorting="True" OnSorting="gvStaff_Sorting">
                <Columns>
                    <asp:BoundField DataField="StaffID" HeaderText="ID" SortExpression="StaffID" />
                    <asp:BoundField DataField="FullName" HeaderText="Staff Name" SortExpression="FullName" />
                    <asp:BoundField DataField="StaffRole" HeaderText="Role" SortExpression="StaffRole" />
                    <asp:BoundField DataField="Username" HeaderText="Username" SortExpression="Username" />

                    <asp:TemplateField HeaderText="Actions">
                        <ItemTemplate>
                            <asp:LinkButton ID="btnEdit" runat="server" CommandName="EditStaff" CommandArgument='<%# Eval("StaffID") %>' CssClass="edit-link">
                                <i class="fas fa-edit"></i> Edit
                            </asp:LinkButton>
                            <asp:LinkButton ID="btnDelete" runat="server" CommandName="DeleteStaff" CommandArgument='<%# Eval("StaffID") %>' CssClass="delete-link" 
                                OnClientClick="return confirm('Are you sure you want to delete this staff account?');">
                                <i class="fas fa-trash"></i> Delete
                            </asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </div>
</asp:Content>