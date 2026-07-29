<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="login.aspx.cs" Inherits="login.login" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="UTF-8" />
    <title>Official Login | Smash-It Management System</title>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" />
    <link href="https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600;700;800&display=swap" rel="stylesheet">
    <style>
        :root { --smash-blue: #1e3a8a; --electric-yellow: #facc15; --bg-light: #f4f7fe; }
        * { box-sizing: border-box; margin: 0; padding: 0; font-family: 'Poppins', sans-serif; }
        body { background-color: var(--bg-light); height: 100vh; display: flex; align-items: center; justify-content: center; padding: 20px; }
        .login-wrapper { display: flex; width: 1100px; height: 650px; background: #fff; border-radius: 24px; overflow: hidden; box-shadow: 0 25px 50px rgba(30, 58, 138, 0.15); }
        .brand-side { flex: 1.4; background: url('https://scontent.fmnl45-2.fna.fbcdn.net/v/t39.30808-6/602407929_122157318458750009_6768574683014781300_n.jpg?_nc_cat=106&ccb=1-7&_nc_sid=dd6889&_nc_eui2=AeEnW4B2HfwPBPwbNJwS3toUX9zJkyS5OtBf3MmTJLk60JvbOK4krI7FXJggxWP4tO4j04tfmORS54YBwCaXwmV9&_nc_ohc=dbGGBLLk8T8Q7kNvwGCBQqN&_nc_oc=AdlGFozu3uARiMXvVH8SXOlaLBUFDBCn067kZNr4GdmYHYBUmAj__TI7hIXbJJsutwrSGi7PPq0yMNL_ekcmInG3&_nc_zt=23&_nc_ht=scontent.fmnl45-2.fna&_nc_gid=F4UMIqDByDK6XTHJy_Yi8Q&_nc_ss=8&oh=00_Afw_8JMYTW47200OZ1o2MZJ6txUJq7UdDGNfG8hZwkkzSA&oe=69BCD3EC'); background-size: cover; background-position: center; position: relative; display: flex; align-items: flex-end; padding: 50px; }
        .brand-side::after { content: ''; position: absolute; top: 0; left: 0; width: 100%; height: 100%; background: linear-gradient(0deg, rgba(30, 58, 138, 0.9) 0%, rgba(30, 58, 138, 0.2) 100%); }
        .brand-content { position: relative; z-index: 2; color: #fff; }
        .brand-content h2 { font-size: 2.5rem; font-weight: 800; line-height: 1.1; text-transform: uppercase; }
        .brand-content p { font-size: 1rem; margin-top: 15px; color: var(--electric-yellow); font-weight: 600; letter-spacing: 1px; text-transform: uppercase; }
        .form-side { flex: 1; padding: 60px; display: flex; flex-direction: column; justify-content: center; background: #fff; }
        .logo-area { margin-bottom: 40px; }
        .logo-area img { width: 80px; margin-bottom: 10px; }
        .form-side h1 { font-size: 1.8rem; font-weight: 800; color: var(--smash-blue); margin-bottom: 5px; }
        .form-side .sub-text { color: #64748b; font-size: 0.9rem; margin-bottom: 35px; }
        .form-group { margin-bottom: 25px; }
        .field-label { display: block; font-size: 0.75rem; font-weight: 700; color: var(--smash-blue); text-transform: uppercase; margin-bottom: 8px; letter-spacing: 0.5px; }
        .input-wrapper { position: relative; }
        .input-wrapper i.main-icon { position: absolute; left: 18px; top: 50%; transform: translateY(-50%); color: #94a3b8; font-size: 1.1rem; }
        .toggle-password { position: absolute; right: 18px; top: 50%; transform: translateY(-50%); color: #94a3b8; cursor: pointer; transition: color 0.3s; }
        .toggle-password:hover { color: var(--smash-blue); }
        .input-field { width: 100%; padding: 16px 45px 16px 50px; border: 2px solid #f1f5f9; border-radius: 12px; background: #f8fafc; font-size: 0.95rem; transition: 0.3s; }
        .input-field:focus { outline: none; border-color: var(--smash-blue); background: #fff; box-shadow: 0 10px 20px rgba(30, 58, 138, 0.05); }
        .options-row { display: flex; justify-content: space-between; align-items: center; margin-bottom: 25px; font-size: 0.85rem; }
        .checkbox-container { display: flex; align-items: center; cursor: pointer; color: #64748b; }
        .btn-login { width: 100%; padding: 18px; background: var(--electric-yellow); color: var(--smash-blue); border: none; border-radius: 12px; font-weight: 800; font-size: 1rem; text-transform: uppercase; cursor: pointer; transition: all 0.3s; letter-spacing: 1px; display: flex; justify-content: center; align-items: center; gap: 10px; }
        .btn-login:hover { background: #eab308; transform: translateY(-3px); box-shadow: 0 12px 24px rgba(250, 204, 21, 0.3); }
        .error-message { background: #fff1f2; color: #e11d48; padding: 15px; border-radius: 10px; font-size: 0.85rem; font-weight: 600; text-align: center; margin-top: 25px; border: 1px solid #ffe4e6; }
        .spinner { display: none; width: 20px; height: 20px; border: 3px solid rgba(30, 58, 138, 0.3); border-radius: 50%; border-top-color: var(--smash-blue); animation: spin 1s infinite; }
        @keyframes spin { to { transform: rotate(360deg); } }
        @media (max-width: 1024px) { .login-wrapper { width: 100%; height: auto; flex-direction: column; } .brand-side { height: 300px; padding: 30px; } }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="login-wrapper">
            <div class="brand-side">
                <div class="brand-content">
                    <h2>Official Smash-It<br />Management System</h2>
                    <p>Performance • Precision • Play</p>
                </div>
            </div>
            <div class="form-side">
                <div class="logo-area">
                    <img src="images/logo.png" alt="Smash-It Logo" />
                    <h1>Staff Sign-In</h1>
                    <p class="sub-text">Smash-It's authorized personnels only.</p>
                </div>
                <div class="form-group">
                    <label class="field-label">Username</label>
                    <div class="input-wrapper">
                        <i class="fas fa-user-shield main-icon"></i>
                        <asp:TextBox ID="txtUsername" runat="server" CssClass="input-field" placeholder="Admin Username" />
                    </div>
                </div>
                <div class="form-group">
                    <label class="field-label">Password</label>
                    <div class="input-wrapper">
                        <i class="fas fa-key main-icon"></i>
                        <asp:TextBox ID="txtPassword" runat="server" CssClass="input-field" TextMode="Password" placeholder="••••••••" />
                        <i class="fas fa-eye toggle-password" onclick="togglePass()"></i>
                    </div>
                </div>
                <div class="options-row">
                    <label class="checkbox-container">
                        <asp:CheckBox ID="chkRememberMe" runat="server" />
                        <span style="margin-left: 5px;">Remember this device</span>
                    </label>
                </div>
                <asp:Button ID="btnLogin1" runat="server" Text="Launch Dashboard" OnClick="btnLogin1_Click"
                    CssClass="btn-login" OnClientClick="return startLoading();" />
                <div id="loadingIndicator" style="display: none; justify-content: center; margin-top: 15px;">
                    <div class="spinner" style="display: block;"></div>
                    <span style="margin-left: 10px; color: var(--smash-blue); font-weight: 600;">Authenticating...</span>
                </div>
                <asp:Label ID="lblMessage" runat="server" CssClass="error-message" Visible="false" />
            </div>
        </div>
    </form>
    <script>
        function togglePass() {
            const passField = document.getElementById('<%= txtPassword.ClientID %>');
            const toggleIcon = document.querySelector('.toggle-password');
            if (passField.type === "password") {
                passField.type = "text";
                toggleIcon.classList.replace('fa-eye', 'fa-eye-slash');
            } else {
                passField.type = "password";
                toggleIcon.classList.replace('fa-eye-slash', 'fa-eye');
            }
        }
        function startLoading() {
            const user = document.getElementById('<%= txtUsername.ClientID %>').value;
            const pass = document.getElementById('<%= txtPassword.ClientID %>').value;
            if (user === "" || pass === "") { alert("Please fill in all fields."); return false; }
            document.getElementById('<%= btnLogin1.ClientID %>').style.display = 'none';
            document.getElementById('loadingIndicator').style.display = 'flex';
            return true;
        }
    </script>
</body>
</html>