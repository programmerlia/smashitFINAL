using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Web.UI.WebControls;
using System.Xml.Linq;

namespace Smash_IT.adminpage
{
    public partial class Content_Manager : System.Web.UI.Page
    {
        private string connectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            connectionString = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

            if (!IsPostBack)
            {
                // 1. Restore the Module state from the URL (if it exists)
                string module = Request.QueryString["module"] ?? "Announcement";
                if (ddlModule.Items.FindByValue(module) != null)
                {
                    ddlModule.SelectedValue = module;
                }

                SwitchModule();

                // 2. Restore the inner dropdown selections (if they exist)
                string sec = Request.QueryString["sec"];
                if (!string.IsNullOrEmpty(sec))
                {
                    if (module == "AboutUs" && DropDownList1.Items.FindByValue(sec) != null)
                    {
                        DropDownList1.SelectedValue = sec;
                        ddlAboutSection_SelectedIndexChanged(null, null);
                    }
                    else if (module == "Announcement" && DropDownList2.Items.FindByValue(sec) != null)
                    {
                        DropDownList2.SelectedValue = sec;
                        ddlAnnouncement_SelectedIndexChanged(null, null);
                    }
                }

                // 3. Display success messages based on URL parameter
                string msg = Request.QueryString["msg"];
                if (msg == "saved") ShowMessage("Action completed successfully!", false);
                else if (msg == "deleted") ShowMessage("Record deleted successfully.", false);
            }
        }

        /* ================= HELPER METHODS ================= */
        private void ShowMessage(string message, bool isError = false)
        {
            lblMessage.Text = message;
            lblMessage.ForeColor = isError ? Color.Red : Color.Green;
        }

        // The PRG (Post-Redirect-Get) Helper
        private void RedirectWithState(string msgCode)
        {
            string module = ddlModule.SelectedValue;
            string sec = "";

            if (module == "AboutUs") sec = DropDownList1.SelectedValue;
            else if (module == "Announcement") sec = DropDownList2.SelectedValue;

            string url = $"{Request.Url.AbsolutePath}?module={module}&sec={sec}&msg={msgCode}";
            Response.Redirect(url, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private string UploadImageAndGetPath(FileUpload fu, string currentImagePath)
        {
            if (fu == null || !fu.HasFile)
                return currentImagePath;

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            string fileExtension = Path.GetExtension(fu.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                ShowMessage("Invalid file type. Allowed: jpg, jpeg, png, gif, webp.", true);
                return currentImagePath;
            }

            try
            {
                string cloudname = ConfigurationManager.AppSettings["CloudinaryCloudName"];
                string apikey = ConfigurationManager.AppSettings["CloudinaryApiKey"];
                string secretKey = ConfigurationManager.AppSettings["CloudinaryApiSecret"];

                if (string.IsNullOrWhiteSpace(cloudname) ||
                    string.IsNullOrWhiteSpace(apikey) ||
                    string.IsNullOrWhiteSpace(secretKey))
                {
                    ShowMessage("Cloudinary settings are missing in Web.config.", true);
                    return currentImagePath;
                }

                Account account = new Account(cloudname, apikey, secretKey);
                Cloudinary cloudinary = new Cloudinary(account);

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fu.FileName, fu.PostedFile.InputStream),
                    Folder = "smash-it-uploads"
                };


                var uploadResult = cloudinary.Upload(uploadParams);
                ShowMessage(
    "Upload debug -> PublicId: " + Convert.ToString(uploadResult.PublicId) +
    " | SecureUrl: " + Convert.ToString(uploadResult.SecureUrl) +
    " | Url: " + Convert.ToString(uploadResult.Url),
    false
);

                if (uploadResult == null)
                {
                    ShowMessage("Cloudinary upload returned no result.", true);
                    return currentImagePath;
                }

                if (uploadResult.Error != null)
                {
                    ShowMessage("Cloudinary upload failed: " + uploadResult.Error.Message, true);
                    return currentImagePath;
                }

                if (uploadResult.SecureUrl != null)
                    return uploadResult.SecureUrl.ToString();

                if (uploadResult.Url != null)
                    return uploadResult.Url.ToString();

                ShowMessage("Upload finished but no image URL was returned by Cloudinary.", true);
                return currentImagePath;
            }
            catch (Exception ex)
            {
                ShowMessage("Cloudinary API Upload failed: " + ex.Message, true);
                return currentImagePath;
            }
        }
        /* ================= NAVIGATION & TOGGLES ================= */
        protected void ddlModule_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClearFields();
            SwitchModule();
        }

        private void SwitchModule()
        {
            string module = ddlModule.SelectedValue;

            phMiniHome.Visible = (module == "Home");
            phMiniAnno.Visible = (module == "Announcement");
            phMiniAbout.Visible = (module == "AboutUs");

            phAnnouncementTables.Visible = (module == "Announcement");
            phGenericTables.Visible = (module == "Home" || module == "AboutUs");

            try
            {
                if (module == "Announcement")
                {
                    mvContent.SetActiveView(viewAnnouncement);
                    LoadAnnouncementGrids();
                    LoadAnnoPreview();
                }
                else
                {
                    mvContent.SetActiveView(module == "Home" ? viewHome : viewAbout);
                    LoadGenericGrids(module);

                    if (module == "Home")
                    {
                        LoadHomeSliderPreview();
                        hfContentID.Value = "";
                        hfCurrentImg.Value = "";
                        txtHomeTitle.Text = "";
                        txtHomeSubtitle.Text = "";
                        txtHomeContent.Text = "";
                    }
                    else
                    {
                        LoadAboutPreview();
                        if (DropDownList1.SelectedValue == "") DropDownList1.SelectedValue = "-1";
                        if (DropDownList1.SelectedValue == "-1") mvAboutUs.ActiveViewIndex = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error switching modules: " + ex.Message, true);
            }
        }

        protected void ddlAboutSection_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedVal = DropDownList1.SelectedValue;
            int index = Convert.ToInt32(selectedVal);

            mvAboutUs.ActiveViewIndex = index;

            hfContentID.Value = "";
            hfCurrentImg.Value = "";

            switch (selectedVal)
            {
                case "1":
                    txtStoryContent.Text = "";
                    break;
                case "2":
                    txtStoryMiVis.Text = "";
                    break;
                case "3":
                    txtMemberFirstName.Text = "";
                    txtMemberLastName.Text = "";
                    txtMemberRole.Text = "";
                    break;
            }
        }

        protected void ddlAnnouncement_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedVal = DropDownList2.SelectedValue;
            int index = Convert.ToInt32(selectedVal);

            viewAnnoForm.ActiveViewIndex = index;

            try
            {
                if (selectedVal == "0")
                {
                    LoadHeroFromAnnouncementTable();
                }
                else if (selectedVal == "1")
                {
                    hfAnnouncementID.Value = "";
                    txtAnnoTitle.Text = "";
                    txtAnnoDesc.Text = "";
                    txtAnnoOrder.Text = "1";
                    txtDateUntil.Text = "";
                    hfCurrentImg.Value = "";
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error loading form: " + ex.Message, true);
            }
        }

        public void LoadHeroFromAnnouncementTable()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT AnnouncementID, FilePath FROM tblAnnouncement WHERE Title = 'anno-hero'";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        hfAnnouncementID.Value = dr["AnnouncementID"].ToString();
                        hfCurrentImg.Value = dr["FilePath"].ToString();

                        if (PreAnnoHeroImg != null) PreAnnoHeroImg.ImageUrl = GetBase64Image(dr["FilePath"]);

                    }
                    else
                    {
                        hfAnnouncementID.Value = "";
                        hfCurrentImg.Value = "";
                        if (PreAnnoHeroImg != null) PreAnnoHeroImg.ImageUrl = string.Empty;

                        lblMessage.Text = "No existing Hero found in Feed. Ready for new entry.";
                        lblMessage.ForeColor = Color.Blue;
                    }
                }
            }
        }

        /* ================= PREVIEW DATA LOADERS ================= */
        private void LoadHomeSliderPreview()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM tblContent WHERE Section = 'Hero-Home' ORDER BY ContentID ASC";
                SqlDataAdapter sda = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                sda.Fill(dt);

                if (rptminiheroslider != null)
                {
                    rptminiheroslider.DataSource = dt;
                    rptminiheroslider.DataBind();
                }

                if (minirptIndicator != null)
                {
                    minirptIndicator.DataSource = dt;
                    minirptIndicator.DataBind();
                }
            }
        }

        private void LoadAboutPreview()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM tblContent WHERE Section LIKE '%About-Us%' AND Section != 'Members-About-Us' ORDER BY ContentID ASC";
                SqlDataAdapter sda = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                sda.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    string section = row["Section"].ToString().ToLower().Trim();
                    string content = row["Content"]?.ToString() ?? "";
                    string imgPath = row["ImgPath"]?.ToString() ?? "";
                    string title = row["Title"]?.ToString() ?? "";

                    if (section.Contains("hero"))
                    {
                        if (litAboutTitle != null) litAboutTitle.Text = string.IsNullOrEmpty(title) ? "ABOUT US" : title;
                        if (imgAboutPreview != null && !string.IsNullOrEmpty(imgPath)) imgAboutPreview.ImageUrl = GetBase64Image(imgPath);
                    }
                    else if (section.Contains("story"))
                    {
                        if (litAboutContent != null) litAboutContent.Text = content;
                        if (imgStoryPreview != null && !string.IsNullOrEmpty(imgPath)) imgStoryPreview.ImageUrl = GetBase64Image(imgPath);
                    }
                    else if (section.Contains("mission-vision"))
                    {
                        string[] miVis = content.Split(';');
                        if (miVis.Length > 0 && litMissionText != null) litMissionText.Text = miVis[0];
                        if (miVis.Length > 1 && litVisionText != null) litVisionText.Text = miVis[1];
                    }
                }

                string memQuery = "SELECT (Firstname + ' ' + Lastname) as Title, Position as Subtitle, ImgPath FROM tblAboutUsMembers";
                SqlDataAdapter sdaMem = new SqlDataAdapter(memQuery, con);
                DataTable dtMembers = new DataTable();
                sdaMem.Fill(dtMembers);

                if (rptAboutStack != null)
                {
                    rptAboutStack.DataSource = dtMembers;
                    rptAboutStack.DataBind();
                }
            }
        }

        private void LoadAnnoPreview()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"SELECT Title, [Content], CreatedAt, FilePath, URL_FB 
                                 FROM tblAnnouncement 
                                 WHERE ViewStatus = 1 AND Title != 'anno-hero' 
                                 ORDER BY DisplayOrder ASC, CreatedAt DESC";

                SqlDataAdapter sda = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                sda.Fill(dt);
                if (rptAnnoZigzag != null)
                {
                    rptAnnoZigzag.DataSource = dt;
                    rptAnnoZigzag.DataBind();
                }

                string heroQuery = "SELECT FilePath FROM tblAnnouncement WHERE Title = 'anno-hero'";
                SqlCommand heroCmd = new SqlCommand(heroQuery, con);
                con.Open();
                object heroPath = heroCmd.ExecuteScalar();
                if (heroPath != null && heroPath != DBNull.Value && PreAnnoHeroImg != null)
                {
                    PreAnnoHeroImg.ImageUrl = GetBase64Image(heroPath);
                }
                con.Close();
            }
        }

        protected string GetBase64Image(object filePathStr)
        {
            if (filePathStr != DBNull.Value && !string.IsNullOrEmpty(filePathStr.ToString()))
            {
                string path = filePathStr.ToString();
                return ResolveUrl(path);
            }
            return "https://via.placeholder.com/60";
        }

        /* ================= ANNOUNCEMENT LOGIC ================= */
        private void LoadAnnouncementGrids()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string pubSql = "SELECT AnnouncementID, Title, CreatedAt, ViewStatus, DisplayOrder, EndDate FROM tblAnnouncement WHERE ViewStatus = 1 ORDER BY DisplayOrder ASC, CreatedAt DESC";
                SqlDataAdapter pubSda = new SqlDataAdapter(pubSql, con);
                DataTable pubDt = new DataTable();
                pubSda.Fill(pubDt);
                gvPublished.DataSource = pubDt;
                gvPublished.DataBind();

                string draftSql = "SELECT AnnouncementID, Title, CreatedAt, ViewStatus, DisplayOrder FROM tblAnnouncement WHERE ViewStatus = 0 ORDER BY CreatedAt DESC";
                SqlDataAdapter draftSda = new SqlDataAdapter(draftSql, con);
                DataTable draftDt = new DataTable();
                draftSda.Fill(draftDt);
                gvDrafts.DataSource = draftDt;
                gvDrafts.DataBind();
            }
        }

        protected void btnPublish_Click(object sender, EventArgs e)
        {
            if (DropDownList2.SelectedValue == "0") SaveHeroAnno();
            else SaveAnnouncement(true);
        }

        protected void btnDraft_Click(object sender, EventArgs e)
        {
            if (DropDownList2.SelectedValue == "0") SaveHeroAnno();
            else SaveAnnouncement(false);
        }

        private void SaveHeroAnno()
        {
            if (string.IsNullOrEmpty(hfAnnouncementID.Value) && !fileAnnoHeroImg.HasFile)
            {
                ShowMessage("An image is required to create a new Hero Announcement.", true);
                return;
            }

            try
            {
                string title = "anno-hero";
                string content = "Hero Banner Image";
                string imgPath = UploadImageAndGetPath(fileAnnoHeroImg, hfCurrentImg.Value);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    int currentStaffID = Session["StaffID"] != null ? Convert.ToInt32(Session["StaffID"]) : 1;

                    string sql = string.IsNullOrEmpty(hfAnnouncementID.Value)
                        ? "INSERT INTO tblAnnouncement (Title, Content, FilePath, ViewStatus, DisplayOrder, CreatedAt, CreatedByStaffID) VALUES (@Title, @Content, @FilePath, 1, 0, GETDATE(), @StaffID)"
                        : "UPDATE tblAnnouncement SET FilePath=@FilePath WHERE AnnouncementID=@ID";

                    SqlCommand cmd = new SqlCommand(sql, con);

                    if (!string.IsNullOrEmpty(hfAnnouncementID.Value))
                    {
                        cmd.Parameters.AddWithValue("@ID", hfAnnouncementID.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@Title", title);
                        cmd.Parameters.AddWithValue("@Content", content);
                        cmd.Parameters.AddWithValue("@StaffID", currentStaffID);
                    }

                    cmd.Parameters.AddWithValue("@FilePath", string.IsNullOrEmpty(imgPath) ? (object)DBNull.Value : imgPath);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                RedirectWithState("saved");
            }
            catch (Exception ex)
            {
                ShowMessage("Error saving Hero Announcement: " + ex.Message, true);
            }
        }

        private void SaveAnnouncement(bool isPublished)
        {
            // ================= FORM VALIDATION =================
            if (string.IsNullOrWhiteSpace(txtAnnoTitle.Text))
            {
                ShowMessage("Announcement Title is required.", true);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtAnnoDesc.Text))
            {
                ShowMessage("Announcement Description is required.", true);
                return;
            }
            // ---> NEW VALIDATION ADDED HERE <---
            if (string.IsNullOrWhiteSpace(txtFbUrl.Text))
            {
                ShowMessage("Announcement URL is required.", true);
                return;
            }
            if (string.IsNullOrEmpty(hfAnnouncementID.Value) && !fileAnnoImage.HasFile)
            {
                ShowMessage("An image is required for a new Announcement.", true);
                return;
            }

            DateTime parsedDate;
            if (!string.IsNullOrEmpty(txtDateUntil.Text) && !DateTime.TryParse(txtDateUntil.Text, out parsedDate))
            {
                ShowMessage("Invalid 'Date Until' format.", true);
                return;
            }

            // ================= DATABASE SAVING =================
            try
            {
                string imgPath = UploadImageAndGetPath(fileAnnoImage, hfCurrentImg.Value);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    int currentStaffID = Session["StaffID"] != null ? Convert.ToInt32(Session["StaffID"]) : 1;

                    string sql = string.IsNullOrEmpty(hfAnnouncementID.Value)
                        ? "INSERT INTO tblAnnouncement (Title, Content, FilePath, ViewStatus, DisplayOrder, EndDate, URL_FB, CreatedAt, CreatedByStaffID) VALUES (@Title, @Content, @FilePath, @Status, @Order, @EndDate, @URLFB, GETDATE(), @StaffID)"
                        : "UPDATE tblAnnouncement SET Title=@Title, Content=@Content, FilePath=@FilePath, ViewStatus=@Status, DisplayOrder=@Order, EndDate=@EndDate, URL_FB=@URLFB WHERE AnnouncementID=@ID";

                    SqlCommand cmd = new SqlCommand(sql, con);
                    if (!string.IsNullOrEmpty(hfAnnouncementID.Value))
                    {
                        cmd.Parameters.AddWithValue("@ID", hfAnnouncementID.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@StaffID", currentStaffID);
                    }

                    cmd.Parameters.AddWithValue("@Title", txtAnnoTitle.Text.Trim());
                    cmd.Parameters.AddWithValue("@Content", txtAnnoDesc.Text.Trim());
                    cmd.Parameters.AddWithValue("@FilePath", string.IsNullOrEmpty(imgPath) ? (object)DBNull.Value : imgPath);
                    cmd.Parameters.AddWithValue("@Status", isPublished);
                    cmd.Parameters.AddWithValue("@Order", string.IsNullOrEmpty(txtAnnoOrder.Text) ? 1 : Convert.ToInt32(txtAnnoOrder.Text));
                    cmd.Parameters.AddWithValue("@URLFB", txtFbUrl.Text.Trim());

                    if (string.IsNullOrEmpty(txtDateUntil.Text)) cmd.Parameters.AddWithValue("@EndDate", DBNull.Value);
                    else cmd.Parameters.AddWithValue("@EndDate", Convert.ToDateTime(txtDateUntil.Text));

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                RedirectWithState("saved");
            }
            catch (Exception ex)
            {
                ShowMessage("Error saving Announcement: " + ex.Message, true);
            }
        }

        private void LoadAnnouncementForEdit(int id)
        {
            hfAnnouncementID.Value = id.ToString();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = "SELECT Title, Content, FilePath, ViewStatus, DisplayOrder, EndDate, URL_FB FROM tblAnnouncement WHERE AnnouncementID = @ID";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@ID", id);
                    con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            txtAnnoTitle.Text = dr["Title"].ToString();
                            txtAnnoDesc.Text = dr["Content"].ToString();
                            txtFbUrl.Text = dr["URL_FB"].ToString();
                            hfCurrentImg.Value = dr["FilePath"].ToString();
                            string title = dr["Title"].ToString();

                            if (title == "anno-hero")
                            {
                                DropDownList2.SelectedValue = "0";
                                viewAnnoForm.ActiveViewIndex = 0;
                                if (PreAnnoHeroImg != null) PreAnnoHeroImg.ImageUrl = GetBase64Image(dr["FilePath"]);
                            }
                            else
                            {
                                DropDownList2.SelectedValue = "1";
                                viewAnnoForm.ActiveViewIndex = 1;

                                txtAnnoTitle.Text = title;
                                txtAnnoDesc.Text = dr["Content"].ToString();
                                txtAnnoOrder.Text = dr["DisplayOrder"].ToString();

                                if (dr["EndDate"] != DBNull.Value)
                                    txtDateUntil.Text = Convert.ToDateTime(dr["EndDate"]).ToString("yyyy-MM-dd");
                                else txtDateUntil.Text = string.Empty;
                            }
                            ShowMessage("Data loaded for editing. Make changes and hit save.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error loading Announcement for edit: " + ex.Message, true);
            }
        }

        protected void gvPublished_SelectedIndexChanged(object sender, EventArgs e) { LoadAnnouncementForEdit(Convert.ToInt32(gvPublished.SelectedDataKey.Value)); }
        protected void gvDrafts_SelectedIndexChanged(object sender, EventArgs e) { LoadAnnouncementForEdit(Convert.ToInt32(gvDrafts.SelectedDataKey.Value)); }

        protected void gvAnnouncement_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            GridView grid = (GridView)sender;
            int id = Convert.ToInt32(grid.DataKeys[e.RowIndex].Value);

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand("DELETE FROM tblAnnouncement WHERE AnnouncementID=@ID", con);
                    cmd.Parameters.AddWithValue("@ID", id);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                // Redirect to prevent resubmission
                RedirectWithState("deleted");
            }
            catch (Exception ex)
            {
                ShowMessage("Error deleting Announcement: " + ex.Message, true);
            }
        }

        /* ================= GENERIC CONTENT LOGIC ================= */
        private void LoadGenericGrids(string module)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "";

                if (module == "Home")
                {
                    query = "SELECT ContentID, Section, Title, Subtitle FROM tblContent WHERE Section = 'Hero-Home'";
                }
                else
                {
                    query = @"
                        SELECT ContentID, Section, Title, Subtitle 
                        FROM tblContent 
                        WHERE Section IN ('Hero-About-Us', 'Our-Story-About-Us', 'Mission-Vision-About-Us')
                        UNION ALL
                        SELECT MemberID as ContentID, 'Members-About-Us' as Section, (Firstname + ' ' + Lastname) as Title, Position as Subtitle 
                        FROM tblAboutUsMembers";
                }

                SqlDataAdapter sda = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                sda.Fill(dt);
                gvGenericPublished.DataSource = dt;
                gvGenericPublished.DataBind();
            }
        }

        private void LoadGenericData(string sectionKey, string contentId = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = "";

                    if (sectionKey == "Members-About-Us")
                    {
                        query = "SELECT MemberID as ContentID, Firstname, Lastname, Position as Subtitle, '' as Content, ImgPath FROM tblAboutUsMembers WHERE 1=1";
                    }
                    else
                    {
                        query = "SELECT ContentID, Title, Subtitle, Content, ImgPath FROM tblContent WHERE LTRIM(RTRIM(Section)) = @sec";
                    }

                    if (!string.IsNullOrEmpty(contentId))
                    {
                        query += (sectionKey == "Members-About-Us") ? " AND MemberID = @id" : " AND ContentID = @id";
                    }

                    SqlCommand cmd = new SqlCommand(query, con);

                    if (sectionKey != "Members-About-Us")
                    {
                        cmd.Parameters.AddWithValue("@sec", sectionKey);
                    }
                    if (!string.IsNullOrEmpty(contentId))
                    {
                        cmd.Parameters.AddWithValue("@id", contentId);
                    }

                    con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            hfContentID.Value = dr["ContentID"].ToString();
                            hfCurrentImg.Value = dr["ImgPath"].ToString();

                            string dbTitle = dr["Title"].ToString();
                            string dbSubtitle = dr["Subtitle"].ToString();
                            string dbContent = dr["Content"].ToString();

                            if (ddlModule.SelectedValue == "Home")
                            {
                                txtHomeTitle.Text = dbTitle;
                                txtHomeSubtitle.Text = dbSubtitle;
                                txtHomeContent.Text = dbContent;
                            }
                            else
                            {
                                if (sectionKey == "Hero-About-Us")
                                {
                                    if (litAboutTitle != null) litAboutTitle.Text = dbTitle;
                                    if (imgAboutPreview != null) imgAboutPreview.ImageUrl = GetBase64Image(dr["ImgPath"]);
                                }
                                else if (sectionKey == "Our-Story-About-Us")
                                {
                                    txtStoryContent.Text = dbContent;
                                    if (litAboutContent != null) litAboutContent.Text = dbContent;
                                    if (imgStoryPreview != null) imgStoryPreview.ImageUrl = GetBase64Image(dr["ImgPath"]);
                                }
                                else if (sectionKey == "Mission-Vision-About-Us")
                                {
                                    txtStoryMiVis.Text = dbContent;
                                    string[] miVis = dbContent.Split(';');
                                    if (miVis.Length > 0 && litMissionText != null) litMissionText.Text = miVis[0];
                                    if (miVis.Length > 1 && litVisionText != null) litVisionText.Text = miVis[1];
                                }
                                else if (sectionKey == "Members-About-Us")
                                {
                                    txtMemberFirstName.Text = dr["Firstname"].ToString();
                                    txtMemberLastName.Text = dr["Lastname"].ToString();
                                    txtMemberRole.Text = dr["Subtitle"].ToString();

                                    if (litMemberName != null) litMemberName.Text = (dr["Firstname"].ToString() + " " + dr["Lastname"].ToString()).Trim();
                                    if (litMemberRole != null) litMemberRole.Text = dr["Subtitle"].ToString();
                                    if (imgMemberPreview != null) imgMemberPreview.ImageUrl = GetBase64Image(dr["ImgPath"]);
                                }
                            }

                            ShowMessage("Data loaded for " + sectionKey);
                        }
                        else
                        {
                            hfContentID.Value = "";
                            ClearSectionSpecificFields(sectionKey);

                            lblMessage.Text = "No existing data found for " + sectionKey + ". Ready for new entry.";
                            lblMessage.ForeColor = Color.Blue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error loading data: " + ex.Message, true);
            }
        }

        private void ClearSectionSpecificFields(string sectionKey)
        {
            if (sectionKey == "Hero-About-Us")
            {
                if (litAboutTitle != null) litAboutTitle.Text = "ABOUT US";
                if (imgAboutPreview != null) imgAboutPreview.ImageUrl = string.Empty;
            }
            else if (sectionKey == "Our-Story-About-Us")
            {
                txtStoryContent.Text = string.Empty;
                if (litAboutContent != null) litAboutContent.Text = "Content will display here.";
                if (imgStoryPreview != null) imgStoryPreview.ImageUrl = string.Empty;
            }
            else if (sectionKey == "Mission-Vision-About-Us")
            {
                txtStoryMiVis.Text = string.Empty;
                if (litMissionText != null) litMissionText.Text = "To encourage physical fitness...";
                if (litVisionText != null) litVisionText.Text = "To be the premier destination...";
            }
            else if (sectionKey == "Members-About-Us")
            {
                txtMemberFirstName.Text = string.Empty;
                txtMemberLastName.Text = string.Empty;
                txtMemberRole.Text = string.Empty;
                if (litMemberName != null) litMemberName.Text = "Name";
                if (litMemberRole != null) litMemberRole.Text = "Role";
                if (imgMemberPreview != null) imgMemberPreview.ImageUrl = string.Empty;
            }
        }

        protected void gvGenericPublished_SelectedIndexChanged(object sender, EventArgs e)
        {
            int id = Convert.ToInt32(gvGenericPublished.SelectedDataKey.Value);
            string section = gvGenericPublished.SelectedRow.Cells[0].Text;

            if (ddlModule.SelectedValue == "AboutUs")
            {
                switch (section)
                {
                    case "Hero-About-Us": DropDownList1.SelectedValue = "0"; break;
                    case "Our-Story-About-Us": DropDownList1.SelectedValue = "1"; break;
                    case "Mission-Vision-About-Us": DropDownList1.SelectedValue = "2"; break;
                    case "Members-About-Us": DropDownList1.SelectedValue = "3"; break;
                    default: DropDownList1.SelectedValue = "-1"; break;
                }
                mvAboutUs.ActiveViewIndex = Convert.ToInt32(DropDownList1.SelectedValue);
            }

            LoadGenericData(section, id.ToString());
        }

        protected void btnSaveGeneric_Click(object sender, EventArgs e)
        {
            if (ddlModule == null) return;

            bool isHome = ddlModule.SelectedValue == "Home";
            string section = "";
            string title = "", subtitle = "", content = "", fname = "", lname = "";
            FileUpload fu = null;

            if (isHome)
            {
                if (string.IsNullOrWhiteSpace(txtHomeTitle.Text) || string.IsNullOrWhiteSpace(txtHomeSubtitle.Text) || string.IsNullOrWhiteSpace(txtHomeContent.Text))
                {
                    ShowMessage("Title, Subtitle, and Content are all required for the Home Page.", true);
                    return;
                }
                if (string.IsNullOrEmpty(hfContentID.Value) && !fileHomeImage.HasFile)
                {
                    ShowMessage("An image is required for a new Home Page record.", true);
                    return;
                }

                section = "Hero-Home";
                title = txtHomeTitle.Text;
                subtitle = txtHomeSubtitle.Text;
                content = txtHomeContent.Text;
                fu = fileHomeImage;
            }
            else
            {
                if (DropDownList1 == null || DropDownList1.SelectedValue == "-1")
                {
                    ShowMessage("Please select an About Us section first.", true);
                    return;
                }

                switch (DropDownList1.SelectedValue)
                {
                    case "0": // Hero About Us
                        if (string.IsNullOrEmpty(hfContentID.Value) && !fileAboutHeroImg.HasFile)
                        {
                            ShowMessage("An image is required for the Hero section.", true);
                            return;
                        }
                        section = "Hero-About-Us";
                        fu = fileAboutHeroImg;
                        break;
                    case "1": // Our Story
                        if (string.IsNullOrWhiteSpace(txtStoryContent.Text))
                        {
                            ShowMessage("Story content is required.", true);
                            return;
                        }
                        if (string.IsNullOrEmpty(hfContentID.Value) && !FileUpload1.HasFile)
                        {
                            ShowMessage("An image is required for Our Story.", true);
                            return;
                        }
                        section = "Our-Story-About-Us";
                        content = txtStoryContent.Text;
                        fu = FileUpload1;
                        break;
                    case "2": // Mission/Vision
                        if (string.IsNullOrWhiteSpace(txtStoryMiVis.Text) || !txtStoryMiVis.Text.Contains(";"))
                        {
                            ShowMessage("Mission & Vision is required and MUST contain a semicolon (;)", true);
                            return;
                        }
                        section = "Mission-Vision-About-Us";
                        content = txtStoryMiVis.Text;
                        break;
                    case "3": // Members
                        if (string.IsNullOrWhiteSpace(txtMemberFirstName.Text) || string.IsNullOrWhiteSpace(txtMemberLastName.Text) || string.IsNullOrWhiteSpace(txtMemberRole.Text))
                        {
                            ShowMessage("First Name, Last Name, and Role are required.", true);
                            return;
                        }
                        if (string.IsNullOrEmpty(hfContentID.Value) && !MemberPhoto.HasFile)
                        {
                            ShowMessage("A photo is required for new Members.", true);
                            return;
                        }
                        section = "Members-About-Us";
                        fname = txtMemberFirstName.Text.Trim();
                        lname = txtMemberLastName.Text.Trim();
                        subtitle = txtMemberRole.Text.Trim();
                        fu = MemberPhoto;
                        break;
                }
            }

            try
            {
                string imgPath = UploadImageAndGetPath(fu, hfCurrentImg?.Value ?? "");

                // FETCH LOGGED IN STAFF ID TO FIX FOREIGN KEY ERROR
                int currentStaffID = Session["StaffID"] != null ? Convert.ToInt32(Session["StaffID"]) : 1;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string sql = "";
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;

                    if (section == "Members-About-Us")
                    {
                        if (string.IsNullOrEmpty(hfContentID.Value))
                        {
                            sql = "INSERT INTO tblAboutUsMembers (Firstname, Lastname, Position, ImgPath) VALUES (@fname, @lname, @s, @img)";
                        }
                        else
                        {
                            sql = "UPDATE tblAboutUsMembers SET Firstname=@fname, Lastname=@lname, Position=@s, ImgPath=@img WHERE MemberID=@id";
                            cmd.Parameters.AddWithValue("@id", hfContentID.Value);
                        }
                        cmd.Parameters.AddWithValue("@fname", fname);
                        cmd.Parameters.AddWithValue("@lname", lname);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(hfContentID.Value))
                        {
                            // FIXED HERE: Replaced '1' with '@StaffID'
                            sql = "INSERT INTO tblContent (Section, Title, Subtitle, Content, ImgPath, LastModifiedByStaffID) VALUES (@sec, @t, @s, @c, @img, @StaffID)";
                        }
                        else
                        {
                            // Update existing record and also update the LastModified user tracker
                            sql = "UPDATE tblContent SET Title=@t, Subtitle=@s, Content=@c, ImgPath=@img, LastModifiedByStaffID=@StaffID WHERE ContentID=@id";
                            cmd.Parameters.AddWithValue("@id", hfContentID.Value);
                        }
                        cmd.Parameters.AddWithValue("@sec", section);
                        cmd.Parameters.AddWithValue("@c", content);

                        // BIND THE STAFF ID PARAMETER
                        cmd.Parameters.AddWithValue("@StaffID", currentStaffID);
                    }

                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@t", title);
                    cmd.Parameters.AddWithValue("@s", string.IsNullOrEmpty(subtitle) ? (object)DBNull.Value : subtitle);
                    cmd.Parameters.AddWithValue("@img", string.IsNullOrEmpty(imgPath) ? (object)DBNull.Value : imgPath);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                // Redirect to prevent resubmission
                RedirectWithState("saved");
            }
            catch (Exception ex)
            {
                ShowMessage("Error saving data: " + ex.Message, true);
            }
        }

        protected void gvGenericPublished_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int id = Convert.ToInt32(gvGenericPublished.DataKeys[e.RowIndex].Value);
            string section = gvGenericPublished.Rows[e.RowIndex].Cells[0].Text;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;

                    if (section == "Members-About-Us")
                    {
                        cmd.CommandText = "DELETE FROM tblAboutUsMembers WHERE MemberID=@id";
                    }
                    else
                    {
                        cmd.CommandText = "DELETE FROM tblContent WHERE ContentID=@id";
                    }

                    cmd.Parameters.AddWithValue("@id", id);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                RedirectWithState("deleted");
            }
            catch (Exception ex)
            {
                ShowMessage("Error deleting record: " + ex.Message, true);
            }
        }

        private void ClearFields()
        {
            hfContentID.Value = "";
            hfAnnouncementID.Value = "";
            hfCurrentImg.Value = "";
            hfAsyncImageBase64.Value = "";

            txtHomeTitle.Text = string.Empty;
            txtHomeSubtitle.Text = string.Empty;
            txtHomeContent.Text = string.Empty;

            txtStoryContent.Text = string.Empty;
            txtStoryMiVis.Text = string.Empty;

            txtMemberFirstName.Text = string.Empty;
            txtMemberLastName.Text = string.Empty;
            txtMemberRole.Text = string.Empty;

            txtAnnoTitle.Text = string.Empty;
            txtAnnoDesc.Text = string.Empty;
            txtAnnoOrder.Text = "1";
            txtDateUntil.Text = string.Empty;

            lblMessage.Text = "Form cleared. Ready to add a new entry.";
            lblMessage.ForeColor = Color.Blue;
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            string module = ddlModule.SelectedValue;
            string url = $"{Request.Url.AbsolutePath}?module={module}";
            Response.Redirect(url, false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}