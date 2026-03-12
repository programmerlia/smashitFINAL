using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;

namespace Smash_IT.adminpage
{
    public partial class inventory : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        // ViewState keeps the filter active between clicks
        private string CurrentFilter
        {
            get { return ViewState["Filter"]?.ToString() ?? "All"; }
            set { ViewState["Filter"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                RefreshData();
                LoadDropdowns();
            }
        }

        private void RefreshData()
        {
            LoadModels();
            LoadItems();
        }

        protected void txtSearchCatalog_TextChanged(object sender, EventArgs e) => LoadModels();

        // Fixes the "Name does not exist" error by correctly referencing the buttons
        protected void FilterItems_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            CurrentFilter = btn.CommandArgument;

            // Reset all and apply active class
            btnFilterAll.CssClass = "btn-filter";
            btnFilterRental.CssClass = "btn-filter";
            btnFilterConsumable.CssClass = "btn-filter";

            btn.CssClass = "btn-filter active";

            LoadModels();
        }

        private void LoadModels()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string search = txtSearchCatalog.Text.Trim();
                string query = @"
                    SELECT m.ModelID, m.EquipmentType, m.EquipmentSpec, m.ItemCategory, m.DefaultRentalPrice, m.DefaultSellPrice,
                    CASE WHEN m.ItemCategory = 'Consumable' THEN ISNULL(m.ConsumableQty, 0)
                    ELSE (SELECT COUNT(*) FROM tblEquipmentItem WHERE ModelID = m.ModelID AND IsDeleted = 0)
                    END AS TotalStock FROM tblEquipmentModel m 
                    WHERE m.IsArchived = 0 ";

                if (CurrentFilter != "All") query += " AND m.ItemCategory = @Filter ";
                if (!string.IsNullOrEmpty(search)) query += " AND (m.EquipmentType LIKE @search OR m.EquipmentSpec LIKE @search) ";

                query += " ORDER BY m.ItemCategory DESC, m.EquipmentType ASC";

                SqlCommand cmd = new SqlCommand(query, conn);
                if (CurrentFilter != "All") cmd.Parameters.AddWithValue("@Filter", CurrentFilter);
                if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@search", "%" + search + "%");

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                gvModels.DataSource = dt;
                gvModels.DataBind();
            }
        }

        private void LoadItems()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"
                    SELECT i.ItemID, m.EquipmentType + ' (' + ISNULL(m.EquipmentSpec, 'Std') + ')' AS EquipmentName,
                    CASE WHEN EXISTS (SELECT 1 FROM tblRental r WHERE r.ItemID = i.ItemID AND r.ReturnedAt IS NULL) 
                    THEN 'In Use' ELSE 'Available' END AS Status
                    FROM tblEquipmentItem i 
                    JOIN tblEquipmentModel m ON i.ModelID = m.ModelID 
                    WHERE i.IsDeleted = 0 ORDER BY i.ItemID DESC";
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                gvItems.DataSource = dt;
                gvItems.DataBind();
            }
        }

        private void LoadDropdowns()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = "SELECT ModelID, EquipmentType + ' (' + ISNULL(EquipmentSpec, 'Std') + ')' AS DisplayName FROM tblEquipmentModel WHERE ItemCategory = 'Rental' AND IsArchived = 0";
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable(); da.Fill(dt);
                ddlModels.DataSource = dt;
                ddlModels.DataTextField = "DisplayName";
                ddlModels.DataValueField = "ModelID";
                ddlModels.DataBind();
                ddlModels.Items.Insert(0, new ListItem("-- Select Rental Model --", ""));
            }
        }

        protected void btnSaveModel_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query;
                    bool isUpdate = !string.IsNullOrEmpty(hfSelectedModelID.Value);

                    if (isUpdate)
                        query = "UPDATE tblEquipmentModel SET EquipmentType=@Type, EquipmentSpec=@Spec, ItemCategory=@Cat, DefaultRentalPrice=@RP, DefaultSellPrice=@SP, ConsumableQty=@Qty WHERE ModelID=@ID";
                    else
                        query = "INSERT INTO tblEquipmentModel (EquipmentType, EquipmentSpec, ItemCategory, DefaultRentalPrice, DefaultSellPrice, ConsumableQty, IsArchived) VALUES (@Type, @Spec, @Cat, @RP, @SP, @Qty, 0)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Type", txtEquipType.Text.Trim());
                    cmd.Parameters.AddWithValue("@Spec", string.IsNullOrWhiteSpace(txtEquipSpec.Text) ? (object)DBNull.Value : txtEquipSpec.Text.Trim());
                    cmd.Parameters.AddWithValue("@Cat", ddlItemCategory.SelectedValue);

                    if (ddlItemCategory.SelectedValue == "Rental")
                    {
                        cmd.Parameters.AddWithValue("@RP", decimal.TryParse(txtRentalPrice.Text, out decimal rp) ? (object)rp : (object)0);
                        cmd.Parameters.AddWithValue("@SP", DBNull.Value);
                        cmd.Parameters.AddWithValue("@Qty", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@RP", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SP", decimal.TryParse(txtSellPrice.Text, out decimal sp) ? (object)sp : (object)0);
                        cmd.Parameters.AddWithValue("@Qty", int.TryParse(txtConsumableQty.Text, out int cq) ? (object)cq : (object)0);
                    }

                    if (isUpdate) cmd.Parameters.AddWithValue("@ID", hfSelectedModelID.Value);
                    cmd.ExecuteNonQuery();

                    ShowMessage(isUpdate ? "Updated successfully." : "Added successfully.");
                    btnCancelModel_Click(null, null);
                    RefreshData();
                    LoadDropdowns();
                }
            }
            catch (Exception ex) { ShowMessage("Error: " + ex.Message, true); }
        }

        protected void gvModels_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditModel")
            {
                int id = Convert.ToInt32(e.CommandArgument);
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    SqlCommand cmd = new SqlCommand("SELECT * FROM tblEquipmentModel WHERE ModelID = @ID", conn);
                    cmd.Parameters.AddWithValue("@ID", id);
                    conn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        hfSelectedModelID.Value = id.ToString();
                        txtEquipType.Text = dr["EquipmentType"].ToString();
                        txtEquipSpec.Text = dr["EquipmentSpec"].ToString();
                        ddlItemCategory.SelectedValue = dr["ItemCategory"].ToString();
                        txtRentalPrice.Text = dr["DefaultRentalPrice"] != DBNull.Value ? dr["DefaultRentalPrice"].ToString() : "";
                        txtSellPrice.Text = dr["DefaultSellPrice"] != DBNull.Value ? dr["DefaultSellPrice"].ToString() : "";
                        txtConsumableQty.Text = dr["ConsumableQty"] != DBNull.Value ? dr["ConsumableQty"].ToString() : "";
                        pnlModelInput.Visible = true;
                        btnSaveModel.Text = "Update Item";
                        ddlItemCategory_SelectedIndexChanged(null, null);
                    }
                }
            }
            else if (e.CommandName == "DeleteModel") ExecuteDelete("tblEquipmentModel", Convert.ToInt32(e.CommandArgument));
        }

        protected void btnSaveItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ddlModels.SelectedValue)) return;
            int qty = int.TryParse(txtQuantity.Text, out int q) ? q : 1;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                for (int i = 0; i < qty; i++)
                {
                    SqlCommand cmd = new SqlCommand("INSERT INTO tblEquipmentItem (ModelID, IsDeleted) VALUES (@MID, 0)", conn);
                    cmd.Parameters.AddWithValue("@MID", ddlModels.SelectedValue);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshData();
            btnCancelItem_Click(null, null);
        }

        protected void gvItems_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "DeleteItem") ExecuteDelete("tblEquipmentItem", Convert.ToInt32(e.CommandArgument));
        }

        private void ExecuteDelete(string tableName, int id)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string sql = (tableName == "tblEquipmentModel")
                    ? "UPDATE tblEquipmentModel SET IsArchived = 1 WHERE ModelID = @ID"
                    : "UPDATE tblEquipmentItem SET IsDeleted = 1 WHERE ItemID = @ID";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            RefreshData();
            ShowMessage("Removed from registry.");
        }

        protected void ddlItemCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isRental = ddlItemCategory.SelectedValue == "Rental";
            pnlRentalFields.Visible = isRental;
            pnlConsumableFields.Visible = !isRental;
            pnlConsumableQty.Visible = !isRental;
        }

        protected void btnShowAddModel_Click(object sender, EventArgs e) { pnlModelInput.Visible = true; btnSaveModel.Text = "Save Item"; hfSelectedModelID.Value = ""; ClearFields(); ddlItemCategory_SelectedIndexChanged(null, null); }
        protected void btnCancelModel_Click(object sender, EventArgs e) { pnlModelInput.Visible = false; ClearFields(); }
        protected void btnShowAddItem_Click(object sender, EventArgs e) { pnlItemInput.Visible = true; }
        protected void btnCancelItem_Click(object sender, EventArgs e) { pnlItemInput.Visible = false; }
        private void ClearFields() { txtEquipType.Text = txtEquipSpec.Text = txtRentalPrice.Text = txtSellPrice.Text = txtConsumableQty.Text = ""; hfSelectedModelID.Value = ""; }
        private void ShowMessage(string m, bool err = false) { lblMsg.Text = m; lblMsg.Visible = true; lblMsg.CssClass = err ? "status-msg msg-error" : "status-msg msg-success"; }
    }
}