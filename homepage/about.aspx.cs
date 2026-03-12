using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Smash_IT.homepage
{
    public partial class abouut : System.Web.UI.Page
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadAboutContent();
                LoadMembers();
            }
        }

        private void LoadAboutContent()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT Section, Title, Content, ImgPath FROM tblContent WHERE Section IN ('Hero-About-Us', 'Our-Story-About-Us', 'Mission-Vision-About-Us')";
                SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string section = dr["Section"].ToString();
                        string title = dr["Title"].ToString();
                        string content = dr["Content"].ToString();
                        string imgPath = dr["ImgPath"].ToString();

                        if (section == "Hero-About-Us")
                        {
                            litHeroTitle.Text = !string.IsNullOrWhiteSpace(title) ? title : "About Us";

                            if (!string.IsNullOrEmpty(imgPath))
                            {
                                if (imgPath.StartsWith("data:image"))
                                {
                                    imgHero.ImageUrl = imgPath;
                                }
                                else
                                {
                                    imgHero.ImageUrl = imgPath;
                                }
                            }
                            else
                            {
                                imgHero.ImageUrl = ResolveUrl("~/images/default-hero.jpg");
                            }
                        }
                        else if (section == "Our-Story-About-Us")
                        {
                            litStoryContent.Text = content;
                            if (!string.IsNullOrEmpty(imgPath)) imgStory.ImageUrl = imgPath;
                        }
                        else if (section == "Mission-Vision-About-Us")
                        {
                            string[] splitContent = content.Split(';');

                            litMission.Text = splitContent.Length > 0 ? splitContent[0].Trim() : "";
                            litVision.Text = splitContent.Length > 1 ? splitContent[1].Trim() : "";
                        }
                    }
                }
            }
        }

        private void LoadMembers()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT (Firstname + ' ' + Lastname) AS Title, Position AS Subtitle, ImgPath FROM tblAboutUsMembers ORDER BY MemberID ASC";
                SqlDataAdapter sda = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                sda.Fill(dt);

                rptMembers.DataSource = dt;
                rptMembers.DataBind();
            }
        }

        protected string GetBase64Image(object filePathStr)
        {
            if (filePathStr != DBNull.Value && !string.IsNullOrEmpty(filePathStr.ToString()))
            {
                return ResolveUrl(filePathStr.ToString());
            }
            return ResolveUrl("~/images/placeholder.jpg");
        }
    }
}