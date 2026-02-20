<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="login.aspx.cs" Inherits="login.login" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Admin Login | Smash-It</title>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Poppins:wght@400;600;700&display=swap');

        /* ===== GLOBAL RESET ===== */
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            font-family: 'Poppins', sans-serif;
        }

        body {
            background-color: #f4f7fe;
            height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
        }

        /* ===== LAYOUT ===== */
        .page-layout {
            display: flex;
            width: 900px;
            height: 550px;
            background: #fff;
            border-radius: 15px;
            overflow: hidden;
            box-shadow: 0 15px 35px rgba(30, 58, 138, 0.1);
        }

        .login-card {
            flex: 1;
            padding: 50px;
            display: flex;
            flex-direction: column;
            justify-content: center;
        }

        .side-image {
            flex: 1;
            background: #1e3a8a;
            position: relative;
        }

        .login-photo {
            width: 100%;
            height: 100%;
            object-fit: cover;
            opacity: 0.9;
        }

        /* ===== ELEMENTS ===== */
        .logo {
            width: 70px;
            margin-bottom: 20px;
        }

        .login-title {
            font-size: 1.8rem;
            color: #1e3a8a;
            margin-bottom: 8px;
            font-weight: 700;
        }

        .login-sub {
            font-size: 0.85rem;
            color: #64748b;
            margin-bottom: 30px;
            line-height: 1.5;
        }

        .form-group {
            margin-bottom: 20px;
        }

        .field-label {
            display: block;
            font-size: 0.8rem;
            font-weight: 600;
            color: #1e3a8a;
            margin-bottom: 6px;
        }

        .input {
            width: 100%;
            padding: 12px 15px;
            border: 1px solid #e2e8f0;
            border-radius: 8px;
            background: #f8fafc;
            font-size: 0.9rem;
            transition: all 0.3s ease;
        }

        .input:focus {
            outline: none;
            border-color: #1e3a8a;
            background: #fff;
            box-shadow: 0 0 0 3px rgba(30, 58, 138, 0.1);
        }

        /* ===== PRIMARY BUTTON ===== */
        .btn-primary {
            display: inline-block;
            width: 100%;
            margin-top: 16px;
            padding: 10px 12px;
            background-color: #0078d4;
            color: #fff;
            border: none;
            border-radius: 6px;
            cursor: pointer;
            font-size: 15px;
            font-weight: 600;
            text-align: center;
            text-transform: uppercase;
            transition: background 0.2s ease, transform 0.1s ease;
        }

        .btn-primary:hover, .btn-primary:focus {
            background-color: #0062b3;
            outline: none;
        }

        .btn-primary:active {
            transform: scale(0.98);
        }

        .message {
            display: block;
            margin-top: 20px;
            font-size: 0.85rem;
            color: #e11d48;
            text-align: center;
            font-weight: 500;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="page-layout">
            
            <div class="login-card">
                <img src="images/logo.png" alt="Smash-It Logo" class="logo" />
                <h1 class="login-title">ADMIN LOGIN</h1>
                <p class="login-sub">Welcome back, staff. Please sign in to manage the sports center.</p>

                <div class="form-group">
                    <label class="field-label">Username or Email</label>
                    <asp:TextBox ID="txtUsername" runat="server" CssClass="input" placeholder="Enter username" />
                </div>

                <div class="form-group">
                    <label class="field-label">Password</label>
                    <asp:TextBox ID="txtPassword" runat="server" CssClass="input" TextMode="Password" placeholder="Enter password" />
                </div>

                <asp:Button ID="btnLogin1" runat="server" Text="Sign In" OnClick="btnLogin1_Click" CssClass="btn-primary" />

                <asp:Label ID="lblMessage" runat="server" CssClass="message" />
            </div>

            <div class="side-image">
                <img src="https://scontent.fmnl17-2.fna.fbcdn.net/v/t39.30808-6/628056061_122162347400750009_5936316705622488222_n.jpg?stp=cp6_dst-jpg_tt6&_nc_cat=107&ccb=1-7&_nc_sid=7b2446&_nc_eui2=AeFQm8omG7wNkBYSJJGKdP-1DivofHp5rrYOK-h8enmutk3O0Z93XutCrruLJUhbFiuZ7fq1NaIYOTEEB7G3lF5g&_nc_ohc=-XNj-jUMfdIQ7kNvwEDzzXv&_nc_oc=AdlK7ijrdhyVtFkEKCJ0NJldARRWc1of-MCI870LVTHicK-b1ymRDBmUemeh6BfhGvs&_nc_zt=23&_nc_ht=scontent.fmnl17-2.fna&_nc_gid=e50U5PsLmMsKPzy3pQpxlg&oh=00_AfuLTahysL0dQmwPPgFE2J5DgdKhHuuKusDdDxuEC9UA7w&oe=699E7D8F" alt="Sports Center" class="login-photo" />
            </div>

        </div>
    </form>
</body>
</html>