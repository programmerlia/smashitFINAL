<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/adminpage/admin.Master" CodeBehind="Content_Manager.aspx.cs" Inherits="Smash_IT.adminpage.Content_Manager" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/admin_content_manager.css") %>' />

    <link rel="stylesheet" type="text/css" href="https://cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick.css" />
    <link rel="stylesheet" type="text/css" href="https://cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick-theme.css" />
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/slick-carousel@1.8.1/slick/slick.min.js"></script>

    <style>
        @media (max-width: 992px) {
            .dashboard-split {
                flex-direction: column;
            }

            .dashboard-sidebar {
                width: 100%;
                max-width: none;
            }
        }

        @media (max-width: 768px) {
            .form-grid {
                grid-template-columns: 1fr;
            }

            .mini-members-grid {
                grid-template-columns: repeat(2, 1fr);
            }

            .mini-story-grid, .mini-mission-grid {
                grid-template-columns: 1fr;
            }

            .dashboard-container, .table-container {
                padding: 15px;
            }
        }
        .table-responsive {
            overflow-x: auto;
            -webkit-overflow-scrolling: touch;
        }
    </style>

    <script type="text/javascript">
        // 1. Live Text Preview
        function updatePreview(targetId, text) {
            const targetElement = document.getElementById(targetId);
            if (targetElement) {
                targetElement.innerText = text ? text : "Content will display here.";
            }
        }

        // 2. Mission & Vision Splitter
        function updateMissionVision(val) {
            let parts = val.split(';');
            let missionTarget = document.getElementById('litMissionText');
            let visionTarget = document.getElementById('litVisionText');

            if (missionTarget) missionTarget.innerText = parts[0] ? parts[0].trim() : "To encourage physical fitness...";
            if (visionTarget) visionTarget.innerText = parts[1] ? parts[1].trim() : "To be the premier destination...";
        }

        // 3. Live Image Preview & Async Prep
        function previewImage(input, imgId) {
            if (input.files && input.files[0]) {
                var reader = new FileReader();
                reader.onload = function (e) {
                    const imgElement = document.getElementById(imgId);
                    if (imgElement) {
                        imgElement.src = e.target.result;
                    }

                    const hiddenField = document.getElementById('<%= hfAsyncImageBase64.ClientID %>');
                    if (hiddenField) {
                        hiddenField.value = e.target.result;
                    }
                };
                reader.readAsDataURL(input.files[0]);
            }
        }

        function updateMemberName() {
            var fname = document.getElementById('<%= txtMemberFirstName.ClientID %>').value;
            var lname = document.getElementById('<%= txtMemberLastName.ClientID %>').value;
            var target = document.getElementById('litMemberName');
            if (target) {
                target.innerText = (fname || lname) ? (fname + " " + lname).trim() : "Name";
            }
        }

        let slideInterval;

        function initSlider() {
            if (slideInterval) {
                clearInterval(slideInterval);
            }

            const wrapper = document.getElementById('minisliderwrapper');
            const dots = document.querySelectorAll('.mini-slider-indicators .dot');
            const totalSlides = dots.length;
            let currentSlide = 0;

            if (!wrapper || totalSlides === 0) return;

            function goToSlide(index) {
                currentSlide = index;
                wrapper.style.transform = `translateX(-${currentSlide * 100}%)`;

                dots.forEach(dot => dot.classList.remove('active'));
                if (dots[currentSlide]) {
                    dots[currentSlide].classList.add('active');
                }
            }

            dots.forEach((dot, index) => {
                dot.addEventListener('click', (event) => {
                    goToSlide(index);
                    resetTimer();
                });
            });

            function startTimer() {
                if (totalSlides <= 1) return;
                slideInterval = setInterval(() => {
                    let nextSlide = (currentSlide + 1) % totalSlides;
                    goToSlide(nextSlide);
                }, 5000);
            }

            function resetTimer() {
                clearInterval(slideInterval);
                startTimer();
            }

            startTimer();
        }

        function pageLoad() {
            initSlider();
        }
    </script>
</asp:Content>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" />


    <section class="admin-section container">

        <div class="dashboard-container">
            <h2>Global Content Manager</h2>
            <asp:Label ID="lblMessage" runat="server" CssClass="message"></asp:Label>

            <asp:HiddenField ID="hfContentID" runat="server" />
            <asp:HiddenField ID="hfAnnouncementID" runat="server" />
            <asp:HiddenField ID="hfCurrentImg" runat="server" />
            <asp:HiddenField ID="hfAsyncImageBase64" runat="server" />

            <div class="form-group" style="max-width: 400px;">
                <label>Navigate to Module:</label>
                <asp:DropDownList ID="ddlModule" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="ddlModule_SelectedIndexChanged">
                    <asp:ListItem Text="Announcement" Value="Announcement" />
                    <asp:ListItem Text="Home Page" Value="Home" />
                    <asp:ListItem Text="About Us Page" Value="AboutUs" />
                </asp:DropDownList>
            </div>
        </div>

        <div class="dashboard-split">
            <div class="dashboard-main">
                <asp:MultiView ID="mvContent" runat="server">

                    <asp:View ID="viewAnnouncement" runat="server">
                        <div class="dashboard-container">
                            <h3>Announcement Editor</h3>
                            <div class="form-grid">
                                <div class="left-col">
                                    <div class="form-group">
                                        <asp:DropDownList ID="DropDownList2" runat="server" CssClass="form-control"
                                            AutoPostBack="true" OnSelectedIndexChanged="ddlAnnouncement_SelectedIndexChanged">
                                            <asp:ListItem Text="-- Select Section --" Value="-1" />
                                            <asp:ListItem Text="Announcement Hero" Value="0" />
                                            <asp:ListItem Text="Announcement Content" Value="1" />
                                        </asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <asp:MultiView ID="viewAnnoForm" runat="server" ActiveViewIndex="-1">
                                <asp:View ID="viewAnnoHero" runat="server">
                                    <div class="form-grid">
                                        <div class="left-col">
                                            <div class="form-group">
                                                <label>Image Hero:</label>
                                                <asp:FileUpload ID="fileAnnoHeroImg" runat="server" onchange="previewImage(this, 'imgAnnoPreview')" />
                                            </div>
                                        </div>
                                    </div>
                                </asp:View>

                                <asp:View ID="viewAnnoContent" runat="server">
                                    <div class="form-grid">
                                        <div class="left-col">
                                            <div class="form-group">
                                                <label>Title:</label>
                                                <asp:TextBox ID="txtAnnoTitle" runat="server" CssClass="form-control" placeholder="Enter headline..."></asp:TextBox>
                                            </div>
                                            <div class="form-group">
                                                <label>Description:</label>
                                                <asp:TextBox ID="txtAnnoDesc" runat="server" TextMode="MultiLine" Rows="4" CssClass="form-control" placeholder="Enter details..."></asp:TextBox>
                                            </div>
                                            <div class="form-group">
                                                <label>Date Until (Optional):</label>
                                                <asp:TextBox ID="txtDateUntil" runat="server" TextMode="Date" CssClass="form-control"></asp:TextBox>
                                            </div>
                                        </div>

                                        <div class="right-col">
                                            <div class="form-group">
                                                <label>Image:</label>
                                                <asp:FileUpload ID="fileAnnoImage" runat="server" />
                                                <small style="color: #888;">Select an image to upload with this announcement.</small>
                                            </div>
                                            <div class="form-group">
                                                <label>Display Order:</label>
                                                <asp:TextBox ID="txtAnnoOrder" runat="server" TextMode="Number" CssClass="form-control" Text="1"></asp:TextBox>
                                            </div>
                                            <div class="form-group">
                                                <label>Announcement URL: <span style="color:red;">*</span></label>
                                                <asp:TextBox ID="txtFbUrl" runat="server" CssClass="form-control" placeholder="Enter Announcement URL..."></asp:TextBox>
                                            </div>
                                        </div>
                                    </div> 
                                </asp:View>

                            </asp:MultiView>
                            <div class="btn-container">
                                <asp:Button ID="btnPublish" runat="server" Text="Save Changes" CssClass="btn-publish" OnClick="btnPublish_Click" />
                                <asp:Button ID="btnDraft" runat="server" Text="Save Draft" CssClass="btn-draft" OnClick="btnDraft_Click" />
                                <asp:Button ID="btnClearAnno" runat="server" Text="Clear" CssClass="btn-draft" OnClick="btnClear_Click" />
                            </div>
                        </div>
                    </asp:View>

                    <asp:View ID="viewHome" runat="server">
                        <div class="dashboard-container">
                            <h3>Home Page Hero</h3>
                            <div class="form-grid">
                                <div class="left-col">
                                    <div class="form-group">
                                        <label>Title:</label>
                                        <asp:TextBox ID="txtHomeTitle" runat="server" CssClass="form-control" oninput="updatePreview('litHomeTitle', this.value)"></asp:TextBox>
                                    </div>
                                    <div class="form-group">
                                        <label>Subtitle:</label>
                                        <asp:TextBox ID="txtHomeSubtitle" runat="server" CssClass="form-control" oninput="updatePreview('litHomeSub', this.value)"></asp:TextBox>
                                    </div>
                                </div>
                                <div class="right-col">
                                    <div class="form-group">
                                        <label>Content:</label>
                                        <asp:TextBox ID="txtHomeContent" runat="server" TextMode="MultiLine" Rows="3" CssClass="form-control" oninput="updatePreview('litHomeContent', this.value)"></asp:TextBox>
                                    </div>
                                    <div class="form-group">
                                        <label>Image:</label>
                                        <asp:FileUpload ID="fileHomeImage" runat="server" onchange="previewImage(this, 'imgHomePreview')" />
                                    </div>
                                </div>
                            </div>

                            <div class="btn-container">
                                <asp:Button ID="btnSaveHome" runat="server" Text="Save Changes" CssClass="btn-publish" OnClick="btnSaveGeneric_Click" />
                                <asp:Button ID="btnClearHome" runat="server" Text="Clear" CssClass="btn-draft" OnClick="btnClear_Click" />
                            </div>
                        </div>
                    </asp:View>

                    <asp:View ID="viewAbout" runat="server">
                        <div class="dashboard-container">
                            <h3>About Us Sections</h3>
                            <div class="form-grid">
                                <div class="left-col">
                                    <div class="form-group">
                                        <label>Section:</label>
                                        <asp:DropDownList ID="DropDownList1" runat="server" CssClass="form-control"
                                            AutoPostBack="true" OnSelectedIndexChanged="ddlAboutSection_SelectedIndexChanged">
                                            <asp:ListItem Text="-- Select Section --" Value="-1" />
                                            <asp:ListItem Text="Hero (About Us)" Value="0" />
                                            <asp:ListItem Text="Our Story" Value="1" />
                                            <asp:ListItem Text="Mission & Vision" Value="2" />
                                            <asp:ListItem Text="Members" Value="3" />
                                        </asp:DropDownList>
                                    </div>
                                </div>
                            </div>

                            <asp:MultiView ID="mvAboutUs" runat="server" ActiveViewIndex="-1">
                                <asp:View ID="viewAboutHero" runat="server">
                                    <div class="form-grid">
                                        <div class="left-col">
                                            <div class="form-group">
                                                <label>Image Header:</label>
                                                <asp:FileUpload ID="fileAboutHeroImg" runat="server" onchange="previewImage(this, 'imgAboutPreview')" />
                                            </div>
                                        </div>
                                    </div>
                                </asp:View>

                                <asp:View ID="viewAboutStory" runat="server">
                                    <div class="form-grid">
                                        <div class="left-col">
                                            <div class="form-group">
                                                <label>Content:</label>
                                                <asp:TextBox ID="txtStoryContent" runat="server" TextMode="MultiLine" Rows="3" CssClass="form-control" onkeyup="updatePreview('litAboutContent', this.value)"></asp:TextBox>
                                            </div>
                                        </div>
                                        <div class="right-col">
                                            <div class="form-group">
                                                <label>Our Story Image:</label>
                                                <asp:FileUpload ID="FileUpload1" runat="server" onchange="previewImage(this, 'imgStoryPreview')" />
                                            </div>
                                        </div>
                                    </div>
                                </asp:View>

                                <asp:View ID="viewMission" runat="server">
                                    <div class="form-grid">
                                        <div class="left-col">
                                            <div class="form-group">
                                                <label>Mission & Vision:</label>
                                                <p style="font-size: 0.8rem; color: #666;">Note: Use ';' to separate (Mission;Vision)</p>
                                                <asp:TextBox ID="txtStoryMiVis" runat="server" TextMode="MultiLine" Rows="3" CssClass="form-control" onkeyup="updateMissionVision(this.value)"></asp:TextBox>
                                            </div>
                                        </div>
                                    </div>
                                </asp:View>

                                <asp:View ID="viewMembers" runat="server">
                                    <div class="form-grid">
                                        <div class="left-col">
                                            <div class="form-group">
                                                <label>First Name:</label>
                                                <asp:TextBox ID="txtMemberFirstName" runat="server" CssClass="form-control" onkeyup="updateMemberName()"></asp:TextBox>
                
                                                <label style="margin-top: 10px;">Last Name:</label>
                                                <asp:TextBox ID="txtMemberLastName" runat="server" CssClass="form-control" onkeyup="updateMemberName()"></asp:TextBox>
                
                                                <label style="margin-top: 10px;">Role/Position:</label>
                                                <asp:TextBox ID="txtMemberRole" runat="server" CssClass="form-control" onkeyup="updatePreview('litMemberRole', this.value)"></asp:TextBox>
                                            </div>
                                        </div>
                                        <div class="right-col">
                                            <div class="form-group">
                                                <label>Member Photo:</label>
                                                <asp:FileUpload ID="MemberPhoto" runat="server" onchange="previewImage(this, 'imgMemberPreview')" />
                                            </div>
                                        </div>
                                    </div>
                                </asp:View>

                            </asp:MultiView>

                            <div class="btn-container">
                                <asp:Button ID="Button1" runat="server" Text="Save Changes" CssClass="btn-publish" OnClick="btnSaveGeneric_Click" />
                                <asp:Button ID="Button2" runat="server" Text="Clear" CssClass="btn-draft" OnClick="btnClear_Click" />
                            </div>
                        </div>
                    </asp:View>

                </asp:MultiView>
            </div>

            <div class="dashboard-sidebar">
                <div class="preview-sticky-container">
                    <h3 class="preview-title">Live Preview <i class="fas fa-desktop"></i></h3>
                    <div class="mini-viewport">

                        <asp:PlaceHolder ID="phMiniHome" runat="server">
                            <div class="mini-slider-container">
                                <div class="mini-slider-wrapper" id="minisliderwrapper">
                                    <asp:Repeater ID="rptminiheroslider" runat="server">
                                        <ItemTemplate>
                                            <div class="mini-slide">
                                                <img src='<%# GetBase64Image(Eval("ImgPath")) %>' alt='<%# Eval("Title") %>' />
                                                <div class="mini-hero-overlay-container">
                                                    <div class="mini-hero-content-wrapper">
                                                        <h1 class="mini-hero-display-title"><%# Eval("Title") %></h1>
                                                        <p class="mini-hero-subtext"><%# Eval("Subtitle") %></p>
                                                        <div class="mini-schedule-item">
                                                            <span class="mini-schedule-label">| SCHEDULE & RATES</span>
                                                            <span class="mini-hero-time"><%# Eval("Content") %></span>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </div>
                                <div class="mini-slider-indicators">
                                    <asp:Repeater ID="minirptIndicator" runat="server">
                                        <ItemTemplate>
                                            <button type="button" class='<%# Container.ItemIndex == 0 ? "dot active" : "dot" %>'
                                                data-index='<%# Container.ItemIndex %>'>
                                            </button>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </div>
                            </div>
                        </asp:PlaceHolder>

                        <asp:PlaceHolder ID="phMiniAnno" runat="server" Visible="false">
                            <div class="mini-announcement-feed">

                                <div class="preview-section" id="preview-hero2">
                                    <p class="preview-label">Hero Preview</p>
                                    <div class="mini-hero">
                                        <asp:Image ID="PreAnnoHeroImg" runat="server" ImageUrl="~/images/placeholder.jpg" CssClass="mini-hero-img" ClientIDMode="Static" />
                                        <div class="mini-hero-overlay"></div>
                                        <div class="mini-hero-content">
                                            <div class="mini-hero-title">
                                                <asp:Label runat="server" Text="Announcements" ClientIDMode="Static"></asp:Label>
                                            </div>
                                            <div class="mini-hero-divider"></div>
                                        </div>
                                    </div>
                                </div>

                                <asp:Repeater ID="rptAnnoZigzag" runat="server">
                                    <ItemTemplate>
                                        <div class='<%# Container.ItemIndex % 2 != 0 ? "mini-announcement-item-wrapper reverse" : "mini-announcement-item-wrapper" %>'>

                                            <div class="mini-anno-text-side">
                                                <span class="mini-anno-date">
                                                    <i class="far fa-calendar-alt"></i><%# Eval("CreatedAt", "{0:MMMM dd, yyyy}") %>
                                                </span>
                                                <h3 style="font-size: 1.1rem;"><%# Eval("Title") %></h3>
                                                <div class="mini-anno-desc">
                                                    <%# Eval("Content") %>
                                                </div>

                                                <div class="mini-btn-wrapper">
                                                    <span class="mini-btn-apply-now">Details</span>
                                                </div>
                                            </div>

                                            <div class="mini-anno-image-side">
                                                <div class="mini-image-container">
                                                    <img src='<%# GetBase64Image(Eval("FilePath")) %>' alt='<%# Eval("Title") %>' style="width: 100%; height: 100%; object-fit: cover;" />
                                                </div>
                                            </div>

                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </asp:PlaceHolder>

                        <asp:PlaceHolder ID="phMiniAbout" runat="server" Visible="false">
                            <div class="preview-sticky-container">

                                <div class="preview-section" id="preview-hero">
                                    <p class="preview-label">Hero Preview</p>
                                    <div class="mini-hero">
                                        <asp:Image ID="imgAboutPreview" runat="server" ImageUrl="~/images/placeholder.jpg" CssClass="mini-hero-img" ClientIDMode="Static" />
                                        <div class="mini-hero-overlay"></div>
                                        <div class="mini-hero-content">
                                            <div class="mini-hero-title">
                                                <asp:Label ID="litAboutTitle" runat="server" Text="ABOUT US" ClientIDMode="Static"></asp:Label>
                                            </div>
                                            <div class="mini-hero-divider"></div>
                                        </div>
                                    </div>
                                </div>

                                <div class="preview-section" id="preview-story">
                                    <p class="preview-label">Our Story Preview</p>
                                    <div class="mini-story-grid">
                                        <div class="mini-story-text">
                                            <h3>Our Story</h3>
                                            <div class="mini-story-desc">
                                                <asp:Label ID="litAboutContent" runat="server" Text="Content will display here." ClientIDMode="Static"></asp:Label>
                                            </div>
                                        </div>
                                        <div class="mini-story-img-wrapper">
                                            <div class="mini-story-backdrop"></div>
                                            <asp:Image ID="imgStoryPreview" runat="server" ImageUrl="~/images/placeholder.jpg" CssClass="mini-story-img" ClientIDMode="Static" />
                                        </div>
                                    </div>
                                </div>

                                <div class="preview-section" id="preview-mission">
                                    <p class="preview-label">Mission/Vision Preview</p>
                                    <div class="mini-mission-bg">
                                        <div class="mini-mission-grid">
                                            <div class="mini-mission-card">
                                                <h4 style="font-size: 0.5rem;">MISSION</h4>
                                                <asp:Label ID="litMissionText" runat="server" ClientIDMode="Static" style="font-size: 0.4rem; margin-top: -4px; padding-top: 0;"></asp:Label>
                                            </div>
                                            <div class="mini-mission-card">
                                                <h4 style="font-size: 0.5rem;">VISION</h4>
                                                <asp:Label ID="litVisionText" runat="server" ClientIDMode="Static" style="font-size: 0.4rem; margin-top: -4px; padding-top: 0;"></asp:Label>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <div class="preview-section" id="preview-members">
                                    <p class="preview-label">Members Preview (Wraps every 4)</p>
                                    <div class="mini-members-grid">

                                        <asp:Repeater ID="rptAboutStack" runat="server">
                                            <ItemTemplate>
                                                <div class="mini-member-card">
                                                    <img src='<%# GetBase64Image(Eval("ImgPath")) %>' class="mini-member-circle" />
                                                    <div class="mini-member-name"><%# Eval("Title") %></div>
                                                    <div class="mini-member-role"><%# Eval("Subtitle") %></div>
                                                </div>
                                            </ItemTemplate>
                                        </asp:Repeater>

                                        <div class="mini-member-card live-typing">
                                            <asp:Image ID="imgMemberPreview" runat="server" ImageUrl="~/images/placeholder.jpg" CssClass="mini-member-circle" ClientIDMode="Static" />
                                            <div class="mini-member-name">
                                                <asp:Label ID="litMemberName" runat="server" Text="Name" ClientIDMode="Static"></asp:Label>
                                            </div>
                                            <div class="mini-member-role">
                                                <asp:Label ID="litMemberRole" runat="server" Text="Role" ClientIDMode="Static"></asp:Label>
                                            </div>
                                        </div>

                                    </div>
                                </div>

                            </div>
                        </asp:PlaceHolder>

                    </div>
                </div>
            </div>
        </div>

        <asp:PlaceHolder ID="phAnnouncementTables" runat="server">
            <div class="table-responsive">
                <div class="table-container">
                    <h3>Published Announcements</h3>
                    <asp:GridView ID="gvPublished" runat="server" AutoGenerateColumns="False" DataKeyNames="AnnouncementID" CssClass="announcement-table" OnSelectedIndexChanged="gvPublished_SelectedIndexChanged" OnRowDeleting="gvAnnouncement_RowDeleting">
                        <Columns>
                            <asp:BoundField DataField="DisplayOrder" HeaderText="Order" ItemStyle-Width="50px" />
                            <asp:BoundField DataField="Title" HeaderText="Title" />
                            <asp:BoundField DataField="CreatedAt" HeaderText="Created" DataFormatString="{0:MMM dd, yyyy}" />
                            <asp:BoundField DataField="EndDate" HeaderText="Ends On" DataFormatString="{0:MMM dd, yyyy}" NullDisplayText="-" />
                            <asp:CommandField ShowSelectButton="True" SelectText="Edit" ControlStyle-CssClass="edit-link" />
                            <asp:TemplateField HeaderText="Actions">
                                <ItemTemplate>
                                    <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete" Text="Delete" CssClass="delete-link" OnClientClick="return confirm('Delete this announcement?');" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>

            <div class="table-responsive">
                <div class="table-container" style="margin-top: 30px;">
                    <h3>Drafts</h3>
                    <asp:GridView ID="gvDrafts" runat="server" AutoGenerateColumns="False" DataKeyNames="AnnouncementID" CssClass="announcement-table" OnSelectedIndexChanged="gvDrafts_SelectedIndexChanged" OnRowDeleting="gvAnnouncement_RowDeleting">
                        <Columns>
                            <asp:BoundField DataField="Title" HeaderText="Title" />
                            <asp:BoundField DataField="CreatedAt" HeaderText="Created" DataFormatString="{0:MMM dd, yyyy}" />
                            <asp:CommandField ShowSelectButton="True" SelectText="Edit" ControlStyle-CssClass="edit-link" />
                            <asp:TemplateField HeaderText="Actions">
                                <ItemTemplate>
                                    <asp:LinkButton ID="LinkButton1" runat="server" CommandName="Delete" Text="Delete" CssClass="delete-link" OnClientClick="return confirm('Delete this draft?');" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>
        </asp:PlaceHolder>

        <asp:PlaceHolder ID="phGenericTables" runat="server" Visible="false">
            <div class="table-responsive">
                <div class="table-container" style="margin-top: 40px;">
                    <h3>Published Website Content</h3>
                    <asp:GridView ID="gvGenericPublished" runat="server" AutoGenerateColumns="False"
                        DataKeyNames="ContentID" CssClass="announcement-table" GridLines="None"
                        OnSelectedIndexChanged="gvGenericPublished_SelectedIndexChanged"
                        OnRowDeleting="gvGenericPublished_RowDeleting">
                        <Columns>
                            <asp:BoundField DataField="Section" HeaderText="Page Section" ItemStyle-Width="200px" />
                            <asp:BoundField DataField="Title" HeaderText="Title" />
                            <asp:BoundField DataField="Subtitle" HeaderText="Subtitle" />
                            <asp:CommandField ShowSelectButton="True" SelectText="Edit" ControlStyle-CssClass="edit-link" />
                            <asp:TemplateField HeaderText="Actions">
                                <ItemTemplate>
                                    <asp:LinkButton ID="LinkButton2" runat="server" CommandName="Delete" Text="Delete" CssClass="delete-link" OnClientClick="return confirm('Delete this content?');" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>
        </asp:PlaceHolder>

    </section>

</asp:Content>
