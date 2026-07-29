<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PreparingPayment.aspx.cs" Inherits="Smash_IT.payments.PreparingPayment" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <meta charset="UTF-8" />
    <title>Preparing Payment</title>
    <style>
        body {
            margin: 0;
            font-family: 'Poppins', sans-serif;
            background: #f7f8fc;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        .box {
            width: 420px;
            background: white;
            border-radius: 18px;
            padding: 36px;
            box-shadow: 0 12px 30px rgba(0,0,0,.08);
            text-align: center;
        }
        .spinner {
            width: 54px;
            height: 54px;
            margin: 0 auto 20px;
            border: 6px solid #e9ecef;
            border-top: 6px solid #1a187c;
            border-radius: 50%;
            animation: spin 1s linear infinite;
        }
        .dots {
            margin-top: 12px;
            font-size: 26px;
            color: #1a187c;
            letter-spacing: 4px;
        }
        .dots span {
            animation: blink 1.4s infinite;
        }
        .dots span:nth-child(2) { animation-delay: .2s; }
        .dots span:nth-child(3) { animation-delay: .4s; }

        @keyframes spin {
            to { transform: rotate(360deg); }
        }
        @keyframes blink {
            0%, 80%, 100% { opacity: .2; }
            40% { opacity: 1; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="box">
            <div class="spinner"></div>
            <h2>Connecting to PayMongo</h2>
            <p>Please wait while we prepare your secure checkout.</p>
            <div class="dots"><span>•</span><span>•</span><span>•</span></div>
        </div>
    </form>
</body>
</html>