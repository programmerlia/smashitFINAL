using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Smash_IT.homepage
{
    public partial class index : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindPromos();
            }
        }

        private void BindPromos()
        {
            string connStr = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connStr))
            {
                // Fetch TOP 5 published announcements for the slider
                string query = "SELECT TOP 5 AnnouncementID, Title, [Content], ImageFIleData, ImageFileType FROM tblAnnouncement WHERE ViewStatus = 1 ORDER BY DisplayOrder ASC, CreatedAt DESC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        rptPromos.DataSource = dt;
                        rptPromos.DataBind();
                    }
                }
            }
        }

        public string GetBase64Image(object imageData, object imageType)
        {
            if (imageData != DBNull.Value && imageType != DBNull.Value)
            {
                return "data:" + imageType.ToString() + ";base64," + Convert.ToBase64String((byte[])imageData);
            }
            return "https://placehold.co/500x300?text=No+Image";
        }
    }
}