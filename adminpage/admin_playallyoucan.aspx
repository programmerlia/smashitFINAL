<%@ Page Title="Play All You Can" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_playallyoucan.aspx.cs" Inherits="Smash_IT.adminpage.admin_playallyoucan" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        :root { --smash-blue: #1e3a8a; --electric-yellow: #facc15; --danger: #e11d48; --success: #22c55e; }
        body { background-color: #f1f5f9; font-family: 'Poppins', sans-serif; color: #1e293b; }
        .admin-container { padding: 30px 40px; max-width: 1600px; margin: 0 auto; }
        .admin-intro { background: #fff; padding: 30px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); margin-bottom: 30px; border-left: 6px solid var(--electric-yellow); }
        .admin-intro h1 { font-size: 1.8rem; font-weight: 800; color: var(--smash-blue); margin: 0; text-transform: uppercase; }
        .queue-card { background: #fff; border-radius: 12px; padding: 30px; box-shadow: 0 10px 25px rgba(30, 58, 138, 0.08); border-top: 6px solid var(--smash-blue); margin-bottom: 40px; }
        .form-label { font-size: 0.75rem; font-weight: 700; color: var(--smash-blue); text-transform: uppercase; margin-bottom: 8px; display: block; }
        .form-control-custom { width: 100%; padding: 12px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 0.9rem; }
        .verification-card { text-align: center; background: #f8fafc; padding: 15px; border-radius: 12px; border: 2px dashed #cbd5e1; }
        .verify-img { width: 80px; height: 80px; border-radius: 50%; object-fit: cover; border: 3px solid var(--electric-yellow); margin-bottom: 8px; }
        .scroll-box { height: 140px; overflow-y: auto; border: 1px solid #e2e8f0; border-radius: 8px; padding: 10px; background: #fff; }
        .breakdown-card { background: var(--smash-blue); color: #fff; padding: 20px; border-radius: 12px; }
        .total-bill-amount { font-size: 1.8rem; font-weight: 800; color: var(--electric-yellow); display: block; margin-top: 10px; text-align: center; }
        .checkin-btn { background: var(--electric-yellow); color: var(--smash-blue); border: none; padding: 16px; border-radius: 8px; font-weight: 800; text-transform: uppercase; cursor: pointer; width: 100%; }
        .custom-grid { width: 100%; border-collapse: collapse; margin-top:10px; }
        .custom-grid th { background: var(--smash-blue); color: #fff; padding: 12px; text-align: left; font-size: 0.75rem; }
        .custom-grid td { padding: 12px; border-bottom: 1px solid #f1f5f9; font-size: 0.85rem; }
        .status-msg { display: block; padding: 15px; margin-bottom: 20px; border-radius: 8px; font-weight: 600; font-size: 0.85rem; }
        .btn-action { background: #eff6ff; color: var(--smash-blue); padding: 6px 10px; border-radius: 6px; text-decoration: none; font-weight: 700; font-size: 0.7rem; border: 1px solid #bfdbfe; cursor: pointer; }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-container">
        <div class="admin-intro">
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <h1>Court Management</h1>
                <asp:TextBox ID="txtFilterDate" runat="server" TextMode="Date" CssClass="form-control-custom" AutoPostBack="true" OnTextChanged="txtFilterDate_TextChanged" style="width:200px;" />
            </div>
        </div>

        <asp:Label ID="lblMsg" runat="server" Visible="false" CssClass="status-msg" />

        <div class="queue-card">
            <asp:HiddenField ID="hfPAYCID" runat="server" />
            <asp:HiddenField ID="hfWalkInID" runat="server" />
            <asp:HiddenField ID="hfPlayerData" runat="server" ClientIDMode="Static" />
            
            <div style="display: flex; gap: 25px; flex-wrap: wrap;">
                <div style="flex: 1.2; min-width: 250px;">
                    <label class="form-label">Player Search</label>
                    <asp:TextBox ID="txtPlayerName" runat="server" CssClass="form-control-custom" list="playersList" oninput="updateUI(this.value)" placeholder="Search player..." autocomplete="off" />
                    <datalist id="playersList"></datalist>

                    <div class="verification-card" style="margin-top: 20px;">
                        <img id="imgVerify" src='<%= ResolveUrl("~/uploads/avatars/person.jpg") %>' alt="Verification" class="verify-img" />
                        <br />
                        <span id="lblVerifyName" class="form-label" style="margin:0; font-size:0.6rem;">Awaiting input...</span>
                    </div>
                </div>

                <div style="flex: 1; min-width: 200px;">
                    <label class="form-label">Sport</label>
                    <asp:DropDownList ID="ddlSport" runat="server" CssClass="form-control-custom" style="margin-bottom:20px;">
                        <asp:ListItem Value="badminton">Badminton</asp:ListItem>
                        <asp:ListItem Value="pickleball">Pickleball</asp:ListItem>
                    </asp:DropDownList>
                    <label class="form-label">Check-In Time</label>
                    <asp:TextBox ID="txtTime" runat="server" TextMode="DateTimeLocal" CssClass="form-control-custom" />
                </div>

                <div style="flex: 1.2;">
                    <label class="form-label">Rentals</label>
                    <div class="scroll-box" id="boxEquip">
                        <asp:CheckBoxList ID="cblEquipment" runat="server" RepeatLayout="Flow" OnDataBound="cblEquipment_DataBound" />
                    </div>
                </div>

                <div style="flex: 1.2;">
                    <label class="form-label">Consumables</label>
                    <div class="scroll-box" id="boxShop">
                        <asp:CheckBoxList ID="cblConsumables" runat="server" RepeatLayout="Flow" OnDataBound="cblConsumables_DataBound" />
                    </div>
                </div>

                <div style="flex: 1.2; min-width: 250px;">
                    <div class="breakdown-card">
                        <div style="display:flex; justify-content:space-between; font-size:0.8rem; margin-bottom:5px;"><span>Base PAYC Fee</span><span>P80.00</span></div>
                        <div id="breakdownList"></div>
                        <asp:Label ID="lblTotalDisplay" runat="server" CssClass="total-bill-amount" Text="P80.00" />
                    </div>
                    <asp:Button ID="btnCheckIn" runat="server" Text="Confirm Session" OnClick="btnCheckIn_Click" CssClass="checkin-btn" style="margin-top:10px;" />
                    <asp:LinkButton ID="btnCancelEdit" runat="server" OnClick="btnCancelEdit_Click" style="display:block; text-align:center; margin-top:8px; font-size:0.8rem; color:#64748b;" Visible="false">Cancel Edit</asp:LinkButton>
                </div>
            </div>
        </div>

        <div style="display: flex; gap: 30px; flex-wrap: wrap;">
            <div style="flex: 1; min-width: 450px;" class="grid-card">
                <h5 class="form-label" style="border-bottom:2px solid var(--electric-yellow); padding-bottom:5px;">Badminton Sessions</h5>
                <asp:GridView ID="gvBadminton" runat="server" AutoGenerateColumns="False" CssClass="custom-grid" GridLines="None" OnRowCommand="gvActive_RowCommand" DataKeyNames="PAYCID,WalkInID">
                    <Columns>
                        <asp:TemplateField HeaderText="Player">
                            <ItemTemplate>
                                <div style="font-weight:bold;"><%# Eval("WalkInName") %></div>
                                <span style="font-size:0.7rem; color:#64748b;"><%# Eval("Duration") %></span>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="RentedItems" HeaderText="Items" />
                        <asp:TemplateField HeaderText="Due"><ItemTemplate>P<%# Eval("TotalBill") %></ItemTemplate></asp:TemplateField>
                        <asp:TemplateField HeaderText="Actions">
                            <ItemTemplate>
                                <asp:LinkButton runat="server" CommandName="EditSession" CommandArgument='<%# Eval("PAYCID") + "|" + Eval("WalkInID") %>' CssClass="btn-action">Edit</asp:LinkButton>
                                <asp:LinkButton runat="server" CommandName="FinishSession" CommandArgument='<%# Eval("PAYCID") + "|" + Eval("WalkInID") %>' CssClass="btn-action" OnClientClick="return confirm('Complete?');">Finish</asp:LinkButton>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>

            <div style="flex: 1; min-width: 450px;" class="grid-card">
                <h5 class="form-label" style="border-bottom:2px solid var(--electric-yellow); padding-bottom:5px;">Pickleball Sessions</h5>
                <asp:GridView ID="gvPickleball" runat="server" AutoGenerateColumns="False" CssClass="custom-grid" GridLines="None" OnRowCommand="gvActive_RowCommand" DataKeyNames="PAYCID,WalkInID">
                    <Columns>
                        <asp:TemplateField HeaderText="Player">
                            <ItemTemplate>
                                <div style="font-weight:bold;"><%# Eval("WalkInName") %></div>
                                <span style="font-size:0.7rem; color:#64748b;"><%# Eval("Duration") %></span>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="RentedItems" HeaderText="Items" />
                        <asp:TemplateField HeaderText="Due"><ItemTemplate>P<%# Eval("TotalBill") %></ItemTemplate></asp:TemplateField>
                        <asp:TemplateField HeaderText="Actions">
                            <ItemTemplate>
                                <asp:LinkButton runat="server" CommandName="EditSession" CommandArgument='<%# Eval("PAYCID") + "|" + Eval("WalkInID") %>' CssClass="btn-action">Edit</asp:LinkButton>
                                <asp:LinkButton runat="server" CommandName="FinishSession" CommandArgument='<%# Eval("PAYCID") + "|" + Eval("WalkInID") %>' CssClass="btn-action" OnClientClick="return confirm('Complete?');">Finish</asp:LinkButton>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        function updateUI(name) {
            const dataStr = document.getElementById('hfPlayerData').value;
            if (!dataStr) return;
            const players = JSON.parse(dataStr);
            
            // Populate Datalist only once
            const dl = document.getElementById('playersList');
            if(dl.options.length === 0) {
                players.forEach(p => { let opt = document.createElement('option'); opt.value = p.Name; dl.appendChild(opt); });
            }

            // Fuzzy Match for Image Preview
            const val = name.trim().toLowerCase();
            const player = players.find(p => p.Name.toLowerCase() === val);
            
            const imgEl = document.getElementById('imgVerify');
            const lblEl = document.getElementById('lblVerifyName');
            
            if (player) {
                // Construct path using ResolveUrl context equivalent
                imgEl.src = '<%= ResolveUrl("~/") %>' + player.Img;
                lblEl.innerText = player.Name;
                imgEl.style.borderColor = "#facc15";
            } else {
                imgEl.src = '<%= ResolveUrl("~/uploads/avatars/person.jpg") %>';
                lblEl.innerText = name.length > 0 ? "New Walk-In Player" : "Awaiting input...";
                imgEl.style.borderColor = "#cbd5e1";
            }
            calculateBreakdown(); 
        }

        function calculateBreakdown() {
            let total = 80.00;
            let html = "";
            document.querySelectorAll('.scroll-box input[type="checkbox"]:checked').forEach(cb => {
                let span = cb.closest('span');
                let price = parseFloat(span.getAttribute('data-price') || 0);
                let labelText = cb.nextSibling.textContent.trim();
                html += `<div style="display:flex; justify-content:space-between; font-size:0.8rem; margin-bottom:2px;"><span>${labelText.split('-')[0]}</span><span>P${price.toFixed(2)}</span></div>`;
                total += price;
            });
            document.getElementById('breakdownList').innerHTML = html;
            document.getElementById('<%= lblTotalDisplay.ClientID %>').innerText = "P" + total.toFixed(2);
        }

        document.addEventListener("DOMContentLoaded", () => {
            document.getElementById('boxEquip').addEventListener('change', calculateBreakdown);
            document.getElementById('boxShop').addEventListener('change', calculateBreakdown);
            updateUI(document.getElementById('<%= txtPlayerName.ClientID %>').value);
        });
    </script>
</asp:Content>