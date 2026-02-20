<%@ Page Title="Customer Management" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_customer_account.aspx.cs" Inherits="Smash_IT.adminpage.admin_customer_account" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <title>Customer Management | Smash-It</title>
    <link href='<%= ResolveUrl("~/css/admin-staff.css") %>' rel="stylesheet" type="text/css" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="staff-container">
        
        <%-- 1. Stats Overview Section --%>
        <div class="stats-container">
            <div class="stat-card">
                <div class="stat-icon"><i class="fas fa-users"></i></div>
                <div class="stat-info">
                    <span class="stat-label">Total Registered Players</span>
                    <asp:Label ID="lblTotalPlayers" runat="server" CssClass="stat-number">0</asp:Label>
                </div>
            </div>
            <%-- Add more stat cards here in the future if needed --%>
        </div>

        <%-- 2. Header and Search Section --%>
        <div class="header-flex">
            <div class="search-box">
                <asp:TextBox ID="txtSearch" runat="server" placeholder="Search name, email, or username..." CssClass="search-input"></asp:TextBox>
                <asp:LinkButton ID="btnSearch" runat="server" OnClick="btnSearch_Click" CssClass="search-btn">
                    <i class="fas fa-search"></i>
                </asp:LinkButton>
            </div>
            <asp:Button ID="btnShowAdd" runat="server" Text="+ Add New Customer" OnClick="btnShowAdd_Click" CssClass="add-btn" />
        </div>

        <asp:Label ID="lblMsg" runat="server" CssClass="status-msg"></asp:Label>

        <%-- 3. Input/CRUD Section (Panel) --%>
        <asp:Panel ID="pnlInput" runat="server" Visible="false" CssClass="input-card">
            <asp:HiddenField ID="hfUserID" runat="server" />
            <div class="form-grid">
                <div class="input-group">
                    <label>Full Name</label>
                    <asp:TextBox ID="txtFullName" runat="server" placeholder="Enter Full Name"></asp:TextBox>
                </div>
                <div class="input-group">
                    <label>Email Address</label>
                    <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" placeholder="email@example.com"></asp:TextBox>
                </div>
                <div class="input-group">
                    <label>Phone Number</label>
                    <asp:TextBox ID="txtPhone" runat="server" placeholder="09xxxxxxxxx"></asp:TextBox>
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
                <asp:Button ID="btnSave" runat="server" Text="Save Customer" OnClick="btnSave_Click" CssClass="save-btn" />
                <asp:Button ID="btnCancel" runat="server" Text="Cancel" OnClick="btnCancel_Click" CssClass="cancel-btn" />
            </div>
        </asp:Panel>

        <%-- 4. Customer Data Grid --%>
        <div class="grid-card">
            <asp:GridView ID="gvCustomers" runat="server" AutoGenerateColumns="False" DataKeyNames="UserID" 
                OnRowCommand="gvCustomers_RowCommand" CssClass="custom-grid" GridLines="None"
                AllowSorting="True" OnSorting="gvCustomers_Sorting">
                <Columns>
                    <asp:BoundField DataField="UserID" HeaderText="ID" SortExpression="UserID" />
                    <asp:BoundField DataField="FullName" HeaderText="Customer Name" SortExpression="FullName" />
                    <asp:BoundField DataField="Email" HeaderText="Email" SortExpression="Email" />
                    <asp:BoundField DataField="PhoneNumber" HeaderText="Phone" SortExpression="PhoneNumber" />
                    <asp:BoundField DataField="Username" HeaderText="Username" SortExpression="Username" />
                    <asp:BoundField DataField="CreatedAt" HeaderText="Joined Date" DataFormatString="{0:MMM dd, yyyy}" SortExpression="CreatedAt" />


                    <asp:TemplateField HeaderText="Actions">
                        <ItemTemplate>
                            <asp:LinkButton ID="btnEdit" runat="server" CommandName="EditCustomer" CommandArgument='<%# Eval("UserID") %>' CssClass="edit-link">
                                <i class="fas fa-edit"></i> Edit
                            </asp:LinkButton>
                            <asp:LinkButton ID="btnDelete" runat="server" CommandName="DeleteCustomer" CommandArgument='<%# Eval("UserID") %>' CssClass="delete-link" 
                                OnClientClick="return confirm('Are you sure you want to delete this customer account?');">
                                <i class="fas fa-trash"></i> Delete
                            </asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </div>
</asp:Content>