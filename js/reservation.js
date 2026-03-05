/* reservation.js (REVAMPED + ALIGNED WITH YOUR C#) */


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

(function () {
    "use strict";

    // ----------------- CONFIG -----------------
    const cfg = window.resConfig || {};
    const ids = cfg.ids || {};
    const urls = cfg.urls || {};
    const sessionUser = cfg.sessionUser || {};
    const isLoggedIn = !!cfg.isLoggedIn;

    const KEY_BOOKING = "pendingBooking";
    const KEY_CART = "rentalCart";

    // ----------------- DOM HELPERS -----------------
    const $ = (id) => document.getElementById(id);
    const $id = (key) => document.getElementById(ids[key]);

    // Find element by: staticId OR window clientId var OR provided clientId string
    function byAnyId(staticId, clientIdOrWindowKey) {
        if (staticId) {
            const a = $(staticId);
            if (a) return a;
        }
        if (clientIdOrWindowKey) {
            const cid = (typeof clientIdOrWindowKey === "string" && clientIdOrWindowKey.indexOf("ClientID") >= 0)
                ? window[clientIdOrWindowKey]
                : clientIdOrWindowKey;

            if (cid) {
                const b = document.getElementById(cid);
                if (b) return b;
            }
        }
        return null;
    }

    const HF = {
        // server-generated client IDs (you already have these globals)
        courtID: () => byAnyId(null, "hfCourtIDClientID"),
        resDate: () => byAnyId(null, "hfResDateClientID"),
        start: () => byAnyId(null, "hfStartTimeClientID"),
        end: () => byAnyId(null, "hfEndTimeClientID"),
        lblSlot: () => byAnyId(null, "lblSelectedSlotClientID"),

        // these might be static OR might be rendered as ClientID
        selectedDate: () => byAnyId("hfSelectedDate", "hfSelectedDateClientID"),
        selectedCourtID: () => byAnyId("hfSelectedCourtID", "hfSelectedCourtIDClientID"),

        rentalCart: () => byAnyId("hfRentalCart", "hfRentalCartClientID")
    };

    // If you also have a hidden field for selected sport
    const hfSelectedSport = () => byAnyId(null, "hfSelectedSportClientID");

    // ----------------- STATE -----------------
    let courts = [];
    let queues = [];
    let lastEquipmentRows = [];

    // ----------------- TIME HELPERS -----------------
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
        return String($id("ddlSport")?.value || "").trim().toLowerCase();
    }

    // ----------------- SLOT LABEL (ALIGNED WITH C#) -----------------
    function setSlotLabel() {
        const lbl = HF.lblSlot();
        if (!lbl) return;

        const sport = getSelectedSport();
        const sel = getSelection();

        if (!sport) lbl.textContent = "No sport selected.";
        else if (!sel.courtId || !sel.start) lbl.textContent = "No slot selected.";
        // else leave whatever "Selected: ..." text is set by click handler
    }

    // ----------------- STEP UI -----------------
    function showStep(step) {
        const s1 = $("dateSelectionSection");
        const s2 = $("rentalSelectionSection");
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

    function getCurrentStep() {
        const s3 = $("infoSection");
        const s2 = $("rentalSelectionSection");
        if (s3 && s3.style.display !== "none") return 3;
        if (s2 && s2.style.display !== "none") return 2;
        return 1;
    }

    // ----------------- SELECTION (SINGLE SOURCE OF TRUTH) -----------------
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
        if (HF.selectedDate()) HF.selectedDate().value = sel.date || "";
        if (HF.resDate()) HF.resDate().value = sel.date || "";

        if (HF.selectedCourtID()) HF.selectedCourtID().value = sel.courtId || "";
        if (HF.courtID()) HF.courtID().value = sel.courtId || "";

        if (HF.start()) HF.start().value = sel.start || "";
        if (HF.end()) HF.end().value = sel.end || "";

        if ($id("ddlSport")) $id("ddlSport").value = sel.sport ?? $id("ddlSport").value;
        if ($id("ddlDuration")) $id("ddlDuration").value = sel.duration ?? $id("ddlDuration").value;

        if ($("numPlayers") && sel.players) $("numPlayers").value = sel.players;

        // only fill if empty (keeps user input)
        if ($id("txtFirstname") && sel.firstname && !$id("txtFirstname").value) $id("txtFirstname").value = sel.firstname;
        if ($id("txtLastname") && sel.lastname && !$id("txtLastname").value) $id("txtLastname").value = sel.lastname;
        if ($id("txtEmail") && sel.email && !$id("txtEmail").value) $id("txtEmail").value = sel.email;
        if ($id("txtContact") && sel.contact && !$id("txtContact").value) $id("txtContact").value = sel.contact;

        // keep hidden sport mirror if you use one
        const hfS = hfSelectedSport();
        if (hfS) hfS.value = getSelectedSport();
    }

    function saveState(step) {
        const sel = getSelection();
        SafeStore.set(KEY_BOOKING, JSON.stringify({ ...sel, step: String(step || "1") }));
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

    // ----------------- TIMETABLE -----------------
    function clearSlotVisuals() {
        document.querySelectorAll(".slot.selected, .slot.selected-range")
            .forEach(x => x.classList.remove("selected", "selected-range"));
    }

    function ensureTimeTableShell() {
        // your click binding uses .timetable-shell. If missing, try to bind to .table-responsive as fallback.
        return document.querySelector(".timetable-shell") || document.querySelector(".table-responsive") || null;
    }

    function applySportFilterToTimeTable() {
        const selected = getSelectedSport();

        document.querySelectorAll(".slot.sport-disabled")
            .forEach(el => el.classList.remove("sport-disabled"));

        if (!selected) {
            document.querySelectorAll(".slot.reservable")
                .forEach(el => el.classList.add("sport-disabled"));

            const cur = getSelection();
            if (cur.courtId || cur.start || cur.end) {
                setSelection({ ...cur, courtId: "", start: "", end: "" });
                clearSlotVisuals();
                setSlotLabel();
                syncSummaries();
                saveState(1);
            } else {
                setSlotLabel();
            }
            return;
        }

        document.querySelectorAll(".slot.reservable").forEach(el => {
            const courtId = el.dataset.court;
            if (!courtId) return;

            const court = (courts || []).find(x => String(x.CourtID) === String(courtId));
            const sport = String(court?.SportName || "").toLowerCase();

            if (sport && sport !== selected) el.classList.add("sport-disabled");
        });

        const cur = getSelection();
        if (cur.courtId) {
            const court = (courts || []).find(x => String(x.CourtID) === String(cur.courtId));
            const sport = String(court?.SportName || "").toLowerCase();
            if (sport && sport !== selected) {
                setSelection({ ...cur, courtId: "", start: "", end: "" });
                clearSlotVisuals();
                setSlotLabel();
                syncSummaries();
                saveState(1);
            }
        }

        setSlotLabel();
    }

    function initTimeTableClicks() {
        const shell = ensureTimeTableShell();
        if (!shell) return;

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

            const sel = getSelection();
            setSelection({ ...sel, date, courtId, start, end });

            const lbl = HF.lblSlot();
            if (lbl) lbl.textContent = `Selected: ${date} | Court ${courtNum} | ${start} - ${end} (${durH}h)`;

            syncSummaries();
            loadAndRenderEquipment();
            saveState(1);
        });
    }

    function reselectSlotFromHidden() {
        const cur = getSelection();
        if (!cur.courtId || !cur.date || !cur.start) return;

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

    // ----------------- RENTALS -----------------
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

        // clamp cart to available stock
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

        if (!date || !start) {
            const div = $("equipmentContainer");
            if (div) div.innerHTML = `<div class="text-muted small">Select a time slot first to load rental availability.</div>`;
            renderCartUI([]);
            return;
        }

        fetchEquipmentAvailability(date, start, dur).then(renderEquipmentProducts);
    }
    window.loadAndRenderEquipment = loadAndRenderEquipment;

    // ----------------- TOTALS + SUMMARIES -----------------
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

        const court = (courts || []).find(c => String(c.CourtID) === String(sel.courtId));
        const courtNum = court ? court.CourtNumber : "---";
        const sport = court ? court.SportName : (sel.sport ? sel.sport : "---");

        const timeRange = (sel.start && sel.end)
            ? `${formatTime12Hour(sel.start)} - ${formatTime12Hour(sel.end)}`
            : "---";

        const durTxt = `${getDurationHours()} Hour${getDurationHours() > 1 ? "s" : ""}`;

        if ($("courtSummaryCourt")) $("courtSummaryCourt").innerText = sel.courtId ? `Court ${courtNum}` : "---";
        if ($("courtSummarySport")) $("courtSummarySport").innerText = sel.courtId ? sport : (sel.sport ? sel.sport : "---");
        if ($("courtSummaryTime")) $("courtSummaryTime").innerText = sel.start ? timeRange : "---";
        if ($("courtSummaryDuration")) $("courtSummaryDuration").innerText = durTxt;

        if ($("summaryCourt")) $("summaryCourt").innerText = sel.courtId ? `Court ${courtNum}` : "---";
        if ($("summarySport")) $("summarySport").innerText = sel.courtId ? sport : (sel.sport ? sel.sport : "---");
        if ($("summaryTime")) $("summaryTime").innerText = sel.start ? timeRange : "---";
        if ($("summaryDuration")) $("summaryDuration").innerText = durTxt;

        if ($("summaryPlayers")) $("summaryPlayers").innerText = sel.players || "1";

        const courtTotal = getCourtTotal();
        const rentalsTotal = getRentalsTotalFromUI();
        const grand = courtTotal + rentalsTotal;

        if ($("totalPrice")) $("totalPrice").innerText = "₱ " + courtTotal.toFixed(2);
        if ($("totalFinal")) $("totalFinal").innerText = "₱ " + grand.toFixed(2);
        if ($("summary-totalPrice")) $("summary-totalPrice").innerText = "₱ " + grand.toFixed(2);

        if ($("summaryFirstname")) $("summaryFirstname").innerText = $id("txtFirstname")?.value || "------";
        if ($("summaryLastname")) $("summaryLastname").innerText = $id("txtLastname")?.value || "------";
        if ($("summaryEmail")) $("summaryEmail").innerText = $id("txtEmail")?.value || "------";
        if ($("summaryContact")) $("summaryContact").innerText = $id("txtContact")?.value || "------";
        if ($("summary-totalPriceInfo")) $("summary-totalPriceInfo").innerText = "₱ " + grand.toFixed(2);

        if (HF.rentalCart()) HF.rentalCart().value = SafeStore.get(KEY_CART) || "{}";

        // keep label consistent whenever summaries update
        setSlotLabel();
    }

    // ----------------- NAVIGATION -----------------
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

    // ----------------- PLAYER INFO -----------------
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

    // ----------------- PAYMENT MODAL -----------------
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

    // ----------------- UPDATEPANEL REBIND -----------------
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

            const shell = ensureTimeTableShell();
            if (shell) shell.dataset.bound = "0";

            bindControlsOnce();
            initTimeTableClicks();
            applySportFilterToTimeTable();
            reselectSlotFromHidden();

            loadAndRenderEquipment();
            syncSummaries();
        });
    }

    // ----------------- BIND CONTROLS -----------------
    function bindControlsOnce() {
        const sport = $id("ddlSport");
        if (sport && sport.dataset.bound !== "1") {
            sport.dataset.bound = "1";
            sport.addEventListener("change", () => {
                const cur = getSelection();
                setSelection({ ...cur, courtId: "", start: "", end: "" });
                clearSlotVisuals();
                setSlotLabel();
                saveState(1);

                const tgt = ids.ddlSportUnique;
                const hfS = hfSelectedSport();
                if (hfS) hfS.value = getSelectedSport();

                if (typeof __doPostBack === "function" && tgt) {
                    __doPostBack(tgt, "");
                    return;
                }

                applySportFilterToTimeTable();
                syncSummaries();
            });
        }

        const dur = $id("ddlDuration");
        if (dur && dur.dataset.bound !== "1") {
            dur.dataset.bound = "1";
            dur.addEventListener("change", () => {
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

    // ----------------- RESUME / INITIAL -----------------
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
        setSlotLabel();

        const div = $("equipmentContainer");
        if (div) div.innerHTML = `<div class="text-muted small">Select a time slot first to load rental availability.</div>`;
        renderCartUI([]);

        syncSummaries();
        saveState(1);
    }

    // ----------------- PUBLIC RESET (IMPROVED) -----------------
    window.resetReservationUI = function resetReservationUI() {
        try {
            sessionStorage.removeItem(KEY_BOOKING);
            sessionStorage.removeItem(KEY_CART);
            sessionStorage.removeItem("isPaying");
        } catch { }

        const section = $("reservationSection");
        if (section) section.style.display = "none";

        ["dateSelectionSection", "rentalSelectionSection", "infoSection"].forEach(id => {
            const el = $(id);
            if (el) el.style.display = "none";
        });

        document.querySelectorAll(".step-item").forEach(x => x.classList.remove("active"));
        document.querySelector(".step-item")?.classList.add("active");

        const clearById = (domId) => { const el = document.getElementById(domId); if (el) el.value = ""; };

        if (ids.txtFirstname) clearById(ids.txtFirstname);
        if (ids.txtLastname) clearById(ids.txtLastname);
        if (ids.txtEmail) clearById(ids.txtEmail);
        if (ids.txtContact) clearById(ids.txtContact);

        if (ids.ddlSport) document.getElementById(ids.ddlSport) && (document.getElementById(ids.ddlSport).value = "");
        if (ids.ddlDuration) document.getElementById(ids.ddlDuration) && (document.getElementById(ids.ddlDuration).value = "1");
        const np = $("numPlayers");
        if (np) np.value = "1";

        const setText = (id, val) => { const el = $(id); if (el) el.textContent = val; };
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

        const cartItems = $("cartItems");
        if (cartItems) cartItems.innerHTML = "";

        // clear hidden fields safely
        const clearEl = (el) => { if (el) el.value = ""; };
        clearEl(HF.selectedCourtID());
        clearEl(HF.selectedDate());
        clearEl(HF.courtID());
        clearEl(HF.resDate());
        clearEl(HF.start());
        clearEl(HF.end());
        clearEl(HF.rentalCart());
        const hfS = hfSelectedSport();
        clearEl(hfS);

        // close modal
        const modal = $("paymentModal");
        if (modal) modal.style.display = "none";

        location.reload();
    };

    // ----------------- INIT -----------------
    function initOnce() {


        if (window.__reservationInitDone) return;
        window.__reservationInitDone = true;

        try { courts = JSON.parse($("hfCourts")?.value || "[]"); } catch { courts = []; }
        try { queues = JSON.parse($("hfQueues")?.value || "[]"); } catch { queues = []; }

        wireUpdatePanelHookOnce();
        bindControlsOnce();

        if (isLoggedIn) {
            fillUserInfoIfEmpty();
            syncSummaries();
        }
        initTimeTableClicks();

        if (HF.rentalCart()) HF.rentalCart().value = SafeStore.get(KEY_CART) || "{}";
        const hfS = hfSelectedSport();
        if (hfS) hfS.value = getSelectedSport();

        const raw = SafeStore.get(KEY_BOOKING);
        if (raw) resumeToSavedStep();
        else {
            resetInitialNoPreselect();
            applySportFilterToTimeTable();
        }

        syncSummaries();
    }

    function safeInit() {
        try { initOnce(); }
        catch (e) { console.error("Reservation init failed:", e); }
    }

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", safeInit);
    else safeInit();

    if (window.Sys && Sys.Application) Sys.Application.add_load(safeInit);

})();