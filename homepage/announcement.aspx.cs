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
                LoadPublishedAnnouncements();
            }
        }

        private void LoadPublishedAnnouncements()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = @"SELECT Title, [Content], CreatedAt, ImageFIleData, ImageFileType 
                                     FROM tblAnnouncement 
                                     WHERE ViewStatus = 1 
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
                // Log error or show a friendly message
                System.Diagnostics.Debug.WriteLine("Error loading announcements: " + ex.Message);
            }
        }

        public string GetBase64Image(object imageData, object imageType)
        {
            if (imageData != null && imageData != DBNull.Value && !string.IsNullOrEmpty(imageType?.ToString()))
            {
                byte[] bytes = (byte[])imageData;
                return $"data:{imageType};base64,{Convert.ToBase64String(bytes)}";
            }

            // Modern placeholder service
            return "https://images.unsplash.com/photo-1506784365847-bbad939e9335?q=80&w=1000&auto=format&fit=crop";
        }
    }
}