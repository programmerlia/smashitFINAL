using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace Smash_IT.homepage
{
    public partial class announcement : System.Web.UI.Page
    {
        readonly string connectionString = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadHeroImage();
                LoadPublishedAnnouncements();
            }
        }

        private void LoadHeroImage()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = "SELECT FilePath FROM tblAnnouncement WHERE Title = 'anno-hero'";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        con.Open();
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value && !string.IsNullOrEmpty(result.ToString()))
                        {
                            AnnoImgHero.ImageUrl = result.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading hero image: " + ex.Message);
            }
        }

        private void LoadPublishedAnnouncements()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = @"SELECT Title, Content, CreatedAt, FilePath, URL_FB 
                                     FROM tblAnnouncement 
                                     WHERE ViewStatus = 1 AND Title != 'anno-hero' 
                                     ORDER BY DisplayOrder ASC, CreatedAt DESC";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        con.Open();
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            sda.Fill(dt);

                            rptAnnouncements.DataSource = dt;
                            rptAnnouncements.DataBind();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading announcements: " + ex.Message);
            }
        }

        protected string ResolveImagePath(object filePathStr)
        {
            if (filePathStr != null && filePathStr != DBNull.Value && !string.IsNullOrEmpty(filePathStr.ToString()))
            {
                string path = filePathStr.ToString();
                return ResolveUrl(path);
            }
            return ResolveUrl("~/images/placeholder.jpg");
        }

        protected string GetValidUrl(object urlObj)
        {
            if (urlObj == null || urlObj == DBNull.Value || string.IsNullOrWhiteSpace(urlObj.ToString()))
            {
                return "#";
            }

            string url = urlObj.ToString().Trim();

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            return url;
        }
    }
}