<%@ Page Title="Financial Dashboard | SmashIt" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_transactions.aspx.cs" Inherits="Smash_IT.adminpage.admin_transactions" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <style>
        .staff-container { padding: 30px 40px; font-family: 'Poppins', sans-serif; background-color: #f8fafc; }
        .stats-container { display: flex; gap: 20px; margin-bottom: 30px; }
        .stat-card { background: #fff; padding: 20px 25px; border-radius: 12px; flex: 1; border-left: 6px solid #facc15; box-shadow: 0 4px 15px rgba(30,58,138,0.05); }
        .stat-number { font-size: 1.8rem; font-weight: 800; color: #1e3a8a; }
        .chart-card { background: #fff; padding: 25px; border-radius: 12px; box-shadow: 0 10px 25px rgba(30,58,138,0.08); border-top: 5px solid #1e3a8a; margin-bottom: 30px; }
        .custom-grid { width: 100%; border-collapse: collapse; background: white; border-radius: 12px; overflow: hidden; }
        .custom-grid th { background: #1e3a8a; color: white; padding: 15px; text-align: left; font-size: 0.85rem; text-transform: uppercase; }
        .custom-grid td { padding: 15px; border-bottom: 1px solid #f1f5f9; font-size: 0.9rem; }
        
        /* Updated Buttons */
        .void-btn { color: #e11d48; background: #fff1f2; border: 1px solid #fecaca; padding: 5px 12px; border-radius: 6px; cursor: pointer; font-weight: 600; font-size: 0.75rem; text-decoration: none; display: inline-block; margin-left: 5px; }
        .view-btn { color: #1e3a8a; background: #eff6ff; border: 1px solid #bfdbfe; padding: 5px 12px; border-radius: 6px; cursor: pointer; font-weight: 600; font-size: 0.75rem; text-decoration: none; display: inline-block; }
        .status-voided { text-decoration: line-through; color: #94a3b8; opacity: 0.6; }

        /* Custom Modal Styles */
        .custom-modal-backdrop { display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(15, 23, 42, 0.6); z-index: 1040; backdrop-filter: blur(3px); }
        .custom-modal { display: none; position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%); background: white; width: 100%; max-width: 500px; border-radius: 16px; z-index: 1050; box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1); padding: 30px; }
        .custom-modal-header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #f1f5f9; padding-bottom: 15px; margin-bottom: 20px; }
        .custom-modal-title { font-weight: 800; color: #1e3a8a; font-size: 1.25rem; margin: 0; }
        .close-btn { background: none; border: none; font-size: 1.5rem; cursor: pointer; color: #64748b; line-height: 1; }
        .detail-row { display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px dashed #e2e8f0; font-size: 0.9rem; }
        .detail-label { color: #64748b; font-weight: 600; font-size: 0.8rem; text-transform: uppercase; }
        .detail-value { font-weight: 700; color: #0f172a; text-align: right; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="staff-container">
        <div class="d-flex justify-content-between align-items-center mb-4 bg-white p-3 rounded-3 shadow-sm">
            <h2 class="fw-bold text-primary m-0">Financial Dashboard</h2>
            <div class="d-flex gap-2">
                <asp:DropDownList ID="ddlTimeFilter" runat="server" AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed" CssClass="form-select w-auto">
                    <asp:ListItem Text="All History" Value="All" />
                    <asp:ListItem Text="Today" Value="Day" />
                    <asp:ListItem Text="Last 7 Days" Value="Week" />
                    <asp:ListItem Text="This Month" Value="Month" />
                </asp:DropDownList>
                <asp:Button ID="btnExport" runat="server" Text="Export CSV" OnClick="btnExport_Click" CssClass="btn btn-dark fw-bold" />
            </div>
        </div>

        <div class="stats-container">
            <div class="stat-card">
                <div>
                    <div class="small text-muted fw-bold">TOTAL REVENUE</div>
                    <div class="stat-number" id="lblTotalRevenue" runat="server">₱0.00</div>
                </div>
            </div>
            <div class="stat-card" style="border-left-color: #10b981;">
                <div>
                    <div class="small text-muted fw-bold">TOTAL SALES</div>
                    <div class="stat-number" id="lblTransCount" runat="server">0</div>
                </div>
            </div>
            <div class="stat-card" style="border-left-color: #ef4444;">
                <div>
                    <div class="small text-muted fw-bold">VOIDED TOTAL</div>
                    <div class="stat-number" id="lblVoidTotal" runat="server">₱0.00</div>
                </div>
            </div>
        </div>

        <div class="chart-card">
            <h6 class="fw-bold text-primary text-uppercase small mb-4">Revenue Trend</h6>
            <div style="height: 300px;"><canvas id="revenueChart"></canvas></div>
        </div>

        <div class="bg-white rounded-3 shadow-sm overflow-hidden">
            <asp:GridView ID="gvTransactions" runat="server" AutoGenerateColumns="False" CssClass="custom-grid" GridLines="None" OnRowCommand="gvTransactions_RowCommand">
                <Columns>
                    <asp:BoundField DataField="PaymentID" HeaderText="Ref #" ItemStyle-CssClass="fw-bold text-muted ps-4" HeaderStyle-CssClass="ps-4" />
                    <asp:BoundField DataField="PaymentDate" HeaderText="Timestamp" DataFormatString="{0:MMM dd, HH:mm}" />
                    <asp:TemplateField HeaderText="Customer & Details">
                        <ItemTemplate>
                            <div class='<%# Eval("PaymentTypeName").ToString() == "VOID" ? "status-voided" : "" %>'>
                                <div class="fw-bold"><%# Eval("CustomerName") %></div>
                                <div class="small text-muted text-uppercase" style="font-size:0.7rem;"><%# Eval("ItemDetails") %></div>
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Amount" ItemStyle-CssClass="text-end" HeaderStyle-CssClass="text-end">
                        <ItemTemplate>
                            <span class='<%# Convert.ToDecimal(Eval("Amount")) < 0 ? "text-danger fw-bold" : "fw-bold text-success" %>'>
                                ₱<%# Eval("Amount", "{0:N2}") %>
                            </span>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField ItemStyle-CssClass="pe-4 text-end" HeaderStyle-CssClass="pe-4 text-end">
                        <ItemTemplate>
                            <asp:LinkButton runat="server" CommandName="ViewDetails" CommandArgument='<%# Eval("PaymentID") %>' CssClass="view-btn">View</asp:LinkButton>
                            <asp:LinkButton runat="server" CommandName="VoidTrans" CommandArgument='<%# Eval("PaymentID") %>' 
                                CssClass="void-btn" Visible='<%# Convert.ToDecimal(Eval("Amount")) > 0 %>' 
                                OnClientClick="return confirm('VOID this transaction?');">Void</asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </div>

    <div id="modalBackdrop" class="custom-modal-backdrop" onclick="closeModal()"></div>
    <div id="detailsModal" class="custom-modal">
        <div class="custom-modal-header">
            <h4 class="custom-modal-title">Transaction Details</h4>
            <button type="button" class="close-btn" onclick="closeModal()">&times;</button>
        </div>
        <asp:Literal ID="litModalContent" runat="server"></asp:Literal>
    </div>

    <script>
        // Modal logic
        function showModal() {
            document.getElementById('modalBackdrop').style.display = 'block';
            document.getElementById('detailsModal').style.display = 'block';
        }

        function closeModal() {
            document.getElementById('modalBackdrop').style.display = 'none';
            document.getElementById('detailsModal').style.display = 'none';
        }

        // Chart logic
        document.addEventListener("DOMContentLoaded", function () {
            const ctx = document.getElementById('revenueChart').getContext('2d');
            new Chart(ctx, {
                type: 'line',
                data: {
                    labels: <%= ChartLabels %>,
                    datasets: [{
                        label: 'Revenue',
                        data: <%= ChartData %>,
                        borderColor: '#1e3a8a',
                        backgroundColor: 'rgba(30,58,138,0.1)',
                        fill: true,
                        tension: 0.4
                    }]
                },
                options: { maintainAspectRatio: false, plugins: { legend: { display: false } } }
            });
        });
    </script>
</asp:Content>