using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web.UI.WebControls;

namespace Smash_IT.adminpage
{
    public partial class admin_announcement : System.Web.UI.Page
    {
        // 1. Declare the variable, but do not set it here
        private string connectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            connectionString = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

            if (!IsPostBack)
            {
                // Make sure there are no // slashes in front of this line!
                LoadGrids();
            }
        }

        // Add this method anywhere inside your Admin_Announcement class
        private void LoadGrids()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                // Load Published (ViewStatus = 1)
                string queryPub = "SELECT AnnouncementID, Title, CreatedAt, ViewStatus, DisplayOrder FROM tblAnnouncement WHERE ViewStatus = 1 ORDER BY DisplayOrder ASC, CreatedAt DESC";
                using (SqlCommand cmd = new SqlCommand(queryPub, con))
                {
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dtPub = new DataTable();
                        sda.Fill(dtPub);
                        gvPublished.DataSource = dtPub;
                        gvPublished.DataBind();
                    }
                }

                // Load Drafts (ViewStatus = 0)
                string queryDraft = "SELECT AnnouncementID, Title, CreatedAt, ViewStatus, DisplayOrder FROM tblAnnouncement WHERE ViewStatus = 0 ORDER BY CreatedAt DESC";
                using (SqlCommand cmd = new SqlCommand(queryDraft, con))
                {
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dtDraft = new DataTable();
                        sda.Fill(dtDraft);
                        gvDrafts.DataSource = dtDraft;
                        gvDrafts.DataBind();
                    }
                }
            }
        }

        // --- 2. Add these Event Handlers for the PUBLISHED GridView ---

        protected void gvPublished_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Get the ID of the clicked published announcement
            int id = Convert.ToInt32(gvPublished.SelectedDataKey.Value);

            // Store it in the hidden field so the Save button knows we are updating an existing record
            hfAnnouncementID.Value = id.ToString();

            // Fetch the data from the database and populate the main form
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT Title, [Content], ViewStatus, DisplayOrder FROM tblAnnouncement WHERE AnnouncementID = @ID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtTitle.Text = reader["Title"].ToString();
                            txtDescription.Text = reader["Content"].ToString();
                            chkViewStatus.Checked = Convert.ToBoolean(reader["ViewStatus"]);
                            txtOrder.Text = reader["DisplayOrder"].ToString();

                            lblMessage.Text = "Published Announcement loaded. You can now edit and re-save.";
                            lblMessage.CssClass = "message";
                        }
                    }
                }
            }
        }
        protected void gvPublished_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            DeleteRecord(gvPublished, e);
        }

        // --- 3. Add these Event Handlers for the DRAFTS GridView ---
        protected void gvDrafts_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Get the ID of the clicked draft
            int id = Convert.ToInt32(gvDrafts.SelectedDataKey.Value);

            // Store it in the hidden field so the Save button knows we are updating, not inserting
            hfAnnouncementID.Value = id.ToString();

            // Fetch the data from the database and populate the form
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT Title, [Content], ViewStatus, DisplayOrder FROM tblAnnouncement WHERE AnnouncementID = @ID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtTitle.Text = reader["Title"].ToString();
                            txtDescription.Text = reader["Content"].ToString();
                            chkViewStatus.Checked = Convert.ToBoolean(reader["ViewStatus"]);
                            txtOrder.Text = reader["DisplayOrder"].ToString();

                            lblMessage.Text = "Draft loaded. You can now edit and re-save or publish.";
                            lblMessage.CssClass = "message";
                        }
                    }
                }
            }
        }

        protected void gvDrafts_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            DeleteRecord(gvDrafts, e);
        }

        // --- 4. The Reusable SQL Execution Methods ---
        private void UpdateRecord(GridView gv, GridViewUpdateEventArgs e)
        {
            int id = Convert.ToInt32(gv.DataKeys[e.RowIndex].Value);
            string title = e.NewValues["Title"].ToString();
            int order = Convert.ToInt32(e.NewValues["DisplayOrder"]);
            bool status = Convert.ToBoolean(e.NewValues["ViewStatus"]);

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "UPDATE tblAnnouncement SET Title=@Title, DisplayOrder=@Order, ViewStatus=@Status WHERE AnnouncementID=@ID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@Order", order);
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@ID", id);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            gv.EditIndex = -1; // Closes the edit boxes
            LoadGrids(); // Refresh both grids because changing ViewStatus moves the record between tables!
        }

        private void DeleteRecord(GridView gv, GridViewDeleteEventArgs e)
        {
            int id = Convert.ToInt32(gv.DataKeys[e.RowIndex].Value);

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "DELETE FROM tblAnnouncement WHERE AnnouncementID=@ID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ID", id);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            LoadGrids(); // Refresh the display
        }

        protected void btnPublish_Click(object sender, EventArgs e)
        {
            SaveAnnouncement(true); // true = Active/Published
        }

        protected void btnSaveDraft_Click(object sender, EventArgs e)
        {
            SaveAnnouncement(false); // false = Inactive/Draft
        }

        private void SaveAnnouncement(bool isPublished)
        {
            string title = txtTitle.Text.Trim();
            string content = txtDescription.Text.Trim();
            bool viewStatus = isPublished ? chkViewStatus.Checked : false;

            int displayOrder = 1;
            int.TryParse(txtOrder.Text, out displayOrder);

            byte[] imageBytes = null;
            string fileName = null;
            string fileType = null;

            if (fileUploadImage.HasFile)
            {
                using (BinaryReader br = new BinaryReader(fileUploadImage.PostedFile.InputStream))
                {
                    imageBytes = br.ReadBytes(fileUploadImage.PostedFile.ContentLength);
                }
                fileName = fileUploadImage.FileName;
                fileType = fileUploadImage.PostedFile.ContentType;
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;

                // CHECK: Are we creating a new one, or updating a loaded draft?
                if (string.IsNullOrEmpty(hfAnnouncementID.Value))
                {
                    // --- CREATE NEW (INSERT) ---
                    cmd.CommandText = @"INSERT INTO tblAnnouncement 
                                (Title, [Content], ImageFIleData, ImageFileName, ImageFileType, CreatedAt, ViewStatus, DisplayOrder, CreatedByStaffID) 
                                VALUES 
                                (@Title, @Content, @ImageFIleData, @ImageFileName, @ImageFileType, @CreatedAt, @ViewStatus, @DisplayOrder, @CreatedByStaffID)";

                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    cmd.Parameters.Add("@ImageFIleData", SqlDbType.VarBinary).Value = (object)imageBytes ?? DBNull.Value;
                    cmd.Parameters.AddWithValue("@ImageFileName", (object)fileName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ImageFileType", (object)fileType ?? DBNull.Value);
                }
                else
                {
                    // --- UPDATE EXISTING ---
                    // We check if a new image was uploaded. If not, we update the text but keep the old image in the database safely.
                    if (fileUploadImage.HasFile)
                    {
                        cmd.CommandText = @"UPDATE tblAnnouncement SET 
                                    Title=@Title, [Content]=@Content, ViewStatus=@ViewStatus, DisplayOrder=@DisplayOrder, 
                                    ImageFIleData=@ImageFIleData, ImageFileName=@ImageFileName, ImageFileType=@ImageFileType 
                                    WHERE AnnouncementID=@ID";
                        cmd.Parameters.Add("@ImageFIleData", SqlDbType.VarBinary).Value = imageBytes;
                        cmd.Parameters.AddWithValue("@ImageFileName", fileName);
                        cmd.Parameters.AddWithValue("@ImageFileType", fileType);
                    }
                    else
                    {
                        cmd.CommandText = @"UPDATE tblAnnouncement SET 
                                    Title=@Title, [Content]=@Content, ViewStatus=@ViewStatus, DisplayOrder=@DisplayOrder 
                                    WHERE AnnouncementID=@ID";
                    }
                    cmd.Parameters.AddWithValue("@ID", Convert.ToInt32(hfAnnouncementID.Value));
                }

                // Add the parameters that are shared between both INSERT and UPDATE
                cmd.Parameters.AddWithValue("@Title", title);
                cmd.Parameters.AddWithValue("@Content", content);
                cmd.Parameters.AddWithValue("@ViewStatus", viewStatus);
                cmd.Parameters.AddWithValue("@DisplayOrder", displayOrder);
                cmd.Parameters.AddWithValue("@CreatedByStaffID", 1);

                try
                {
                    con.Open();
                    cmd.ExecuteNonQuery();

                    lblMessage.Text = string.IsNullOrEmpty(hfAnnouncementID.Value) ? "Announcement successfully created!" : "Draft successfully updated/published!";
                    lblMessage.CssClass = "message";

                    ClearForm();
                    LoadGrids();
                }
                catch (Exception ex)
                {
                    lblMessage.Text = "Error saving announcement: " + ex.Message;
                    lblMessage.CssClass = "error";
                }
            }
        }

        private void ClearForm()
        {
            hfAnnouncementID.Value = string.Empty;
            txtTitle.Text = string.Empty;
            txtDescription.Text = string.Empty;
            txtOrder.Text = "1";
            chkViewStatus.Checked = true;
            txtDateUntil.Text = string.Empty;
        }
    }

}