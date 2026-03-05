<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="AuthModal.ascx.cs" Inherits="Smash_IT.Controls.AuthModal" %>

<style>
  @import url('https://fonts.googleapis.com/css2?family=Poppins:wght@400;600;700&display=swap');

  /* Make only the modal use this font (won't affect the whole site) */
  #loginSignupModal, #loginSignupModal * {
    font-family: 'Poppins', sans-serif;
    box-sizing: border-box;
  }

  /* ===== CARD LOOK (same vibe as your login.aspx) ===== */
  .auth-card {
    padding: 24px 26px;
    background: #fff;
    border-radius: 14px;
  }

  .auth-title {
    font-size: 1.35rem;
    color: #1e3a8a;
    margin: 8px 0 4px;
    font-weight: 800;
    letter-spacing: .5px;
  }

  .auth-sub {
    font-size: .85rem;
    color: #64748b;
    margin-bottom: 14px;
    line-height: 1.5;
  }

  .auth-logo {
    width: 58px;
    height: auto;
    display: block;
  }

  /* ===== Tabs cleaner ===== */
  .auth-tabs.nav-tabs {
    border-bottom: 1px solid #e2e8f0;
  }

  .auth-tabs .nav-link {
    border: 0 !important;
    border-bottom: 2px solid transparent !important;
    color: #64748b;
    font-weight: 700;
    padding: .65rem .8rem;
  }

  .auth-tabs .nav-link.active {
    color: #1e3a8a !important;
    border-bottom-color: #1e3a8a !important;
    background: transparent !important;
  }

  /* ===== Form ===== */
  .form-group { margin-bottom: 14px; }
  .field-label {
    display:block;
    font-size: .8rem;
    font-weight: 700;
    color:#1e3a8a;
    margin-bottom: 6px;
  }

  .input {
    width: 100%;
    padding: 12px 15px;
    border: 1px solid #e2e8f0;
    border-radius: 8px;
    background: #f8fafc;
    font-size: .9rem;
    transition: all .2s ease;
  }

  .input:focus {
    outline: none;
    border-color: #1e3a8a;
    background: #fff;
    box-shadow: 0 0 0 3px rgba(30, 58, 138, 0.10);
  }

  /* WebForms TextBox uses CssClass; ensure it looks like input */
  input.input { appearance: none; }

  /* ===== Buttons (same as your login.aspx btn-primary) ===== */
  .btn-primary-like {
    display: inline-block;
    width: 100%;
    margin-top: 10px;
    padding: 10px 12px;
    background-color: #0078d4;
    color: #fff;
    border: none;
    border-radius: 6px;
    cursor: pointer;
    font-size: 15px;
    font-weight: 700;
    text-align: center;
    text-transform: uppercase;
    transition: background .2s ease, transform .1s ease;
  }

  .btn-primary-like:hover, .btn-primary-like:focus {
    background-color: #0062b3;
    outline: none;
  }
  .btn-primary-like:active { transform: scale(.98); }

  .btn-success-like {
    background-color: #16a34a;
  }
  .btn-success-like:hover, .btn-success-like:focus {
    background-color: #15803d;
  }

  .message {
    display:block;
    margin-top: 12px;
    font-size: .85rem;
    color: #e11d48;
    text-align:center;
    font-weight: 600;
  }

  /* ===== Stepper (keep your concept, styled to match) ===== */
  .signup-stepper{
    display:flex;
    align-items:center;
    gap:.5rem;
    user-select:none;
    margin-bottom: 14px;
  }
  .step-btn{
    display:flex;
    align-items:center;
    gap:.5rem;
    border:0;
    background:transparent;
    padding:.25rem .25rem;
    color:#64748b;
    cursor:pointer;
  }
  .step-circle{
    width:28px;
    height:28px;
    border-radius:999px;
    display:inline-flex;
    align-items:center;
    justify-content:center;
    border:2px solid #cbd5e1;
    font-weight:800;
    line-height:1;
    font-size: .85rem;
  }
  .step-text{ font-size:.85rem; font-weight:800; }
  .step-line{ flex:1; height:2px; background:#e2e8f0; }

  .step-btn.is-active{ color:#1e3a8a; }
  .step-btn.is-active .step-circle{
    border-color:#1e3a8a; background:#1e3a8a; color:#fff;
  }

  .step-btn.is-done{ color:#0f172a; }
  .step-btn.is-done .step-circle{
    border-color:#0f172a; background:#0f172a; color:#fff;
  }

  .step-btn.is-locked{ cursor:default; color:#adb5bd; }
  .step-btn.is-locked .step-circle{
    border-color:#e2e8f0; color:#94a3b8; background:#f8fafc;
  }

  /* Modal spacing */
  #loginSignupModal .modal-header {
    border-bottom: 0;
    padding-bottom: 0;
  }
  #loginSignupModal .modal-body { padding-top: 10px; }
</style>

<!-- Signup/Login Modal -->
<div class="modal fade" id="loginSignupModal" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
  <div class="modal-dialog modal-dialog-centered">
    <div class="modal-content">
      <div class="modal-header">
        <button type="button" class="btn-close ms-auto" data-bs-dismiss="modal" aria-label="Close"></button>
      </div>

      <div class="modal-body">
        <div class="auth-card">

          <!-- Header -->
          <img src="<%= ResolveUrl("~/images/logo.png") %>" alt="Smash-It Logo" class="auth-logo" />
          <div class="auth-title">LOGIN / SIGNUP</div>
          <div class="auth-sub">Sign in to book courts and manage your reservations.</div>

          <!-- Tabs -->
          <ul class="nav nav-tabs auth-tabs" id="authTab" role="tablist">
            <li class="nav-item">
              <a class="nav-link active" data-bs-toggle="tab" href="#loginTab" role="tab">Login</a>
            </li>
            <li class="nav-item">
              <a class="nav-link" data-bs-toggle="tab" href="#signupTab" role="tab">Signup</a>
            </li>
          </ul>

          <div class="tab-content mt-3">

            <!-- LOGIN TAB -->
            <div class="tab-pane fade show active" id="loginTab" role="tabpanel">
              <div class="form-group">
                <label class="field-label">Username</label>
                <asp:TextBox ID="txtLoginUsername" runat="server" CssClass="input" Placeholder="Enter username" />
              </div>

              <div class="form-group">
                <label class="field-label">Password</label>
                <asp:TextBox ID="txtLoginPassword" runat="server" TextMode="Password" CssClass="input" Placeholder="Enter password" />
              </div>

              <asp:Button ID="btnLogin" runat="server"
                CssClass="btn-primary-like"
                Text="Sign In"
                OnClick="btnLogin_Click" />

              <asp:Label ID="lblLoginMsg" runat="server" CssClass="message" />
            </div>

            <!-- SIGNUP TAB -->
            <div class="tab-pane fade" id="signupTab" role="tabpanel">
            <asp:HiddenField ID="hfSignupStep" runat="server" Value="1" />
              <!-- Stepper -->
              <div class="signup-stepper">
                <button type="button" id="step1Indicator" class="step-btn is-active">
                  <span class="step-circle">1</span>
                  <span class="step-text">Details</span>
                </button>

                <div class="step-line"></div>

                <button type="button" id="step2Indicator" class="step-btn">
                  <span class="step-circle">2</span>
                  <span class="step-text">Verify</span>
                </button>

                <div class="step-line"></div>

                <button type="button" id="step3Indicator" class="step-btn is-locked">
                  <span class="step-circle">3</span>
                  <span class="step-text">Terms</span>
                </button>
              </div>

              <!-- Step 1 -->
              <div id="signupStep1" runat="server">
                <div class="form-group">
                  <label class="field-label">First Name</label>
                  <asp:TextBox ID="txtFirstname" runat="server" CssClass="input" Placeholder="Enter first name" />
                </div>

                <div class="form-group">
                  <label class="field-label">Last Name</label>
                  <asp:TextBox ID="txtLastname" runat="server" CssClass="input" Placeholder="Enter last name" />
                </div>

                <div class="form-group">
                  <label class="field-label">Email</label>
                  <asp:TextBox ID="txtEmail" runat="server" CssClass="input" Placeholder="Enter email" />
                </div>

                <div class="form-group">
                  <label class="field-label">Phone Number</label>
                  <asp:TextBox ID="txtPhone" runat="server" CssClass="input" Placeholder="Enter phone number" />
                </div>

                <div class="form-group">
                  <label class="field-label">Username</label>
                  <asp:TextBox ID="txtSignupUsername" runat="server" CssClass="input" Placeholder="Create a username" />
                </div>

                <div class="form-group">
                  <label class="field-label">Password</label>
                  <asp:TextBox ID="txtSignupPassword" runat="server" TextMode="Password" CssClass="input"
                    Placeholder="Create a password" autocomplete="new-password" />
                </div>

                <asp:Button ID="btnSignup" runat="server"
                  CssClass="btn-primary-like btn-success-like"
                  Text="Next"
                  OnClick="btnSignup_Click"
                  OnClientClick="return validateAndSendOTP();" />

                <asp:Label ID="lblSignupMsg" runat="server" CssClass="message" />
              </div>

              <!-- Step 2 -->
              <div id="signupStep2" runat="server" style="display:none;">
                <div class="auth-sub" style="margin-top:4px;">
                  We sent an OTP to your email. Enter it below to verify your account.
                </div>

                <div class="form-group">
                  <label class="field-label">OTP</label>
                  <asp:TextBox ID="txtOTP" runat="server" CssClass="input" Placeholder="Enter OTP" />
                </div>

                <asp:Button ID="btnVerifyOTP" runat="server"
                  CssClass="btn-primary-like"
                  Text="Verify OTP"
                  OnClick="btnVerifyOTP_Click" />

                <asp:Label ID="lblOTPMsg" runat="server" CssClass="message" />
              </div>

              <!-- Step 3 -->
              <div id="signupStep3" runat="server" style="display:none;">
                <div class="auth-sub" style="margin-top:4px;">
                  Please read and accept our Terms &amp; Conditions before creating your account.
                </div>

                <div class="border rounded p-2 mb-2" style="max-height:160px; overflow:auto;">
                  <strong>Terms &amp; Conditions</strong>
                  <ul class="mb-0">
                    <li>You agree to provide accurate information.</li>
                    <li>You agree to follow community rules.</li>
                    <li>We may contact you for verification/account purposes.</li>
                  </ul>
                </div>

                <div class="d-flex align-items-start">
                  <asp:CheckBox ID="chkTerms" runat="server" />
                  <label for="<%= chkTerms.ClientID %>" class="ms-2" style="font-size:.9rem; color:#0f172a;">
                    I agree to the Terms &amp; Conditions
                  </label>
                </div>

                <asp:Label ID="lblTermsMsg" runat="server" CssClass="message" />

                <asp:Button ID="btnCreateAccount" runat="server"
                  CssClass="btn-primary-like btn-success-like"
                  Text="Create Account"
                  OnClick="btnCreateAccount_Click"
                  OnClientClick="return validateTermsStep();" />
              </div>

            </div>
          </div>

        </div>
      </div>

    </div>
  </div>
</div>

<script>
    // Use ClientIDs so WebForms doesn't break our DOM lookup
    const ids = {
        s1: '<%= signupStep1.ClientID %>',
    s2: '<%= signupStep2.ClientID %>',
    s3: '<%= signupStep3.ClientID %>',
        i1: 'step1Indicator',
        i2: 'step2Indicator',
        i3: 'step3Indicator',
        step: '<%= hfSignupStep.ClientID %>' 
    };

    function $(id) { return document.getElementById(id); }

    function setStep(activeStep) {
        const s1 = $(ids.s1), s2 = $(ids.s2), s3 = $(ids.s3);
        const i1 = $(ids.i1), i2 = $(ids.i2), i3 = $(ids.i3);
        if (!s1 || !s2 || !s3 || !i1 || !i2 || !i3) return;

        s1.style.display = (activeStep === 1) ? 'block' : 'none';
        s2.style.display = (activeStep === 2) ? 'block' : 'none';
        s3.style.display = (activeStep === 3) ? 'block' : 'none';

        [i1, i2, i3].forEach(el => el.classList.remove('is-active', 'is-done'));

        if (activeStep === 1) i1.classList.add('is-active');
        if (activeStep === 2) { i1.classList.add('is-done'); i2.classList.add('is-active'); }
        if (activeStep === 3) { i1.classList.add('is-done'); i2.classList.add('is-done'); i3.classList.add('is-active'); }


        const hf = document.getElementById(ids.step);
        if (hf) hf.value = String(activeStep);
    }
    }

    function goToStep1() { setStep(1); }
    function goToStep2() { setStep(2); }
    function goToStep3() {
        const i3 = $(ids.i3);
        if (i3 && i3.classList.contains('is-locked')) return;
        setStep(3);
    }

    function unlockStep3() {
        const i3 = $(ids.i3);
        if (!i3) return;
        i3.classList.remove('is-locked');
        i3.style.cursor = 'pointer';
        i3.onclick = goToStep3;
    }

    function lockStep3() {
        const i3 = $(ids.i3);
        if (!i3) return;
        i3.classList.add('is-locked');
        i3.onclick = null;
        i3.style.cursor = 'default';
    }

    function initSignupUi() {
        const i1 = $(ids.i1), i2 = $(ids.i2);
        if (i1) i1.onclick = goToStep1;
        if (i2) i2.onclick = goToStep2;

        const hf = document.getElementById(ids.step);
        const step = hf ? parseInt(hf.value || "1", 10) : 1;

        if (step >= 3) unlockStep3();
        else lockStep3();

        setStep(step);
    }

    document.addEventListener('DOMContentLoaded', initSignupUi);

    // UpdatePanel support
    if (window.Sys && Sys.WebForms && Sys.WebForms.PageRequestManager) {
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(initSignupUi);
    }


    window.AuthSignup = {
        setStep: setStep,
        unlockStep3: unlockStep3,
        lockStep3: lockStep3,
        goToStep1: goToStep1,
        goToStep2: goToStep2,
        goToStep3: goToStep3


    };
</script>