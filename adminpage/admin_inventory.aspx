<%@ Page Title="Inventory Management" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_inventory.aspx.cs" Inherits="Smash_IT.adminpage.inventory" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <title>Inventory | Smash-IT Admin</title>
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet" />
    <link href="https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600;700;800&display=swap" rel="stylesheet">
    <style>
        :root {
            --smash-blue: #1e3a8a;
            --electric-yellow: #facc15;
            --text-dark: #1e293b;
            --text-muted: #64748b;
            --border-color: #e2e8f0;
        }

        body {
            background-color: #f4f7fe;
            font-family: 'Poppins', sans-serif;
            color: var(--text-dark);
        }

        .inventory-wrapper {
            padding: 30px 40px;
            max-width: 100%;
            margin: 0 auto;
        }

        /* ===== HEADER ===== */
        .admin-intro {
            background: #ffffff;
            padding: 30px;
            border-radius: 12px;
            box-shadow: 0 4px 15px rgba(30, 58, 138, 0.05);
            margin-bottom: 30px;
            border-left: 6px solid var(--electric-yellow);
            display: flex;
            justify-content: space-between;
            align-items: center;
            flex-wrap: wrap;
            gap: 20px;
        }

            .admin-intro h1 {
                color: var(--smash-blue);
                font-weight: 800;
                margin: 0;
                font-size: 1.8rem;
                text-transform: uppercase;
            }

        .search-box {
            display: flex;
            background: #f8fafc;
            border: 1px solid #cbd5e1;
            border-radius: 8px;
            padding: 0 15px;
            height: 45px;
            align-items: center;
            width: 300px;
        }

        .search-input {
            border: none !important;
            outline: none !important;
            width: 100%;
            font-size: 0.9rem;
            background: transparent;
            font-family: inherit;
            color: var(--smash-blue);
            font-weight: 600;
        }

        /* ===== FILTER BAR ===== */
        .filter-bar {
            display: flex;
            gap: 10px;
            margin-bottom: 20px;
            align-items: center;
        }

        .btn-filter {
            padding: 8px 18px;
            border-radius: 8px;
            font-weight: 700;
            cursor: pointer;
            border: 1px solid #cbd5e1;
            background: #fff;
            color: #64748b;
            font-size: 0.75rem;
            text-transform: uppercase;
            transition: 0.2s;
        }

            .btn-filter.active {
                background: var(--smash-blue);
                color: white;
                border-color: var(--smash-blue);
            }

        /* ===== LAYOUT ===== */
        .inventory-layout {
            display: grid;
            grid-template-columns: 1.4fr 1fr;
            gap: 25px;
        }

        @media (max-width: 1200px) {
            .inventory-layout {
                grid-template-columns: 1fr;
            }
        }

        .card {
            background: #fff;
            border-radius: 12px;
            box-shadow: 0 4px 15px rgba(30, 58, 138, 0.05);
            overflow: hidden;
            border: 1px solid var(--border-color);
        }

        .card-header {
            background: var(--smash-blue);
            color: #fff;
            padding: 15px 20px;
            font-weight: 700;
            text-transform: uppercase;
            font-size: 0.9rem;
            letter-spacing: 1px;
        }

        /* ===== FORMS ===== */
        .input-panel {
            padding: 25px;
            background: #fff;
            border-bottom: 2px solid #f1f5f9;
            border-top: 5px solid var(--smash-blue);
        }

        .form-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
            gap: 20px;
            margin-bottom: 20px;
        }

        .input-field {
            width: 100%;
            padding: 12px;
            border: 1px solid #e2e8f0;
            border-radius: 8px;
            font-family: inherit;
            font-size: 0.9rem;
            box-sizing: border-box;
        }

            .input-field:focus {
                border-color: var(--electric-yellow);
                outline: none;
                background-color: #fefce8;
            }

        .btn {
            padding: 12px 24px;
            border-radius: 8px;
            font-weight: 700;
            cursor: pointer;
            border: none;
            text-transform: uppercase;
            font-size: 0.8rem;
            transition: 0.2s;
        }

        .btn-add {
            background-color: var(--electric-yellow);
            color: var(--smash-blue);
        }

        .btn-save {
            background: var(--smash-blue);
            color: white;
        }

        .btn-cancel {
            background: #f1f5f9;
            color: #475569;
        }

        /* ===== TABLES ===== */
        /* ===== TABLES ===== */
    .custom-grid {
    width: max-content;
    min-width: 100%;
    border-collapse: collapse;
}

            .custom-grid th {
                background: #f8fafc;
                padding: 15px;
                text-align: left;
                font-size: 0.7rem;
                color: #64748b;
                text-transform: uppercase;
                border-bottom: 2px solid var(--border-color);
            }

         .custom-grid td {
    padding: 15px;
    border-bottom: 1px solid #f1f5f9;
    font-size: 0.9rem;
    vertical-align: middle;
    white-space: nowrap;
}

        .pill {
            padding: 5px 12px;
            border-radius: 6px;
            font-size: 0.7rem;
            font-weight: 800;
            text-transform: uppercase;
            display: inline-block;
        }

        .pill-avail {
            background: #dcfce7;
            color: #166534;
        }

        .pill-rented {
            background: #fee2e2;
            color: #991b1b;
        }

        .action-link {
            color: var(--smash-blue);
            background: #eff6ff;
            padding: 6px 10px;
            border-radius: 6px;
            text-decoration: none;
            font-size: 0.9rem;
        }

            .action-link.delete {
                color: #e11d48;
                background: #fff1f2;
            }

        .card-scroll {
            width: 100%;
            overflow-x: auto;
        }

        .custom-grid .action-link,
        .custom-grid a,
        .custom-grid .btn {
            white-space: normal;
        }

        .custom-grid th:last-child,
        .custom-grid td:last-child {
            white-space: nowrap;
            width: 90px;
        }

        .status-msg {
            display: block;
            padding: 15px;
            margin-bottom: 20px;
            border-radius: 8px;
            font-weight: 600;
        }

        .msg-success {
            background: #f0fdf4;
            color: #166534;
            border-left: 5px solid #22c55e;
        }

        .msg-error {
            background: #fef2f2;
            color: #991b1b;
            border-left: 5px solid #e11d48;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="inventory-wrapper">

        <div class="admin-intro">
            <div>
                <h1>Inventory</h1>
                <p>Maintain rental equipment and consumables stock levels.</p>
            </div>
            <div style="display: flex; gap: 15px;">
                <div class="search-box">
                    <asp:TextBox ID="txtSearchCatalog" runat="server" placeholder="Search..." CssClass="search-input" AutoPostBack="true" OnTextChanged="txtSearchCatalog_TextChanged" />
                    <i class="fas fa-search" style="color: var(--smash-blue);"></i>
                </div>
                <asp:Button ID="btnShowAddModel" runat="server" Text="+ New Item" OnClick="btnShowAddModel_Click" CssClass="btn btn-add" />
            </div>
        </div>

        <div class="filter-bar">
            <asp:Button ID="btnFilterAll" runat="server" Text="All Gear" OnClick="FilterItems_Click" CommandArgument="All" CssClass="btn-filter active" />
            <asp:Button ID="btnFilterRental" runat="server" Text="Rentals" OnClick="FilterItems_Click" CommandArgument="Rental" CssClass="btn-filter" />
            <asp:Button ID="btnFilterConsumable" runat="server" Text="Consumables" OnClick="FilterItems_Click" CommandArgument="Consumable" CssClass="btn-filter" />
        </div>

        <asp:Label ID="lblMsg" runat="server" Visible="false"></asp:Label>

        <div class="inventory-layout">

            <%-- CATALOG CARD --%>
            <div class="card">
                <div class="card-header">Gear Catalog</div>

                <asp:Panel ID="pnlModelInput" runat="server" Visible="false" CssClass="input-panel">
                    <asp:HiddenField ID="hfSelectedModelID" runat="server" />
                    <div class="form-grid">
                        <div class="form-group">
                            Type
                            <asp:DropDownList ID="ddlItemCategory" runat="server" CssClass="input-field" AutoPostBack="true" OnSelectedIndexChanged="ddlItemCategory_SelectedIndexChanged">
                                <asp:ListItem Value="Rental" Text="Rental Item"></asp:ListItem>
                                <asp:ListItem Value="Consumable" Text="Consumable (Sale)"></asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="form-group">
                            Name
                            <asp:TextBox ID="txtEquipType" runat="server" placeholder="e.g. Racket" CssClass="input-field" />
                        </div>
                        <div class="form-group">
                            <label>Specs</label>
                            <asp:TextBox ID="txtEquipSpec" runat="server" placeholder="e.g. Pro Model" CssClass="input-field" />
                        </div>
                    </div>

                    <div class="form-grid">
                        <asp:Panel ID="pnlRentalFields" runat="server">
                            <label>Rental Rate (P)</label>
                            <asp:TextBox ID="txtRentalPrice" runat="server" CssClass="input-field" placeholder="0.00" />
                        </asp:Panel>
                        <asp:Panel ID="pnlConsumableFields" runat="server" Visible="false">
                            <label>Sale Price (P)</label>
                            <asp:TextBox ID="txtSellPrice" runat="server" CssClass="input-field" placeholder="0.00" />
                        </asp:Panel>
                        <asp:Panel ID="pnlConsumableQty" runat="server" Visible="false">
                            <label>Stock Qty</label>
                            <asp:TextBox ID="txtConsumableQty" runat="server" CssClass="input-field" placeholder="0" />
                        </asp:Panel>
                    </div>

                    <div style="display: flex; gap: 10px;">
                        <asp:Button ID="btnSaveModel" runat="server" Text="Save" OnClick="btnSaveModel_Click" CssClass="btn btn-save" />
                        <asp:Button ID="btnCancelModel" runat="server" Text="Cancel" OnClick="btnCancelModel_Click" CssClass="btn btn-cancel" />
                    </div>
                </asp:Panel>

                <div class="card-scroll">
                    <asp:GridView ID="gvModels" runat="server" AutoGenerateColumns="False" DataKeyNames="ModelID" OnRowCommand="gvModels_RowCommand" CssClass="custom-grid" GridLines="None">
                        <Columns>
                            <asp:BoundField DataField="ItemCategory" HeaderText="Type" ItemStyle-Font-Bold="true" />
                            <asp:TemplateField HeaderText="Description">
                                <ItemTemplate>
                                    <div style="font-weight: 700; color: var(--smash-blue);"><%# Eval("EquipmentType") %></div>
                                    <small style="color: var(--text-muted);"><%# Eval("EquipmentSpec") %></small>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Price">
                                <ItemTemplate>
                                    <%# Eval("ItemCategory").ToString() == "Rental"
        ? "₱ " + Convert.ToDecimal(Eval("DefaultRentalPrice")).ToString("0.00")
        : "₱ " + Convert.ToDecimal(Eval("DefaultSellPrice")).ToString("0.00") %>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="TotalStock" HeaderText="Stock" ItemStyle-Font-Bold="true" />
                            <asp:TemplateField ItemStyle-HorizontalAlign="Right">
                                <ItemTemplate>
                                    <asp:LinkButton runat="server" CommandName="EditModel" CommandArgument='<%# Eval("ModelID") %>' CssClass="action-link"><i class="fas fa-edit"></i></asp:LinkButton>
                                    <asp:LinkButton runat="server" CommandName="DeleteModel" CommandArgument='<%# Eval("ModelID") %>' CssClass="action-link delete" OnClientClick="return confirm('Archive item?');"><i class="fas fa-trash"></i></asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>

            <%-- REGISTRY CARD --%>
            <div class="card">
                <div class="card-header">Physical Unit Registry</div>
                <div class="roster-actions" style="padding: 20px; background: #fafafa;">
                    <asp:Button ID="btnShowAddItem" runat="server" Text="+ Register Units" OnClick="btnShowAddItem_Click" CssClass="btn btn-cancel" Style="width: 100%; border: 2px dashed var(--border-color);" />

                    <asp:Panel ID="pnlItemInput" runat="server" Visible="false" Style="margin-top: 20px;">
                        <div class="form-group">
                            <label>Rental Model</label>
                            <asp:DropDownList ID="ddlModels" runat="server" CssClass="input-field"></asp:DropDownList>
                        </div>
                        <div class="form-grid" style="margin-top: 15px;">
                            <div class="form-group">
                                <label>Quantity</label>
                                <asp:TextBox ID="txtQuantity" runat="server" TextMode="Number" Text="1" CssClass="input-field" />
                            </div>
                            <div class="form-group" style="display: flex; align-items: flex-end; gap: 10px;">
                                <asp:Button ID="btnSaveItem" runat="server" Text="Add" OnClick="btnSaveItem_Click" CssClass="btn btn-save" Style="flex: 1" />
                                <asp:Button ID="btnCancelItem" runat="server" Text="X" OnClick="btnCancelItem_Click" CssClass="btn btn-cancel" />
                            </div>
                        </div>
                    </asp:Panel>
                </div>
                                <div style="max-height: 500px; overflow-y: auto;">
                    <div class="card-scroll">
                        <asp:GridView ID="gvItems" runat="server" AutoGenerateColumns="False" DataKeyNames="ItemID" OnRowCommand="gvItems_RowCommand" CssClass="custom-grid" GridLines="None">
                            <Columns>
                                <asp:BoundField DataField="ItemID" HeaderText="ID" ItemStyle-Width="50px" ItemStyle-ForeColor="#94a3b8" />

                                <asp:TemplateField HeaderText="Asset">
                                    <ItemTemplate>
                                        <div style="font-weight: 700; color: var(--smash-blue);"><%# Eval("EquipmentName") %></div>
                                    </ItemTemplate>
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Play Mode">
                                    <ItemTemplate>
                                        <div style="font-weight: 600; color: #334155;"><%# Eval("PlayMode") %></div>
                                    </ItemTemplate>
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Rented By">
                                    <ItemTemplate>
                                        <div style="font-weight: 600; color: #0f172a;"><%# Eval("RentedBy") %></div>
                                    </ItemTemplate>
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Time Rented">
                                    <ItemTemplate>
                                        <div style="font-size: 0.85rem; color: #475569;"><%# Eval("TimeRented") %></div>
                                    </ItemTemplate>
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Status">
                                    <ItemTemplate>
                                        <span class='pill <%# Eval("Status").ToString() == "Available" ? "pill-avail" : "pill-rented" %>'><%# Eval("Status") %></span>
                                    </ItemTemplate>
                                </asp:TemplateField>

                                <asp:TemplateField ItemStyle-HorizontalAlign="Right">
                                    <ItemTemplate>
                                        <asp:LinkButton runat="server" CommandName="DeleteItem" CommandArgument='<%# Eval("ItemID") %>' CssClass="action-link delete" OnClientClick="return confirm('Remove unit?');"><i class="fas fa-times"></i></asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>