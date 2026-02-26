<%@  Control Language="C#" AutoEventWireup="true" CodeBehind="AuthModal.ascx.cs" Inherits="Smash_IT.Controls.AuthModal" %>

<style>
  .signup-stepper{
    display:flex;
    align-items:center;
    gap:.5rem;
    user-select:none;
  }
  .step-btn{
    display:flex;
    align-items:center;
    gap:.5rem;
    border:0;
    background:transparent;
    padding:.25rem .25rem;
    color:#6c757d;
    cursor:pointer;
  }
  .step-circle{
    width:28px;
    height:28px;
    border-radius:999px;
    display:inline-flex;
    align-items:center;
    justify-content:center;
    border:2px solid #ced4da;
    font-weight:600;
    line-height:1;
  }
  .step-text{ font-size:.875rem; font-weight:600; }
  .step-line{ flex:1; height:2px; background:#e9ecef; }

  .step-btn.is-active{ color:#0d6efd; }
  .step-btn.is-active .step-circle{
    border-color:#0d6efd; background:#0d6efd; color:#fff;
  }

  .step-btn.is-done{ color:#1a187c; }
  .step-btn.is-done .step-circle{
    border-color:#1a187c; background:#1a187c; color:#fff;
  }

  .step-btn.is-locked{ cursor:default; color:#adb5bd; }
  .step-btn.is-locked .step-circle{
    border-color:#dee2e6; color:#adb5bd; background:#f8f9fa;
  }
</style>
<!-- Signup/Login Modal -->
<div class="modal fade" id="loginSignupModal" tabindex="-1" aria-hidden="true" data-bs-backdrop="static">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Login / Signup</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>

            <div class="modal-body">
                <ul class="nav nav-tabs" id="authTab" role="tablist">
                    <li class="nav-item">
                        <a class="nav-link active" data-bs-toggle="tab" href="#loginTab">Login</a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" data-bs-toggle="tab" href="#signupTab">Signup</a>
                    </li>
                </ul>

                <div class="tab-content mt-3">
                    <!-- LOGIN TAB -->
                    <div class="tab-pane fade show active" id="loginTab">
                        <asp:TextBox ID="txtLoginUsername" runat="server" CssClass="form-control mb-2" Placeholder="Username" />
                        <asp:TextBox ID="txtLoginPassword" runat="server" TextMode="Password" CssClass="form-control mb-2" Placeholder="Password" />
                        <asp:Button ID="btnLogin" runat="server" CssClass="btn btn-primary" Text="Login" OnClick="btnLogin_Click" />
                        <asp:Label ID="lblLoginMsg" CssClass="text-danger mt-2" runat="server" />
                    </div>

                    <!-- SIGNUP TAB -->
                    <div class="tab-pane fade" id="signupTab">
      
      <!-- Stepper -->
<div class="signup-stepper mb-3">
  <button type="button" id="step1Indicator" class="step-btn is-active" onclick="goToStep1()">
    <span class="step-circle">1</span>
    <span class="step-text">Details</span>
  </button>

  <div class="step-line"></div>

  <button type="button" id="step2Indicator" class="step-btn" onclick="goToStep2()">
    <span class="step-circle">2</span>
    <span class="step-text">Verify</span>
  </button>

  <div class="step-line"></div>

  <!-- Step 3 starts LOCKED (no onclick at all) -->
  <button type="button" id="step3Indicator" class="step-btn is-locked">
    <span class="step-circle">3</span>
    <span class="step-text">Terms</span>
  </button>
</div>


                        <div id="signupStep1">
                            <asp:TextBox ID="txtFullName" runat="server" CssClass="form-control mb-2" Placeholder="Full Name" />
                            <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control mb-2" Placeholder="Email" />
                            <asp:TextBox ID="txtPhone" runat="server" CssClass="form-control mb-2" Placeholder="Phone Number" />
                            <asp:TextBox ID="txtSignupUsername" runat="server" CssClass="form-control mb-2" Placeholder="Username" />
                            <asp:TextBox ID="txtSignupPassword" runat="server" TextMode="Password" CssClass="form-control mb-2"
                                Placeholder="Password" autocomplete="new-password" />

                            <asp:Button ID="btnSignup" runat="server" CssClass="btn btn-success" Text="Next"
                                OnClick="btnSignup_Click" OnClientClick="return validateAndSendOTP();" />

                            <asp:Label ID="lblSignupMsg" runat="server" CssClass="text-danger mt-2 d-block" />
                        </div>

                        <div id="signupStep2" style="display: none;">
                            <p class="small">We sent an OTP to your email. Enter it below to verify your account.</p>
                            <asp:TextBox ID="txtOTP" runat="server" CssClass="form-control mb-2" Placeholder="Enter OTP" />
                            <asp:Button ID="btnVerifyOTP" runat="server" CssClass="btn btn-primary" Text="Verify OTP" OnClick="btnVerifyOTP_Click" />
                            <asp:Label ID="lblOTPMsg" runat="server" CssClass="text-danger mt-2" />
                        </div>

                        <div id="signupStep3" style="display: none;">
                            <p class="small mb-2">
                                Please read and accept our Terms &amp; Conditions before creating your account.
                            </p>

                            <div class="border rounded p-2 mb-2" style="max-height: 160px; overflow: auto;">
                                <!-- Replace this with your real Terms text or a link -->
                                <strong>Terms &amp; Conditions</strong>
                                <ul class="mb-0">
                                    <li>You agree to provide accurate information.</li>
                                    <li>You agree to follow community rules.</li>
                                    <li>We may contact you for verification/account purposes.</li>
                                </ul>
                            </div>

                            <asp:CheckBox ID="chkTerms" runat="server" />
                            <label for="<%= chkTerms.ClientID %>" class="ms-1">
                                I agree to the Terms &amp; Conditions
                            </label>

                            <asp:Label ID="lblTermsMsg" runat="server" CssClass="text-danger mt-2 d-block" />

                            <asp:Button ID="btnCreateAccount" runat="server" CssClass="btn btn-success mt-2"
                                Text="Create Account" OnClick="btnCreateAccount_Click" OnClientClick="return validateTermsStep();" />
                        </div>


                    </div>
                </div>
                
</div>
            </div>

        </div>
    </div>
</div>

<script>
    function setStep(activeStep) {
        const s1 = document.getElementById('signupStep1');
        const s2 = document.getElementById('signupStep2');
        const s3 = document.getElementById('signupStep3');

        const i1 = document.getElementById('step1Indicator');
        const i2 = document.getElementById('step2Indicator');
        const i3 = document.getElementById('step3Indicator');

        if (!s1 || !s2 || !s3 || !i1 || !i2 || !i3) return;

        s1.style.display = (activeStep === 1) ? 'block' : 'none';
        s2.style.display = (activeStep === 2) ? 'block' : 'none';
        s3.style.display = (activeStep === 3) ? 'block' : 'none';

        [i1, i2, i3].forEach(el => el.classList.remove('is-active', 'is-done'));

        if (activeStep === 1) i1.classList.add('is-active');
        if (activeStep === 2) { i1.classList.add('is-done'); i2.classList.add('is-active'); }
        if (activeStep === 3) { i1.classList.add('is-done'); i2.classList.add('is-done'); i3.classList.add('is-active'); }
    }

    function goToStep1() { setStep(1); }
    function goToStep2() { setStep(2); }
    function goToStep3() {
        const i3 = document.getElementById('step3Indicator');
        if (i3 && i3.classList.contains('is-locked')) return;
        setStep(3);
    }

    function unlockStep3() {
        const i3 = document.getElementById('step3Indicator');
        if (!i3) return;
        i3.classList.remove('is-locked');
        i3.style.cursor = 'pointer';
        i3.onclick = goToStep3;
    }

    function lockStep3() {
        const i3 = document.getElementById('step3Indicator');
        if (!i3) return;
        i3.classList.add('is-locked');
        i3.onclick = null;
        i3.style.cursor = 'default';
    }

    function initSignupUi() {
        lockStep3();
        setStep(1);
    }

    document.addEventListener('DOMContentLoaded', initSignupUi);

    // if you're using UpdatePanel, this makes it work after postback too
    if (window.Sys && Sys.WebForms && Sys.WebForms.PageRequestManager) {
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(initSignupUi);
    }
</script>