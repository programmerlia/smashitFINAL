<%@ Page Language="C#" AutoEventWireup="true"
    MasterPageFile="~/adminpage/admin.Master"
    CodeBehind="admin_announcement.aspx.cs"
    Inherits="Smash_IT.adminpage.admin_announcement" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">

    <link rel="stylesheet" href='<%= ResolveUrl("~/css/admin_announcement.css") %>' />

</asp:Content>


<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <section class="admin-section container">
        <h2>Announcement Dashboard</h2>

        <div class="dashboard-container">
            <h3>Create New Announcement</h3>

            <asp:Label ID="lblMessage" runat="server" CssClass="message" EnableViewState="false"></asp:Label>

            <asp:HiddenField ID="hfAnnouncementID" runat="server" />

            <div class="form-grid">
                <div class="left-col">
                    <div class="form-group">
                        <label>Title:</label>
                        <asp:TextBox ID="txtTitle" runat="server" CssClass="form-control" MaxLength="150" Required="true"></asp:TextBox>
                    </div>

                    <div class="form-group">
                        <label>Description:</label>
                        <asp:TextBox ID="txtDescription" runat="server" TextMode="MultiLine" CssClass="form-control"></asp:TextBox>
                    </div>

                    <div class="form-group">
                        <label>View Status:</label>
                        <asp:CheckBox ID="chkViewStatus" runat="server" CssClass="toggle-status" Checked="true" Text=" Visible" />
                    </div>

                    <div class="form-group">
                        <label>Order:</label>
                        <asp:TextBox ID="txtOrder" runat="server" TextMode="Number" CssClass="form-control" Width="80px" Text="1"></asp:TextBox>
                    </div>
                </div>

                <div class="right-col">
                    <div class="form-group">
                        <label>Image Upload:</label>
                        <asp:FileUpload ID="fileUploadImage" runat="server" />
                    </div>

                    <div class="form-group">
                        <label>Date Until:</label>
                        <asp:TextBox ID="txtDateUntil" runat="server" TextMode="Date" CssClass="form-control"></asp:TextBox>
                    </div>
                </div>
            </div>

            <div class="btn-container">
                <asp:Button ID="btnPublish" runat="server" Text="Publish Announcement" CssClass="btn-publish" OnClick="btnPublish_Click" />
                <asp:Button ID="btnSaveDraft" runat="server" Text="Save Draft" CssClass="btn-draft" OnClick="btnSaveDraft_Click" formnovalidate="formnovalidate" />
            </div>

        </div>


        <!--GV FOR THE PUBLISHED ANNOUNCEMENT-->
        <div class="table-container" style="margin-top: 40px;">
            <h3>Published Announcements</h3>

            <asp:GridView ID="gvPublished" runat="server" AutoGenerateColumns="False" DataKeyNames="AnnouncementID"
                CssClass="announcement-table" GridLines="None" ShowHeaderWhenEmpty="True"
                OnSelectedIndexChanged="gvPublished_SelectedIndexChanged" OnRowDeleting="gvPublished_RowDeleting">
                <Columns>
                    <asp:BoundField DataField="DisplayOrder" HeaderText="Order" ItemStyle-Width="50px" />
                    <asp:BoundField DataField="Title" HeaderText="Title" />
                    <asp:BoundField DataField="CreatedAt" HeaderText="Date Created" DataFormatString="{0:MMM dd, yyyy}" ReadOnly="True" />
                    <asp:CheckBoxField DataField="ViewStatus" HeaderText="Visible" ItemStyle-HorizontalAlign="Center" />

                    <asp:TemplateField HeaderText="Actions">
                                    <ItemTemplate>
                                    
                                        <asp:LinkButton ID="btnEdit" runat="server" CommandName="Select" 
                                            CssClass="edit-link" style="margin-right:15px; font-weight:bold; color:#000058; text-decoration:none;">
                                            <i class="fas fa-edit"></i> Edit
                                        </asp:LinkButton>

                                      
                                        <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete" 
                                            CssClass="delete-link" style="font-weight:bold; color:#e11d48; text-decoration:none;"
                                            OnClientClick="return confirm('Are you sure you want to delete this announcement?');">
                                            <i class="fas fa-trash"></i> Delete
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </asp:TemplateField>
                </Columns>
                <EmptyDataTemplate>
                    <p style="padding: 15px; color: #777;">No published announcements found.</p>
                </EmptyDataTemplate>
            </asp:GridView>
        </div>


        <!--GV FOR THE SAVED DRAFTS-->
        <div class="table-container" style="margin-top: 40px;">
            <h3>Saved Drafts</h3>

            <asp:GridView ID="gvDrafts" runat="server" AutoGenerateColumns="False" DataKeyNames="AnnouncementID"
                CssClass="announcement-table" GridLines="None" ShowHeaderWhenEmpty="True"
                OnSelectedIndexChanged="gvDrafts_SelectedIndexChanged" OnRowDeleting="gvDrafts_RowDeleting">
                <Columns>
                    <asp:BoundField DataField="DisplayOrder" HeaderText="Order" ItemStyle-Width="50px" />
                    <asp:BoundField DataField="Title" HeaderText="Title" />
                    <asp:BoundField DataField="CreatedAt" HeaderText="Date Created" DataFormatString="{0:MMM dd, yyyy}" ReadOnly="True" />
                    <asp:CheckBoxField DataField="ViewStatus" HeaderText="Visible" ItemStyle-HorizontalAlign="Center" />

                    <asp:TemplateField HeaderText="Actions">
                                <ItemTemplate>
                                   
                                    <asp:LinkButton ID="btnEdit" runat="server" CommandName="Select" 
                                        CssClass="edit-link" style="margin-right:15px; font-weight:bold; color:#000058; text-decoration:none;">
                                        <i class="fas fa-edit"></i> Edit
                                    </asp:LinkButton>

                                  
                                    <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete" 
                                        CssClass="delete-link" style="font-weight:bold; color:#e11d48; text-decoration:none;"
                                        OnClientClick="return confirm('Are you sure you want to delete this announcement?');">
                                        <i class="fas fa-trash"></i> Delete
                                    </asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                </Columns>
                <EmptyDataTemplate>
                    <p style="padding: 15px; color: #777;">No drafts found.</p>
                    <p style="padding: 15px; color: #777;">No drafts found.</p>
                </EmptyDataTemplate>
            </asp:GridView>
        </div>

    </section>

</asp:Content>
