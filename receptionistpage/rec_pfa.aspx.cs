using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Linq;

namespace Smash_IT.receptionistpage
{
    public partial class rec_pfa : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                txtTime.Text = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
                txtFilterDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
                LoadPlayersList();
                LoadInventory();
                LoadActiveSessions();
            }
        }

        protected void txtFilterDate_TextChanged(object sender, EventArgs e) => LoadActiveSessions();

        private void ShowStatus(string msg, bool isError)
        {
            lblMsg.Text = msg;
            lblMsg.Visible = true;
            lblMsg.CssClass = "status-msg " + (isError ? "msg-error" : "msg-success");
        }

        private void LoadInventory()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                object widParam = string.IsNullOrEmpty(hfWalkInID.Value) ? (object)DBNull.Value : hfWalkInID.Value;

                // Updated Rental Query: Adds item specific ID and a subquery for the total available count of that model
                string sqlRent = @"SELECT ei.ItemID, 
                           em.EquipmentType + ' #' + CAST(ei.ItemID AS VARCHAR) + ' - ₱' + CAST(em.DefaultRentalPrice AS VARCHAR) + ' (' + CAST((SELECT COUNT(*) FROM tblEquipmentItem e2 WHERE e2.ModelID = em.ModelID AND e2.ItemID NOT IN (SELECT ItemID FROM tblRental WHERE ReturnedAt IS NULL)) AS VARCHAR) + ' avail)' as Display 
                           FROM tblEquipmentItem ei JOIN tblEquipmentModel em ON ei.ModelID = em.ModelID 
                           WHERE em.ItemCategory = 'Rental' AND em.IsArchived = 0 
                           AND (ei.ItemID NOT IN (SELECT ItemID FROM tblRental WHERE ReturnedAt IS NULL)
                                OR ei.ItemID IN (SELECT ItemID FROM tblRental WHERE WalkInID = @wid AND ReturnedAt IS NULL))";

                SqlCommand cmd = new SqlCommand(sqlRent, conn);
                cmd.Parameters.AddWithValue("@wid", widParam);
                DataTable dt = new DataTable(); new SqlDataAdapter(cmd).Fill(dt);
                cblEquipment.DataSource = dt;
                cblEquipment.DataTextField = "Display";
                cblEquipment.DataValueField = "ItemID";
                cblEquipment.DataBind();

                // Updated Consumable Query: Appends the current ConsumableQty to the display string
                string sqlConsumables = @"SELECT ModelID, 
                                  EquipmentType + ' - ₱' + CAST(DefaultSellPrice AS VARCHAR) + ' (' + CAST(ConsumableQty AS VARCHAR) + ' avail)' as Display 
                                  FROM tblEquipmentModel 
                                  WHERE ItemCategory = 'Consumable' AND IsArchived = 0 AND ConsumableQty > 0";

                DataTable dt2 = new DataTable();
                new SqlDataAdapter(sqlConsumables, conn).Fill(dt2);
                cblConsumables.DataSource = dt2;
                cblConsumables.DataTextField = "Display";
                cblConsumables.DataValueField = "ModelID";
                cblConsumables.DataBind();
            }
        }

        protected void cblEquipment_DataBound(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                DataTable dt = new DataTable();
                new SqlDataAdapter("SELECT ei.ItemID, m.DefaultRentalPrice FROM tblEquipmentItem ei JOIN tblEquipmentModel m ON ei.ModelID=m.ModelID", conn).Fill(dt);
                foreach (ListItem li in cblEquipment.Items)
                {
                    if (int.TryParse(li.Value, out int id))
                    {
                        var row = dt.AsEnumerable().FirstOrDefault(r => r.Field<int>("ItemID") == id);
                        if (row != null) li.Attributes.Add("data-price", row["DefaultRentalPrice"].ToString());
                    }
                }
            }
        }

        protected void cblConsumables_DataBound(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                DataTable dt = new DataTable();
                new SqlDataAdapter("SELECT ModelID, DefaultSellPrice FROM tblEquipmentModel", conn).Fill(dt);
                foreach (ListItem li in cblConsumables.Items)
                {
                    if (int.TryParse(li.Value, out int id))
                    {
                        var row = dt.AsEnumerable().FirstOrDefault(r => r.Field<int>("ModelID") == id);
                        if (row != null) li.Attributes.Add("data-price", row["DefaultSellPrice"].ToString());
                    }
                }
            }
        }

        protected void btnCheckIn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPlayerName.Text)) return;

            DateTime parsedTime;
            if (!DateTime.TryParse(txtTime.Text.Replace("T", " "), out parsedTime)) parsedTime = DateTime.Now;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    bool isEdit = !string.IsNullOrEmpty(hfPAYCID.Value);
                    string[] names = txtPlayerName.Text.Trim().Split(new char[] { ' ' }, 2);
                    int wid = 0;

                    if (isEdit)
                    {
                        if (int.TryParse(hfWalkInID.Value, out wid))
                        {
                            SqlCommand cmd = new SqlCommand("UPDATE tblPlayerWalkIn SET Firstname=@F, Lastname=@L WHERE WalkInID=@WID", conn, trans);
                            cmd.Parameters.AddWithValue("@F", names[0]);
                            cmd.Parameters.AddWithValue("@L", names.Length > 1 ? names[1] : "");
                            cmd.Parameters.AddWithValue("@WID", wid); cmd.ExecuteNonQuery();

                            SqlCommand cmdR = new SqlCommand("UPDATE tblPlayAllYouCanRegistry SET SportName=@S, CheckInTime=@T WHERE PAYCID=@PID", conn, trans);
                            cmdR.Parameters.AddWithValue("@S", ddlSport.SelectedValue);
                            cmdR.Parameters.AddWithValue("@T", parsedTime);
                            cmdR.Parameters.AddWithValue("@PID", hfPAYCID.Value); cmdR.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        SqlCommand cmdM = new SqlCommand("SELECT UserID FROM tblPlayerAccount WHERE (Firstname + ' ' + Lastname) = @Full", conn, trans);
                        cmdM.Parameters.AddWithValue("@Full", txtPlayerName.Text.Trim());
                        object uId = cmdM.ExecuteScalar() ?? DBNull.Value;

                        SqlCommand cmdW = new SqlCommand("INSERT INTO tblPlayerWalkIn (UserID, Firstname, Lastname, BasePaid) VALUES (@UID, @F, @L, 1); SELECT SCOPE_IDENTITY();", conn, trans);
                        cmdW.Parameters.AddWithValue("@UID", uId); cmdW.Parameters.AddWithValue("@F", names[0]); cmdW.Parameters.AddWithValue("@L", names.Length > 1 ? names[1] : "");
                        wid = Convert.ToInt32(cmdW.ExecuteScalar());

                        SqlCommand cmdReg = new SqlCommand("INSERT INTO tblPlayAllYouCanRegistry (WalkInID, SportName, CheckInTime, StatusName) VALUES (@W, @S, @T, 'Active')", conn, trans);
                        cmdReg.Parameters.AddWithValue("@W", wid); cmdReg.Parameters.AddWithValue("@S", ddlSport.SelectedValue);
                        cmdReg.Parameters.AddWithValue("@T", parsedTime); cmdReg.ExecuteNonQuery();

                        new SqlCommand($"INSERT INTO tblPayment (PaymentTypeName, WalkInID, PaymentDate, Amount) VALUES ('PAYC', {wid}, GETDATE(), 80)", conn, trans).ExecuteNonQuery();
                    }

                    ProcessInventory(wid, conn, trans, isEdit);
                    trans.Commit();
                    Response.Redirect(Request.RawUrl);
                }
                catch (Exception ex) { if (trans.Connection != null) trans.Rollback(); ShowStatus(ex.Message, true); }
            }
        }

        private void ProcessInventory(int wid, SqlConnection conn, SqlTransaction trans, bool isEdit)
        {
            List<int> dbRents = new List<int>();
            if (isEdit)
            {
                using (SqlCommand c = new SqlCommand("SELECT ItemID FROM tblRental WHERE WalkInID=@W AND ReturnedAt IS NULL", conn, trans))
                {
                    c.Parameters.AddWithValue("@W", wid); using (SqlDataReader r = c.ExecuteReader()) while (r.Read()) dbRents.Add((int)r["ItemID"]);
                }
            }
            foreach (ListItem li in cblEquipment.Items)
            {
                if (int.TryParse(li.Value, out int iid))
                {
                    if (li.Selected && !dbRents.Contains(iid))
                    {
                        SqlCommand cmd = new SqlCommand("INSERT INTO tblRental (WalkInID, ItemID, RentalDate) VALUES (@W, @I, GETDATE()); SELECT SCOPE_IDENTITY();", conn, trans);
                        cmd.Parameters.AddWithValue("@W", wid); cmd.Parameters.AddWithValue("@I", iid);
                        int rid = Convert.ToInt32(cmd.ExecuteScalar());
                        decimal p = Convert.ToDecimal(new SqlCommand($"SELECT m.DefaultRentalPrice FROM tblEquipmentModel m JOIN tblEquipmentItem i ON m.ModelID=i.ModelID WHERE i.ItemID={iid}", conn, trans).ExecuteScalar() ?? 0);
                        new SqlCommand($"INSERT INTO tblPayment (PaymentTypeName, WalkInID, RentalID, Amount, PaymentDate) VALUES ('Rental', {wid}, {rid}, {p}, GETDATE())", conn, trans).ExecuteNonQuery();
                    }
                    else if (!li.Selected && dbRents.Contains(iid))
                    {
                        new SqlCommand($"DELETE FROM tblPayment WHERE WalkInID={wid} AND RentalID IN (SELECT RentalID FROM tblRental WHERE WalkInID={wid} AND ItemID={iid} AND ReturnedAt IS NULL)", conn, trans).ExecuteNonQuery();
                        new SqlCommand($"DELETE FROM tblRental WHERE WalkInID={wid} AND ItemID={iid} AND ReturnedAt IS NULL", conn, trans).ExecuteNonQuery();
                    }
                }
            }
            List<int> dbCons = new List<int>();
            if (isEdit)
            {
                using (SqlCommand c = new SqlCommand("SELECT ModelID FROM tblConsumable WHERE WalkInID=@W", conn, trans))
                {
                    c.Parameters.AddWithValue("@W", wid); using (SqlDataReader r = c.ExecuteReader()) while (r.Read()) dbCons.Add((int)r["ModelID"]);
                }
            }
            foreach (ListItem li in cblConsumables.Items)
            {
                if (int.TryParse(li.Value, out int mid))
                {
                    if (li.Selected && !dbCons.Contains(mid))
                    {
                        decimal p = Convert.ToDecimal(new SqlCommand($"SELECT DefaultSellPrice FROM tblEquipmentModel WHERE ModelID={mid}", conn, trans).ExecuteScalar() ?? 0);
                        new SqlCommand($"UPDATE tblEquipmentModel SET ConsumableQty -= 1 WHERE ModelID={mid}", conn, trans).ExecuteNonQuery();
                        SqlCommand cmd = new SqlCommand("INSERT INTO tblConsumable (WalkInID, ModelID, Quantity, UnitPrice) VALUES (@W, @M, 1, @P); SELECT SCOPE_IDENTITY();", conn, trans);
                        cmd.Parameters.AddWithValue("@W", wid); cmd.Parameters.AddWithValue("@M", mid); cmd.Parameters.AddWithValue("@P", p);
                        int cid = Convert.ToInt32(cmd.ExecuteScalar());
                        new SqlCommand($"INSERT INTO tblPayment (PaymentTypeName, WalkInID, ConsumableID, Amount, PaymentDate) VALUES ('Consumable', {wid}, {cid}, {p}, GETDATE())", conn, trans).ExecuteNonQuery();
                    }
                    else if (!li.Selected && dbCons.Contains(mid))
                    {
                        new SqlCommand($"UPDATE tblEquipmentModel SET ConsumableQty += 1 WHERE ModelID={mid}", conn, trans).ExecuteNonQuery();
                        new SqlCommand($"DELETE FROM tblPayment WHERE WalkInID={wid} AND ConsumableID IN (SELECT ConsumableID FROM tblConsumable WHERE WalkInID={wid} AND ModelID={mid})", conn, trans).ExecuteNonQuery();
                        new SqlCommand($"DELETE FROM tblConsumable WHERE WalkInID={wid} AND ModelID={mid}", conn, trans).ExecuteNonQuery();
                    }
                }
            }
        }

        protected void gvActive_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            string[] args = e.CommandArgument.ToString().Split('|');
            if (e.CommandName == "EditSession")
            {
                hfPAYCID.Value = args[0]; hfWalkInID.Value = args[1];
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT W.Firstname + ' ' + W.Lastname as Name, R.SportName, R.CheckInTime FROM tblPlayAllYouCanRegistry R JOIN tblPlayerWalkIn W ON R.WalkInID=W.WalkInID WHERE R.PAYCID=@P", conn);
                    cmd.Parameters.AddWithValue("@P", args[0]);
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        txtPlayerName.Text = dr["Name"].ToString();
                        ddlSport.SelectedValue = dr["SportName"].ToString().ToLower();
                        txtTime.Text = Convert.ToDateTime(dr["CheckInTime"]).ToString("yyyy-MM-ddTHH:mm");
                        btnCheckIn.Text = "Update Session"; btnCancelEdit.Visible = true;
                    }
                    dr.Close(); LoadInventory();

                    DataTable dtR = new DataTable(); using (SqlCommand c = new SqlCommand("SELECT ItemID FROM tblRental WHERE WalkInID=@W AND ReturnedAt IS NULL", conn)) { c.Parameters.AddWithValue("@W", args[1]); new SqlDataAdapter(c).Fill(dtR); }
                    foreach (ListItem li in cblEquipment.Items) { li.Selected = dtR.AsEnumerable().Any(r => r.Field<int>("ItemID").ToString() == li.Value); }

                    DataTable dtC = new DataTable(); using (SqlCommand c = new SqlCommand("SELECT ModelID FROM tblConsumable WHERE WalkInID=@W", conn)) { c.Parameters.AddWithValue("@W", args[1]); new SqlDataAdapter(c).Fill(dtC); }
                    foreach (ListItem li in cblConsumables.Items) { li.Selected = dtC.AsEnumerable().Any(r => r.Field<int>("ModelID").ToString() == li.Value); }
                }
                ScriptManager.RegisterStartupScript(this, GetType(), "ui", $"updateUI('{txtPlayerName.Text}');", true);
            }
            else if (e.CommandName == "FinishSession")
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    SqlCommand cmdP = new SqlCommand("UPDATE tblPlayAllYouCanRegistry SET StatusName='Completed' WHERE PAYCID=@P", conn); cmdP.Parameters.AddWithValue("@P", args[0]); cmdP.ExecuteNonQuery();
                    SqlCommand cmdR = new SqlCommand("UPDATE tblRental SET ReturnedAt=GETDATE() WHERE WalkInID=@W AND ReturnedAt IS NULL", conn); cmdR.Parameters.AddWithValue("@W", args[1]); cmdR.ExecuteNonQuery();
                }
                Response.Redirect(Request.RawUrl);
            }
        }

        private void LoadActiveSessions()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string sql = @"SELECT R.PAYCID, R.WalkInID, (W.Firstname + ' ' + ISNULL(W.Lastname,'')) as WalkInName, R.SportName as Sport, R.CheckInTime, 
                             (SELECT ISNULL(SUM(Amount),0) FROM tblPayment WHERE WalkInID = W.WalkInID) as TotalBill, 
                             (SELECT ISNULL(STRING_AGG(PaymentTypeName, ', '),'None') FROM tblPayment WHERE WalkInID = W.WalkInID AND PaymentTypeName != 'PAYC') as RentedItems, 
                             DATEDIFF(MINUTE, R.CheckInTime, GETDATE()) as MinDiff FROM tblPlayAllYouCanRegistry R JOIN tblPlayerWalkIn W ON R.WalkInID = W.WalkInID 
                             WHERE R.StatusName = 'Active' AND CAST(R.CheckInTime AS DATE) = @D ORDER BY R.CheckInTime DESC";
                SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                da.SelectCommand.Parameters.AddWithValue("@D", txtFilterDate.Text);
                DataTable dt = new DataTable(); da.Fill(dt);
                dt.Columns.Add("Duration");
                foreach (DataRow r in dt.Rows) { int m = Convert.ToInt32(r["MinDiff"]); r["Duration"] = $"{m / 60}h {m % 60}m"; }
                gvBadminton.DataSource = new DataView(dt) { RowFilter = "Sport = 'badminton'" }; gvBadminton.DataBind();
                gvPickleball.DataSource = new DataView(dt) { RowFilter = "Sport = 'pickleball'" }; gvPickleball.DataBind();
            }
        }

        private void LoadPlayersList()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                DataTable dt = new DataTable();
                new SqlDataAdapter("SELECT (Firstname + ' ' + Lastname) as FullName, ImgPath FROM tblPlayerAccount", conn).Fill(dt);
                var list = new List<object>();
                foreach (DataRow r in dt.Rows) list.Add(new { Name = r["FullName"].ToString(), Img = r["ImgPath"].ToString() });
                hfPlayerData.Value = new JavaScriptSerializer().Serialize(list);
            }
        }

        protected void btnCancelEdit_Click(object sender, EventArgs e) => Response.Redirect(Request.RawUrl);
    }
}