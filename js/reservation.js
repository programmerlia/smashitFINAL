/* reservation.js (FULL) */

const SafeStore = (() => {
    let mem = {};
    function canUse() {
        try {
            const k = "__t";
            sessionStorage.setItem(k, "1");
            sessionStorage.removeItem(k);
            return true;
        } catch { return false; }
    }
    const ok = canUse();

    return {
        get(key) {
            try { return ok ? sessionStorage.getItem(key) : (mem[key] ?? null); }
            catch { return mem[key] ?? null; }
        },
        set(key, val) {
            try { ok ? sessionStorage.setItem(key, val) : (mem[key] = val); }
            catch { mem[key] = val; }
        },
        remove(key) {
            try { ok ? sessionStorage.removeItem(key) : delete mem[key]; }
            catch { delete mem[key]; }
        }
    };
})();


// MUST be global so ASPX OnClientClick can call it
window.resetReservationUI = function resetReservationUI() {
    // clear saved flow/cart so it won’t auto-resume
    try {
        sessionStorage.removeItem("pendingBooking");
        sessionStorage.removeItem("rentalCart");
        sessionStorage.removeItem("isPaying");
    } catch { }

    // hide the reservation section
    const section = document.getElementById("reservationSection");
    if (section) section.style.display = "none";

    // hide all steps
    ["dateSelectionSection", "rentalSelectionSection", "infoSection"].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.style.display = "none";
    });

    // reset step indicator
    document.querySelectorAll(".step-item").forEach(x => x.classList.remove("active"));
    const first = document.querySelector(".step-item");
    if (first) first.classList.add("active");

    // clear textboxes (use resConfig ids)
    const ids = window.resConfig?.ids || {};
    const clearById = (domId) => { const el = document.getElementById(domId); if (el) el.value = ""; };

    if (ids.txtFirstname) clearById(ids.txtFirstname);
    if (ids.txtLastname) clearById(ids.txtLastname);
    if (ids.txtEmail) clearById(ids.txtEmail);
    if (ids.txtContact) clearById(ids.txtContact);

    // reset dropdowns
    if (ids.ddlSport) {
        const s = document.getElementById(ids.ddlSport);
        if (s) s.value = "";
    }
    if (ids.ddlDuration) {
        const d = document.getElementById(ids.ddlDuration);
        if (d) d.value = "1";
    }

    // reset players
    const np = document.getElementById("numPlayers");
    if (np) np.value = "1";

    // clear file upload (replace node)
    const fu = document.querySelector('input[type="file"]');
    if (fu && fu.parentNode) {
        const clone = fu.cloneNode(true);
        fu.parentNode.replaceChild(clone, fu);
    }

    // clear summaries
    const setText = (id, val) => { const el = document.getElementById(id); if (el) el.textContent = val; };
    setText("courtSummaryCourt", "---");
    setText("courtSummarySport", "---");
    setText("courtSummaryTime", "---");
    setText("courtSummaryDuration", "---");
    setText("totalPrice", "₱ 0");
    setText("rentalsTotal", "₱ 0");
    setText("totalFinal", "₱ 0");
    setText("summary-totalPrice", "");
    setText("summary-totalPriceInfo", "");
    setText("summaryCourt", "---");
    setText("summarySport", "---");
    setText("summaryTime", "---");
    setText("summaryDuration", "---");
    setText("summaryPlayers", "1");
    setText("summaryFirstname", "------");
    setText("summaryLastname", "------");
    setText("summaryContact", "------");
    setText("summaryEmail", "------");

    const cartItems = document.getElementById("cartItems");
    if (cartItems) cartItems.innerHTML = "";

    // clear label
    if (window.lblSelectedSlotClientID) {
        const lbl = document.getElementById(window.lblSelectedSlotClientID);
        if (lbl) lbl.innerText = "No slot selected.";
    }

    // clear hidden fields
    const clearHF = (id) => { const el = document.getElementById(id); if (el) el.value = ""; };
    clearHF("hfSelectedCourtID");
    clearHF("hfSelectedDate");
    if (window.hfCourtIDClientID) clearHF(window.hfCourtIDClientID);
    if (window.hfResDateClientID) clearHF(window.hfResDateClientID);
    if (window.hfStartTimeClientID) clearHF(window.hfStartTimeClientID);
    if (window.hfEndTimeClientID) clearHF(window.hfEndTimeClientID);
    clearHF("hfRentalCart");
    clearHF("hfRentalItems");
    clearHF("hfRentalStock");

    // close modal
    const modal = document.getElementById("paymentModal");
    if (modal) modal.style.display = "none";

    location.reload();
};


(function () {
    "use strict";

    const cfg = window.resConfig || {};
    const ids = cfg.ids || {};
    const urls = cfg.urls || {};
    const sessionUser = cfg.sessionUser || {};
    const isLoggedIn = !!cfg.isLoggedIn;

    // --- DOM helpers ---
    const $ = (id) => document.getElementById(id);
    const $id = (key) => document.getElementById(ids[key]);

    // --- Hidden fields (server expects these exact ones) ---
    const HF = {
        // from global script in aspx:
        courtID: () => document.getElementById(window.hfCourtIDClientID),
        resDate: () => document.getElementById(window.hfResDateClientID),
        start: () => document.getElementById(window.hfStartTimeClientID),
        end: () => document.getElementById(window.hfEndTimeClientID),
        lblSlot: () => document.getElementById(window.lblSelectedSlotClientID),

        // server-side static hidden fields:
        selectedDate: () => $("hfSelectedDate"),
        selectedCourtID: () => $("hfSelectedCourtID"),

        // rentals postback:
        rentalCart: () => $("hfRentalCart")
    };

    // --- data loaded from hiddenfields JSON ---
    let courts = [];
    let queues = [];
    let lastEquipmentRows = [];

    // --- storage keys ---
    const KEY_BOOKING = "pendingBooking";  // step, date, courtId, start, end, sport, duration, players, info
    const KEY_CART = "rentalCart";         // rental cart JSON

    // =========================
    // Time helpers
    // =========================
    function addMinutes(timeHHMM, minutesToAdd) {
        const [hh, mm] = (timeHHMM || "00:00").split(":").map(Number);
        const d = new Date();
        d.setHours(hh || 0, mm || 0, 0, 0);
        d.setMinutes(d.getMinutes() + (minutesToAdd || 0));
        return d.toTimeString().slice(0, 5);
    }

    function formatTime12Hour(time24) {
        const [hour, minute] = (time24 || "0:0").split(":").map(Number);
        const period = hour >= 12 ? "PM" : "AM";
        const hour12 = hour % 12 || 12;
        return `${hour12}:${String(minute || 0).padStart(2, "0")} ${period}`;
    }

    function getDurationHours() {
        return parseInt($id("ddlDuration")?.value || "1", 10) || 1;
    }

    function getSelectedSport() {
        return String($id("ddlSport")?.value || "").toLowerCase();
    }

    // =========================
    // Step UI
    // =========================
    function showStep(step) {
        const s1 = $("dateSelectionSection");
        const s2 = $("rentalSelectionSection"); // IMPORTANT: your Step2 DIV must be this ID
        const s3 = $("infoSection");

        if (s1) s1.style.display = (step === 1 ? "flex" : "none");
        if (s2) s2.style.display = (step === 2 ? "flex" : "none");
        if (s3) s3.style.display = (step === 3 ? "flex" : "none");

        const steps = document.querySelectorAll(".step-item");
        steps.forEach(x => x.classList.remove("active"));
        if (steps[step - 1]) steps[step - 1].classList.add("active");
    }

    function showReservation() {
        const resSection = $("reservationSection");
        if (resSection) resSection.style.display = "flex";

        // disable hero button for this session
        const btn = $id("btnReserveNow");
        if (btn) {
            btn.innerText = "Booking in progress...";
            btn.style.backgroundColor = "#6c757d";
            btn.style.color = "white";
            btn.disabled = true;
        }

        showStep(1);
        resSection?.scrollIntoView({ behavior: "smooth" });
    }
    window.showReservation = showReservation;

    // =========================
    // Selection model (single source of truth)
    // =========================
    function getSelection() {
        return {
            sport: getSelectedSport(),
            duration: String(getDurationHours()),
            date: HF.selectedDate()?.value || HF.resDate()?.value || "",
            courtId: HF.selectedCourtID()?.value || HF.courtID()?.value || "",
            start: HF.start()?.value || "",
            end: HF.end()?.value || "",
            players: $("numPlayers")?.value || "1",
            firstname: $id("txtFirstname")?.value || "",
            lastname: $id("txtLastname")?.value || "",
            email: $id("txtEmail")?.value || "",
            contact: $id("txtContact")?.value || ""
        };
    }

    function setSelection(sel) {
        // date
        if (HF.selectedDate()) HF.selectedDate().value = sel.date || "";
        if (HF.resDate()) HF.resDate().value = sel.date || "";

        // court
        if (HF.selectedCourtID()) HF.selectedCourtID().value = sel.courtId || "";
        if (HF.courtID()) HF.courtID().value = sel.courtId || "";

        // start/end
        if (HF.start()) HF.start().value = sel.start || "";
        if (HF.end()) HF.end().value = sel.end || "";

        // dropdowns
        if ($id("ddlSport") && sel.sport) $id("ddlSport").value = sel.sport;
        if ($id("ddlDuration") && sel.duration) $id("ddlDuration").value = sel.duration;

        // players
        if ($("numPlayers") && sel.players) $("numPlayers").value = sel.players;

        // info inputs (don’t overwrite if user already typed)
        if ($id("txtFirstname") && sel.firstname && !$id("txtFirstname").value) $id("txtFirstname").value = sel.firstname;
        if ($id("txtLastname") && sel.lastname && !$id("txtLastname").value) $id("txtLastname").value = sel.lastname;
        if ($id("txtEmail") && sel.email && !$id("txtEmail").value) $id("txtEmail").value = sel.email;
        if ($id("txtContact") && sel.contact && !$id("txtContact").value) $id("txtContact").value = sel.contact;
    }

    function saveState(step) {
        const sel = getSelection();
        const data = { ...sel, step: String(step || "1") };
        SafeStore.set(KEY_BOOKING, JSON.stringify(data));
    }

    function restoreState() {
        const raw = SafeStore.get(KEY_BOOKING);
        if (!raw) return null;
        try {
            const data = JSON.parse(raw);
            setSelection(data);
            return data;
        } catch {
            return null;
        }
    }

    // =========================
    // Timetable clicking (Step 1)
    // =========================
    function clearSlotVisuals() {
        document.querySelectorAll(".slot.selected, .slot.selected-range")
            .forEach(x => x.classList.remove("selected", "selected-range"));
    }

    function applySportFilterToTimeTable() {
        const selected = getSelectedSport();

        document.querySelectorAll(".slot").forEach(el => {
            const courtId = el.dataset.court;
            if (!courtId) return;

            const court = (courts || []).find(x => String(x.CourtID) === String(courtId));
            const sport = String(court?.SportName || "").toLowerCase();

            if (!selected) return;

            if (sport && sport !== selected && el.classList.contains("reservable")) {
                el.classList.add("sport-disabled");
            }
        });

        if (!selected) {
            const cur = getSelection();
            if (cur.courtId || cur.start || cur.end) {
                setSelection({ ...cur, courtId: "", start: "", end: "" });
                clearSlotVisuals();
                if (HF.lblSlot()) HF.lblSlot().textContent = "No slot selected.";
                syncSummaries();
                saveState(1);
            }
            return;
        }

        const cur = getSelection();
        if (cur.courtId) {
            const court = (courts || []).find(x => String(x.CourtID) === String(cur.courtId));
            const sport = String(court?.SportName || "").toLowerCase();
            if (sport && sport !== selected) {
                setSelection({ ...cur, courtId: "", start: "", end: "" });
                clearSlotVisuals();
                if (HF.lblSlot()) HF.lblSlot().textContent = "No slot selected.";
                syncSummaries();
                saveState(1);
            }
        }
    }

    function initTimeTableClicks() {
        const shell = document.querySelector(".timetable-shell");
        if (!shell) return;

        // prevent double-binding even after partial postback
        if (shell.dataset.bound === "1") return;
        shell.dataset.bound = "1";

        shell.addEventListener("click", function (e) {
            const el = e.target.closest(".slot.reservable");
            if (!el) return;
            if (el.classList.contains("sport-disabled")) return;

            const date = el.dataset.date || HF.selectedDate()?.value || "";
            const start = el.dataset.start || "";
            const courtId = el.dataset.court || "";
            const courtNum = el.dataset.courtnum || "";
            const durH = getDurationHours();

            if (!date || !start || !courtId) return;

            // validate continuous range for duration (30-min slots)
            const slotsNeeded = Math.max(1, durH * 2);
            const rangeEls = [];

            for (let i = 0; i < slotsNeeded; i++) {
                const t = addMinutes(start, i * 30);
                const q = `.slot[data-court="${courtId}"][data-date="${date}"][data-start="${t}"]`;
                const cell = document.querySelector(q);

                if (!cell) { alert("That duration doesn't fit (missing slot)."); return; }
                if (!cell.classList.contains("reservable")) { alert("That duration overlaps blocked/queue."); return; }
                if (cell.classList.contains("sport-disabled")) { alert("Sport mismatch."); return; }

                rangeEls.push(cell);
            }

            const end = addMinutes(start, durH * 60);

            clearSlotVisuals();
            rangeEls.forEach((cell, idx) => cell.classList.add(idx === 0 ? "selected" : "selected-range"));

            // store into hidden fields (server + client use these)
            const sel = getSelection();
            setSelection({ ...sel, date, courtId, start, end });

            if (HF.lblSlot()) {
                HF.lblSlot().textContent = `Selected: ${date} | Court ${courtNum} | ${start} - ${end} (${durH}h)`;
            }

            // update everything dependent
            syncSummaries();
            loadAndRenderEquipment(); // rentals availability depends on date/start/duration
            saveState(1);
        });
    }

    function reselectSlotFromHidden() {
        const cur = getSelection();
        if (!cur.courtId || !cur.date || !cur.start) return;

        // apply selected+range visuals based on duration
        const durH = getDurationHours();
        const slotsNeeded = Math.max(1, durH * 2);

        clearSlotVisuals();
        for (let i = 0; i < slotsNeeded; i++) {
            const t = addMinutes(cur.start, i * 30);
            const q = `.slot[data-court="${cur.courtId}"][data-date="${cur.date}"][data-start="${t}"]`;
            const cell = document.querySelector(q);
            if (!cell) break;
            cell.classList.add(i === 0 ? "selected" : "selected-range");
        }
    }

    // =========================
    // Rentals (Step 2)
    // =========================
    function getRentalCart() {
        try { return JSON.parse(SafeStore.get(KEY_CART) || "{}"); }
        catch { return {}; }
    }

    function setRentalCart(cart) {
        SafeStore.set(KEY_CART, JSON.stringify(cart || {}));
        if (HF.rentalCart()) HF.rentalCart().value = SafeStore.get(KEY_CART) || "{}";
    }

    function itemKey(type, spec) { return `${type}||${spec || ""}`; }
    function displayName(type, spec) { return spec ? `${type} (${spec})` : type; }
    function escapeQuotes(s) { return String(s).replace(/\\/g, "\\\\").replace(/'/g, "\\'"); }
    function cssSafe(s) { return String(s).replace(/[^a-zA-Z0-9_]/g, "_"); }

    function fetchEquipmentAvailability(date, startTime, durationHours) {
        if (!urls.equipmentAvailability) return Promise.resolve([]);
        return fetch(urls.equipmentAvailability, {
            method: "POST",
            headers: { "Content-Type": "application/json; charset=utf-8" },
            body: JSON.stringify({ date, startTime, durationHours })
        })
            .then(r => r.json())
            .then(res => res.d || [])
            .catch(() => []);
    }

    function renderCartUI(stockRows) {
        const cartDiv = $("cartItems");
        const totalEl = $("rentalsTotal");
        if (!cartDiv || !totalEl) return;

        const cart = getRentalCart();
        const stockMap = {};
        (stockRows || []).forEach(r => {
            const key = itemKey(r.EquipmentType, r.EquipmentSpec);
            stockMap[key] = {
                price: Number(r.RentalPrice || 0),
                avail: Number(r.AvailableQty || 0),
                type: r.EquipmentType,
                spec: r.EquipmentSpec || ""
            };
        });

        // clamp cart
        let changed = false;
        Object.keys(cart).forEach(k => {
            const avail = stockMap[k]?.avail ?? 0;
            if (cart[k] > avail) { cart[k] = avail; changed = true; }
            if (cart[k] <= 0 || avail <= 0) { delete cart[k]; changed = true; }
        });
        if (changed) setRentalCart(cart);

        const keys = Object.keys(cart);
        if (keys.length === 0) {
            cartDiv.innerHTML = `<div class="text-muted">No rental items yet.</div>`;
            totalEl.innerText = "₱ 0.00";
            syncSummaries();
            return;
        }

        let total = 0;
        cartDiv.innerHTML = keys.map(k => {
            const qty = Number(cart[k] || 0);
            const row = stockMap[k];
            const price = row?.price ?? 0;
            total += qty * price;
            const name = row ? displayName(row.type, row.spec) : k;

            return `
        <div class="cart-line">
          <div>
            <div class="cart-name">${name}</div>
            <div class="cart-sub">₱ ${price.toFixed(2)} x ${qty}</div>
          </div>
          <div class="cart-controls">
            <button type="button" class="cart-btn" onclick="cartDec('${escapeQuotes(k)}')">−</button>
            <div class="fw-bold" style="width:18px;text-align:center;">${qty}</div>
            <button type="button" class="cart-btn" onclick="cartInc('${escapeQuotes(k)}')">+</button>
            <button type="button" class="cart-x" onclick="cartRemove('${escapeQuotes(k)}')">X</button>
          </div>
        </div>
      `;
        }).join("");

        totalEl.innerText = "₱ " + total.toFixed(2);
        syncSummaries();
    }

    // inline handlers
    window.cartInc = (key) => {
        const c = getRentalCart();
        c[key] = (c[key] || 0) + 1;
        setRentalCart(c);
        loadAndRenderEquipment();
    };
    window.cartDec = (key) => {
        const c = getRentalCart();
        c[key] = (c[key] || 0) - 1;
        if (c[key] <= 0) delete c[key];
        setRentalCart(c);
        loadAndRenderEquipment();
    };
    window.cartRemove = (key) => {
        const c = getRentalCart();
        delete c[key];
        setRentalCart(c);
        loadAndRenderEquipment();
    };

    function getPreviewQty(key) {
        const el = $("qty_" + cssSafe(key));
        return el ? parseInt(el.innerText || "1", 10) : 1;
    }
    function setPreviewQty(key, v) {
        const el = $("qty_" + cssSafe(key));
        if (el) el.innerText = String(v);
    }
    window.previewInc = (key, avail, inCart) => {
        let q = getPreviewQty(key);
        const remaining = Math.max(0, Number(avail || 0) - Number(inCart || 0));
        if (q < remaining) q++;
        setPreviewQty(key, q);
    };
    window.previewDec = (key) => {
        let q = getPreviewQty(key);
        if (q > 1) q--;
        setPreviewQty(key, q);
    };
    window.addToCart = (key, avail) => {
        const cart = getRentalCart();
        const inCart = Number(cart[key] || 0);
        const addQty = getPreviewQty(key);
        const remaining = Math.max(0, Number(avail || 0) - inCart);
        if (remaining <= 0) { alert("No more stock available for this item."); return; }
        cart[key] = inCart + Math.min(addQty, remaining);
        setRentalCart(cart);
        loadAndRenderEquipment();
    };

    function renderEquipmentProducts(rows) {
        lastEquipmentRows = Array.isArray(rows) ? rows : [];
        const div = $("equipmentContainer");
        if (!div) return;

        if (!rows || rows.length === 0) {
            div.innerHTML = `<div class="text-muted small">No rental items available.</div>`;
            renderCartUI(rows);
            return;
        }

        const cart = getRentalCart();

        div.innerHTML = rows.map(r => {
            const type = r.EquipmentType;
            const spec = r.EquipmentSpec || "";
            const key = itemKey(type, spec);

            const price = Number(r.RentalPrice || 0);
            const avail = Number(r.AvailableQty || 0);
            const inCart = Number(cart[key] || 0);
            const name = displayName(type, spec);

            const badgeClass = avail <= 0 ? "out" : (avail <= 2 ? "low" : "");
            const badgeText = avail <= 0 ? "OUT" : (avail <= 2 ? "LOW" : "AVAILABLE");

            return `
        <div class="col-12 col-md-6">
          <div class="rental-card">
            <div class="rental-badge ${badgeClass}">${badgeText}</div>

            <div class="rental-head">
              <div style="min-width:0;">
                <div class="rental-title">${name}</div>
                <div class="rental-price">₱ ${price.toFixed(2)}</div>
                <div class="rental-stock"><b>${avail}</b> available</div>
              </div>

              <div class="rental-actions">
                <div class="qty-stepper">
                  <button type="button" onclick="previewDec('${escapeQuotes(key)}')">−</button>
                  <div class="qty-val" id="qty_${cssSafe(key)}">1</div>
                  <button type="button" onclick="previewInc('${escapeQuotes(key)}', ${avail}, ${inCart})">+</button>
                </div>

                <button type="button" class="btn-add-rental"
                        onclick="addToCart('${escapeQuotes(key)}', ${avail})"
                        ${avail <= 0 ? "disabled" : ""}>
                  ADD TO CART
                </button>
              </div>
            </div>
          </div>
        </div>
      `;
        }).join("");

        renderCartUI(rows);
    }

    function loadAndRenderEquipment() {
        const sel = getSelection();
        const date = sel.date;
        const start = sel.start;
        const dur = getDurationHours();

        // If user hasn’t selected slot yet, show “select slot first”
        if (!date || !start) {
            const div = $("equipmentContainer");
            if (div) div.innerHTML = `<div class="text-muted small">Select a time slot first to load rental availability.</div>`;
            renderCartUI([]);
            return;
        }

        fetchEquipmentAvailability(date, start, dur).then(renderEquipmentProducts);
    }
    window.loadAndRenderEquipment = loadAndRenderEquipment;

    // =========================
    // Totals + summaries (ALWAYS keep in sync)
    // =========================
    function getCourtTotal() {
        const dur = getDurationHours();
        const pricePerHour = 330;
        return dur * pricePerHour;
    }

    function getRentalsTotalFromUI() {
        const t = $("rentalsTotal")?.innerText || "₱ 0";
        return Number((t.replace(/[^\d.]/g, "")) || 0);
    }

    function syncSummaries() {
        const sel = getSelection();

        // compute court display
        const court = (courts || []).find(c => String(c.CourtID) === String(sel.courtId));
        const courtNum = court ? court.CourtNumber : "---";
        const sport = court ? court.SportName : (sel.sport ? sel.sport : "---");

        const timeRange = (sel.start && sel.end)
            ? `${formatTime12Hour(sel.start)} - ${formatTime12Hour(sel.end)}`
            : "---";

        const durTxt = `${getDurationHours()} Hour${getDurationHours() > 1 ? "s" : ""}`;

        // Step2 summary (courtSummary*)
        if ($("courtSummaryCourt")) $("courtSummaryCourt").innerText = sel.courtId ? `Court ${courtNum}` : "---";
        if ($("courtSummarySport")) $("courtSummarySport").innerText = sel.courtId ? sport : "---";
        if ($("courtSummaryTime")) $("courtSummaryTime").innerText = sel.start ? timeRange : "---";
        if ($("courtSummaryDuration")) $("courtSummaryDuration").innerText = durTxt;

        // Step3 summary (summary*)
        if ($("summaryCourt")) $("summaryCourt").innerText = sel.courtId ? `Court ${courtNum}` : "---";
        if ($("summarySport")) $("summarySport").innerText = sel.courtId ? sport : "---";
        if ($("summaryTime")) $("summaryTime").innerText = sel.start ? timeRange : "---";
        if ($("summaryDuration")) $("summaryDuration").innerText = durTxt;

        // players
        if ($("summaryPlayers")) $("summaryPlayers").innerText = sel.players || "1";

        // totals
        const courtTotal = getCourtTotal();
        const rentalsTotal = getRentalsTotalFromUI();
        const grand = courtTotal + rentalsTotal;

        if ($("totalPrice")) $("totalPrice").innerText = "₱ " + courtTotal.toFixed(2);
        if ($("totalFinal")) $("totalFinal").innerText = "₱ " + grand.toFixed(2);
        if ($("summary-totalPrice")) $("summary-totalPrice").innerText = "₱ " + grand.toFixed(2);
     
        // user summary text (Step3)
        if ($("summaryFirstname")) $("summaryFirstname").innerText = $id("txtFirstname")?.value || "------";
        if ($("summaryLastname")) $("summaryLastname").innerText = $id("txtLastname")?.value || "------";
        if ($("summaryEmail")) $("summaryEmail").innerText = $id("txtEmail")?.value || "------";
        if ($("summaryContact")) $("summaryContact").innerText = $id("txtContact")?.value || "------";
        if ($("summary-totalPriceInfo")) $("summary-totalPriceInfo").innerText = "₱ " + grand.toFixed(2);
        // keep hidden rentalCart up to date for postback
        if (HF.rentalCart()) HF.rentalCart().value = SafeStore.get(KEY_CART) || "{}";
    }

    // =========================
    // Navigation (single back to rentals)
    // =========================
    function ensureSlotSelectedOrAlert() {
        const sel = getSelection();
        if (!sel.date || !sel.courtId || !sel.start || !sel.end) {
            alert("Please select a court time slot first.");
            return false;
        }
        return true;
    }

    function goToRentals() {
        if (!ensureSlotSelectedOrAlert()) return;
        showStep(2);
        loadAndRenderEquipment();
        syncSummaries();
        saveState(2);
        $("reservationSection")?.scrollIntoView({ behavior: "smooth" });
    }
    window.goToRentals = goToRentals;

    function goBackToDate() {
        showStep(1);
        syncSummaries();
        saveState(1);
        $("reservationSection")?.scrollIntoView({ behavior: "smooth" });
    }
    window.goBackToDate = goBackToDate;

    function goToInfoSection() {
        if (!ensureSlotSelectedOrAlert()) return false;

        saveState(3);

        if (!isLoggedIn) {
            if (typeof window.checkAccount === "function") window.checkAccount(window.location.href);
            return false;
        }

        showStep(3);
        fillUserInfoIfEmpty();
        syncSummaries();
        bindInfoInputsOnce();
        $("reservationSection")?.scrollIntoView({ behavior: "smooth" });
        return false;
    }
    window.goToInfoSection = goToInfoSection;

    function goBackToRentals() {
        showStep(2);
        loadAndRenderEquipment();
        syncSummaries();
        saveState(2);
        $("reservationSection")?.scrollIntoView({ behavior: "smooth" });
    }
    window.goBackToRentals = goBackToRentals;

    // =========================
    // Player info auto-fill + live binding
    // =========================
    function fillUserInfoIfEmpty() {
        const fn = $id("txtFirstname");
        const ln = $id("txtLastname");
        const e = $id("txtEmail");
        const p = $id("txtContact");

        if (fn && !fn.value) fn.value = sessionUser.firstname || "";
        if (ln && !ln.value) ln.value = sessionUser.lastname || "";
        if (e && !e.value) e.value = sessionUser.email || "";
        if (p && !p.value) p.value = sessionUser.phone || "";
    }

    function bindInfoInputsOnce() {
        const inputs = [$id("txtLastname"), $id("txtFirstname"), $id("txtEmail"), $id("txtContact")];
        inputs.forEach(i => {
            if (!i) return;
            if (i.dataset.bound === "1") return;
            i.dataset.bound = "1";
            i.addEventListener("input", () => {
                syncSummaries();
                saveState(3);
            });
        });
    }

    // =========================
    // Payment modal (includes rentals list)
    // =========================
    function renderPaymentRentals() {
        const listEl = $("pmRentals");
        const totalEl = $("pmRentalsTotal");
        if (!listEl || !totalEl) return;

        const cart = getRentalCart();
        const keys = Object.keys(cart);

        const stockMap = {};
        (lastEquipmentRows || []).forEach(r => {
            const key = itemKey(r.EquipmentType, r.EquipmentSpec);
            stockMap[key] = {
                name: displayName(r.EquipmentType, r.EquipmentSpec || ""),
                price: Number(r.RentalPrice || 0)
            };
        });

        if (keys.length === 0) {
            listEl.classList.add("text-muted");
            listEl.innerHTML = "No rental items yet.";
            totalEl.innerText = "₱ 0.00";
            return;
        }

        let total = 0;
        listEl.classList.remove("text-muted");
        listEl.innerHTML = keys.map(k => {
            const qty = Number(cart[k] || 0);
            const item = stockMap[k];
            const name = item?.name || k;
            const price = item?.price ?? 0;
            const line = qty * price;
            total += line;

            return `
        <div style="display:flex; justify-content:space-between; gap:10px; margin:4px 0;">
          <span>${name} × ${qty}</span>
          <span>₱ ${line.toFixed(2)}</span>
        </div>
      `;
        }).join("");

        totalEl.innerText = "₱ " + total.toFixed(2);
    }

    function openPaymentModal() {
        const modal = $("paymentModal");
        if (!modal) return;

        syncSummaries();

        const sel = getSelection();

        if ($("pmCourt")) $("pmCourt").innerText = $("summaryCourt")?.innerText || "---";
        if ($("pmSport")) $("pmSport").innerText = $("summarySport")?.innerText || "---";
        if ($("pmDate")) $("pmDate").innerText = sel.date || "---";
        if ($("pmTime")) $("pmTime").innerText = $("summaryTime")?.innerText || "---";
        if ($("pmDuration")) $("pmDuration").innerText = $("summaryDuration")?.innerText || "---";
        if ($("pmPlayers")) $("pmPlayers").innerText = $("summaryPlayers")?.innerText || "---";
        if ($("pmTotal")) $("pmTotal").innerText = $("summary-totalPrice")?.innerText || "---";

        renderPaymentRentals();
        modal.style.display = "block";
    }
    window.openPaymentModal = openPaymentModal;

    window.closePaymentModal = function () {
        const modal = $("paymentModal");
        if (modal) modal.style.display = "none";
    };

    window.confirmAndPay = function () {
        if (HF.rentalCart()) HF.rentalCart().value = SafeStore.get(KEY_CART) || "{}";

        const sel = getSelection();
        if (!sel.date || !sel.courtId || !sel.start || !sel.duration) {
            alert("Please complete your slot details first.");
            return false;
        }

        saveState(3);
        SafeStore.set("isPaying", "1");

        window.closePaymentModal();

        __doPostBack(window.resConfig.ids.btnSubmitReservationUnique, "");
        return false;
    };

    window.onBookReservationClick = function () {
        if (!ensureSlotSelectedOrAlert()) return false;

        saveState(3);

        if (!isLoggedIn) {
            if (typeof window.checkAccount === "function") window.checkAccount(window.location.href);
            return false;
        }

        openPaymentModal();
        return false;
    };

    // =========================
    // UpdatePanel hook (ASP.NET partial postbacks)
    // =========================
    function wireUpdatePanelHookOnce() {
        if (!(window.Sys && Sys.WebForms && Sys.WebForms.PageRequestManager)) return;
        const prm = Sys.WebForms.PageRequestManager.getInstance();
        if (prm._reservationHooked) return;
        prm._reservationHooked = true;

        prm.add_endRequest(() => {
            try { courts = JSON.parse($("hfCourts")?.value || "[]"); } catch { courts = []; }
            try { queues = JSON.parse($("hfQueues")?.value || "[]"); } catch { queues = []; }

            const wasPaying = SafeStore.get("isPaying") === "1";
            if (wasPaying) {
                SafeStore.remove("isPaying");
                showStep(3);
            }

            const shell = document.querySelector(".timetable-shell");
            if (shell) shell.dataset.bound = "0";

            bindControlsOnce();

            initTimeTableClicks();
            applySportFilterToTimeTable();
            reselectSlotFromHidden();

            loadAndRenderEquipment();
            syncSummaries();
        });
    }

    // =========================
    // Bind dropdowns + players
    // =========================
    function bindControlsOnce() {
        const sport = $id("ddlSport");
        if (sport && sport.dataset.bound !== "1") {
            sport.dataset.bound = "1";
            sport.addEventListener("change", () => {
                const cur = getSelection();
                setSelection({ ...cur, courtId: "", start: "", end: "" });
                clearSlotVisuals();
                if (HF.lblSlot()) HF.lblSlot().textContent = "No slot selected.";
                saveState(1);

                const tgt = ids.ddlSportUnique;
                if (typeof __doPostBack === "function" && tgt) {
                    __doPostBack(tgt, "");
                    return;
                }

                applySportFilterToTimeTable();
                syncSummaries();
            });
        }
        const hfSport = document.getElementById(window.hfSelectedSportClientID);
        if (hfSport) hfSport.value = getSelectedSport(); // "badminton"/"pickleball"/""


        const dur = $id("ddlDuration");
        if (dur && dur.dataset.bound !== "1") {
            dur.dataset.bound = "1";
            dur.addEventListener("change", () => {
                // duration affects end time and range validity
                const sel = getSelection();
                if (sel.start) {
                    const end = addMinutes(sel.start, getDurationHours() * 60);
                    setSelection({ ...sel, end });
                    reselectSlotFromHidden();
                }
                loadAndRenderEquipment();
                syncSummaries();
                saveState(getCurrentStep());
            });
        }

        const np = $("numPlayers");
        if (np && np.dataset.bound !== "1") {
            np.dataset.bound = "1";
            np.addEventListener("input", () => {
                syncSummaries();
                saveState(getCurrentStep());
            });
        }
    }

    // ✅ FIXED: don’t compare to "flex" (your showStep uses "block")
    function getCurrentStep() {
        const s3 = $("infoSection");
        const s2 = $("rentalSelectionSection");
        if (s3 && s3.style.display !== "none") return 3;
        if (s2 && s2.style.display !== "none") return 2;
        return 1;
    }

    // =========================
    // Resume flow
    // =========================
    function resumeToSavedStep() {
        const data = restoreState();
        if (!data) return;

        showReservation();

        const step = parseInt(data.step || "1", 10) || 1;

        showStep(step);

        applySportFilterToTimeTable();
        reselectSlotFromHidden();
        loadAndRenderEquipment();
        fillUserInfoIfEmpty();
        bindInfoInputsOnce();
        syncSummaries();
    }

    function resetInitialNoPreselect() {
        const sport = $id("ddlSport");
        if (sport) sport.value = "";

        const cur = getSelection();
        setSelection({ ...cur, courtId: "", start: "", end: "" });

        clearSlotVisuals();
        if (HF.lblSlot()) HF.lblSlot().textContent = "No slot selected.";

        const div = $("equipmentContainer");
        if (div) div.innerHTML = `<div class="text-muted small">Select a time slot first to load rental availability.</div>`;
        renderCartUI([]);

        syncSummaries();
        saveState(1);
    }



    // =========================
    // Init
    // =========================
    function initOnce() {
        if (window.__reservationInitDone) return;
        window.__reservationInitDone = true;

        try { courts = JSON.parse($("hfCourts")?.value || "[]"); } catch { courts = []; }
        try { queues = JSON.parse($("hfQueues")?.value || "[]"); } catch { queues = []; }

        wireUpdatePanelHookOnce();
        bindControlsOnce();
        initTimeTableClicks();

        const hfSportInit = document.getElementById(window.hfSelectedSportClientID);
        if (hfSportInit) hfSportInit.value = getSelectedSport();

        if (HF.rentalCart()) HF.rentalCart().value = SafeStore.get(KEY_CART) || "{}";

        const raw = SafeStore.get(KEY_BOOKING);
        if (raw) {
            resumeToSavedStep();
        } else {
            resetInitialNoPreselect();
            applySportFilterToTimeTable();
        }

        syncSummaries();
    }

    function safeInit() {
        try { initOnce(); } catch (e) { console.error("Reservation init failed:", e); }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", safeInit);
    } else {
        safeInit();
    }

    if (window.Sys && Sys.Application) {
        Sys.Application.add_load(safeInit);
    }

})();


