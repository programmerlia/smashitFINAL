using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;

namespace Smash_IT.homepage
{
    public partial class index : System.Web.UI.Page
    {
        private string connStr = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadHeroSlider();
                BindPromos();
            }
        }

        private void BindPromos()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    string query = "SELECT TOP 5 AnnouncementID, Title, Content, FilePath, URL_FB FROM tblAnnouncement WHERE ViewStatus = 1 AND Title != 'anno-hero' ORDER BY DisplayOrder ASC, CreatedAt DESC";

                    SqlDataAdapter sda = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    sda.Fill(dt);

                    rptPromos.DataSource = dt;
                    rptPromos.DataBind();
                }

                LoadReserveSection();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading promos: " + ex.Message);
            }
        }

        private void LoadReserveSection()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    string query = "SELECT Title, Subtitle, Content, ImgPath FROM tblContent WHERE Section = 'Section-Reserve'";
                    SqlDataAdapter sda = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    sda.Fill(dt);

                    rptReserve.DataSource = dt;
                    rptReserve.DataBind();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading reserve section: " + ex.Message);
            }
        }

        private void LoadHeroSlider()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    string query = "SELECT Title, Subtitle, Content, ImgPath FROM tblContent WHERE Section = 'Hero-Home'";
                    SqlDataAdapter sda = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    sda.Fill(dt);

                    rptHeroSlider.DataSource = dt;
                    rptHeroSlider.DataBind();

                    rptIndicators.DataSource = dt;
                    rptIndicators.DataBind();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading hero slider: " + ex.Message);
            }
        }

        public string GetBase64Image(object filePathStr)
        {
            if (filePathStr != DBNull.Value && !string.IsNullOrEmpty(filePathStr.ToString()))
            {
                return ResolveUrl(filePathStr.ToString());
            }

            return "https://images.unsplash.com/photo-1628116999299-87cfa7cb6392?q=80&w=1000&auto=format&fit=crop";
        }
    }
}