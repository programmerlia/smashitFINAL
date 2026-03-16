<%@ Page Title="Court Overrides" Language="C#" MasterPageFile="~/adminpage/admin.master" AutoEventWireup="true" CodeBehind="admin_court_override.aspx.cs" Inherits="Smash_IT.adminpage.admin_court_override" EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
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
            --danger: #e11d48;
            --warning: #f59e0b;
            --slate: #64748b;
            --text: #0f172a;
            --border: #e2e8f0;
            --shadow: 0 10px 25px rgba(30, 58, 138, 0.08);
        }

        * {
            box-sizing: border-box;
            font-family: 'Poppins', sans-serif;
        }

        .court-page {
            padding: 28px 34px;
            background: var(--bg-gray);
            min-height: 100vh;
            color: var(--text);
        }

      .page-header {
    background: linear-gradient(135deg, #ffffff 0%, #f8fbff 100%);
    border-radius: 18px;
    padding: 20px 24px;
    box-shadow: var(--shadow);
    border-left: 6px solid var(--electric-yellow);
    margin-bottom: 22px;

    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 16px;
    flex-wrap: wrap;
}
.page-header-left {
    display: flex;
    align-items: center;
    gap: 12px;
}

.page-header h1 {
    margin: 0;
    color: var(--smash-blue);
    font-size: 1.45rem;
    font-weight: 800;
    text-transform: uppercase;
}

.page-header-right {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
}

        .page-header p {
            margin: 8px 0 0 0;
            color: var(--slate);
            font-size: .94rem;
        }

        .header-date {
    min-width: 190px;

}
        .top-controls {
            display: grid;
            grid-template-columns: 220px 180px 1fr;
            gap: 16px;
            align-items: end;
            margin-bottom: 20px;
        }

     
  .form-control {
    width: 100%;
    padding: 11px 13px;
    border: 1px solid var(--border);
    border-radius: 10px;
    font-size: .92rem;
    background: #fff;
    color: var(--text);
}

.btn {
    padding: 11px 16px;
    border: none;
    border-radius: 10px;
    font-weight: 700;
    font-size: 0.83rem;
    text-transform: uppercase;
    cursor: pointer;
    transition: .2s ease;
}

.btn-primary {
    background: var(--smash-blue);
    color: white;
}

.btn-primary:hover {
    transform: translateY(-1px);
    opacity: .96;
}
     
    
        .matrix-card {
            background: var(--white);
            border-radius: 18px;
            padding: 18px;
            box-shadow: 0 6px 18px rgba(0,0,0,0.05);
            width: 100%;
        }

        .matrix-wrap {
            overflow-x: auto;
            overflow-y: auto;
            max-height: calc(100vh - 290px);
            border: 1px solid #e5e7eb;
            border-radius: 14px;
        }

        .matrix-table {
            width: 100%;
            min-width: 1200px;
            border-collapse: separate;
            border-spacing: 0;
        }

        .matrix-table th {
            position: sticky;
            top: 0;
            z-index: 2;
            background: var(--smash-blue);
            color: white;
            padding: 14px 12px;
            text-align: center;
            font-size: 0.78rem;
            text-transform: uppercase;
            white-space: nowrap;
            border-right: 1px solid rgba(255,255,255,0.12);
        }

        .matrix-table th.time-col {
            left: 0;
            z-index: 3;
            text-align: left;
            min-width: 160px;
        }

        .matrix-table td {
            padding: 10px;
            border-right: 1px solid #f1f5f9;
            border-bottom: 1px solid #f1f5f9;
            background: #fff;
            vertical-align: top;
            min-width: 170px;
            height: 96px;
        }

        .matrix-table td.time-cell {
            position: sticky;
            left: 0;
            z-index: 1;
            background: #f8fbff;
            font-weight: 700;
            color: var(--smash-blue-dark);
            min-width: 160px;
        }

        .slot-box {
            width: 100%;
            height: 100%;
            border-radius: 12px;
            padding: 10px 10px 8px;
            border: 1px solid #e5e7eb;
            cursor: pointer;
            transition: .18s ease;
            position: relative;
        }

        .slot-box:hover {
            transform: translateY(-1px);
            box-shadow: 0 8px 20px rgba(15,23,42,.08);
        }

        .slot-box.locked {
            cursor: not-allowed;
            opacity: .96;
        }

        .slot-box.pfa {
            background: #dcfce7;
            border-color: #bbf7d0;
        }

        .slot-box.queue {
            background: #fef3c7;
            border-color: #fde68a;
        }

        .slot-box.reservation {
            background: #dbeafe;
            border-color: #bfdbfe;
        }

        .slot-box.closed {
            background: #fee2e2;
            border-color: #fecaca;
        }

        .slot-mode {
            font-size: .78rem;
            font-weight: 800;
            text-transform: uppercase;
            color: #0f172a;
            margin-bottom: 6px;
        }

        .slot-name {
            font-size: .8rem;
            line-height: 1.35;
            color: #334155;
            min-height: 34px;
        }

        .slot-meta {
            margin-top: 8px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            gap: 8px;
        }

        .slot-state {
            font-size: .68rem;
            font-weight: 700;
            border-radius: 999px;
            padding: 4px 8px;
            background: rgba(255,255,255,.75);
            color: #0f172a;
        }

        .slot-source {
            font-size: .68rem;
            color: #475569;
            font-weight: 600;
        }

        .empty-slot {
            background: #f8fafc;
            border: 1px dashed #cbd5e1;
        }

        .empty-slot .slot-mode {
            color: #64748b;
        }

        .modal-overlay {
            display: none;
            position: fixed;
            inset: 0;
            background: rgba(15, 23, 42, 0.55);
            z-index: 9999;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }

        .modal-overlay.show {
            display: flex;
        }

        .modal-card {
            width: 100%;
            max-width: 420px;
            background: #fff;
            border-radius: 18px;
            overflow: hidden;
            box-shadow: 0 25px 50px rgba(0,0,0,0.18);
        }

        .modal-head {
            background: var(--smash-blue);
            color: #fff;
            padding: 16px 18px;
            display: flex;
            align-items: center;
            justify-content: space-between;
        }

        .modal-head h3 {
            margin: 0;
            font-size: 1rem;
            font-weight: 700;
        }

        .modal-close {
            background: transparent;
            border: none;
            color: #fff;
            font-size: 1rem;
            cursor: pointer;
        }

        .modal-body {
            padding: 18px;
        }

        .modal-group {
            margin-bottom: 14px;
        }

        .modal-group label {
            display: block;
            margin-bottom: 6px;
            font-size: .74rem;
            font-weight: 700;
            text-transform: uppercase;
            color: var(--smash-blue);
        }

        .modal-value {
            color: #334155;
            font-size: .92rem;
            font-weight: 600;
        }

        .modal-select {
            width: 100%;
            padding: 11px 12px;
            border: 1px solid var(--border);
            border-radius: 10px;
            font-size: .92rem;
        }

        .modal-actions {
            display: flex;
            gap: 10px;
            margin-top: 8px;
        }

        .modal-btn {
            flex: 1;
            border: none;
            border-radius: 10px;
            padding: 12px 14px;
            font-weight: 700;
            cursor: pointer;
        }

        .modal-btn.primary {
            background: var(--smash-blue);
            color: white;
        }

        .modal-btn.secondary {
            background: #e2e8f0;
            color: #334155;
        }

        .hidden {
            display: none;
        }

        @media (max-width: 900px) {
            .top-controls {
                grid-template-columns: 1fr;
            }

            .court-page {
                padding: 18px;
            }
        }
    </style>

  <script type="text/javascript">
      function openEditModal(availabilityId, timeRange, courtName, modeName, takenBy, createdByStaffId, sourceLabel, isLocked) {
          document.getElementById('<%= hfAvailabilityID.ClientID %>').value = availabilityId;
          document.getElementById('<%= hfCreatedByStaffID.ClientID %>').value = createdByStaffId;

    document.getElementById('mTimeRange').textContent = timeRange || '--';
    document.getElementById('mCourtName').textContent = courtName || '--';
    document.getElementById('mTakenBy').textContent = takenBy || 'Open / No Assigned Player';
    document.getElementById('mSourceLabel').textContent = sourceLabel || '--';

    var ddl = document.getElementById('<%= ddlModalMode.ClientID %>');
    if (ddl) ddl.value = modeName || 'PlayForAll';

    var locked = String(isLocked).toLowerCase() === "true";
          document.getElementById('<%= btnConfirmApply.ClientID %>').disabled = locked;
          ddl.disabled = locked;

          document.getElementById('slotEditModal').classList.add('show');
      }

      function confirmApplyMode() {
          return confirm("Are you sure you want to update this slot?");
      }

      function closeEditModal() {
          document.getElementById('slotEditModal').classList.remove('show');
      }



      document.addEventListener('click', function (e) {
          var modal = document.getElementById('slotEditModal');
          if (modal && e.target === modal) {
              closeEditModal();
          }
      });
  </script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <asp:ScriptManager ID="sm1" runat="server" />
    <div class="court-page">

    <div class="page-header">
    <div class="page-header-left">
        <i class="fas fa-table-cells-large" style="color: var(--electric-yellow); font-size: 1.2rem;"></i>
        <h1>Court Availability</h1>
    </div>

    <div class="page-header-right">
        <div class="header-date">
            <asp:TextBox ID="txtDate" runat="server" TextMode="Date" CssClass="form-control"></asp:TextBox>
        </div>

        <asp:Button ID="btnLoadSlots" runat="server" Text="Refresh" CssClass="btn btn-primary" OnClick="btnLoadSlots_Click" />
    </div>
</div>

        <div class="matrix-card">
            <asp:Literal ID="litMatrixTable" runat="server"></asp:Literal>
        </div>

        <asp:HiddenField ID="hfAvailabilityID" runat="server" />
        <asp:HiddenField ID="hfCreatedByStaffID" runat="server" />

        <div id="slotEditModal" class="modal-overlay">
            <div class="modal-card">
                <div class="modal-head">
                    <h3>Edit Court Slot</h3>
                    <button type="button" class="modal-close" onclick="closeEditModal()">
                        <i class="fas fa-times"></i>
                    </button>
                </div>

                <div class="modal-body">
                    <div class="modal-group">
                        <label>Time Block</label>
                        <div id="mTimeRange" class="modal-value">--</div>
                    </div>

                    <div class="modal-group">
                        <label>Court</label>
                        <div id="mCourtName" class="modal-value">--</div>
                    </div>

                    <div class="modal-group">
                        <label>Taken By</label>
                        <div id="mTakenBy" class="modal-value">--</div>
                    </div>
                    <div class="modal-group">
    <label>Source</label>
    <div id="mSourceLabel" class="modal-value">--</div>
</div>
                
                    <div class="modal-group">
                        <label>Change Mode To</label>
                        <asp:DropDownList ID="ddlModalMode" runat="server" CssClass="modal-select">
                            <asp:ListItem Value="PlayForAll" Text="Play For All"></asp:ListItem>
                            <asp:ListItem Value="Queue" Text="Queue"></asp:ListItem>
                            
                            <asp:ListItem Value="Reservation" Text="Reservation"></asp:ListItem>
                            <asp:ListItem Value="Closed" Text="Closed / Maintenance"></asp:ListItem>
                        </asp:DropDownList>
                    </div>

                    <div class="modal-actions">
                        <button type="button" class="modal-btn secondary" onclick="closeEditModal()">Cancel</button>
                        <asp:Button
                            ID="btnConfirmApply"
                            runat="server"
                            Text="Apply"
                            CssClass="modal-btn primary"
                            OnClick="btnConfirmApply_Click"
                            OnClientClick="return confirmApplyMode();" />
                    </div>
                </div>
            </div>
        </div>

    </div>
</asp:Content>