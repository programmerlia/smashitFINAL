using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Smash_IT
{


    public partial class Site1 : System.Web.UI.MasterPage
    {

        public string UserImgUrl { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            // default (also used when logged out)
            UserImgUrl = ResolveUrl("~/images/person.png");

            if (Session["UserID"] == null) return;

            int userId = Convert.ToInt32(Session["UserID"]);
            var cs = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

            using (var con = new SqlConnection(cs))
            using (var cmd = new SqlCommand("SELECT ImgPath FROM tblPlayerAccount WHERE UserID=@ID", con))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                con.Open();

                var v = cmd.ExecuteScalar();
                var imgPath = (v == DBNull.Value || v == null) ? "" : v.ToString().Trim();

                if (!string.IsNullOrWhiteSpace(imgPath))
                {
                    if (imgPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        UserImgUrl = imgPath;
                    else
                        UserImgUrl = ResolveUrl("~/" + imgPath.TrimStart('~', '/'));
                }
            }
        }


    }
}