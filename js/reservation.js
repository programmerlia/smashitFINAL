const SafeStore = (() => {
    let mem = {};

    function canUse() {
        try {
            const k = "__t";
            sessionStorage.setItem(k, "1");
            sessionStorage.removeItem(k);
            return true;
        } catch {
            return false;
        }
    }

    const ok = canUse();

    return {
        get(key) {
            try {
                return ok ? sessionStorage.getItem(key) : (mem[key] ?? null);
            } catch {
                return mem[key] ?? null;
            }
        },
        set(key, val) {
            try {
                ok ? sessionStorage.setItem(key, val) : (mem[key] = val);
            } catch {
                mem[key] = val;
            }
        },
        remove(key) {
            try {
                ok ? sessionStorage.removeItem(key) : delete mem[key];
            } catch {
                delete mem[key];
            }
        }
    };
})();

(function () {
    "use strict";

    const cfg = window.resConfig || {};
    const ids = cfg.ids || {};
    const urls = cfg.urls || {};
    const sessionUser = cfg.sessionUser || {};
    const isLoggedIn = !!cfg.isLoggedIn;

    const KEY_BOOKING = "pendingBooking";
    const KEY_RENTAL_CART = "rentalCart";
    const KEY_CONSUMABLE_CART = "consumableCart";

    const $ = (id) => document.getElementById(id);
    const $id = (key) => document.getElementById(ids[key]);

    function byAnyId(staticId, clientIdOrWindowKey) {
        if (staticId) {
            const el = $(staticId);
            if (el) return el;
        }

        if (clientIdOrWindowKey) {
            const cid =
                typeof clientIdOrWindowKey === "string" &&
                    clientIdOrWindowKey.indexOf("ClientID") >= 0
                    ? window[clientIdOrWindowKey]
                    : clientIdOrWindowKey;

            if (cid) {
                const el = document.getElementById(cid);
                if (el) return el;
            }
        }

        return null;
    }

    const HF = {
        courtID: () => byAnyId(null, "hfCourtIDClientID"),
        resDate: () => byAnyId(null, "hfResDateClientID"),
        start: () => byAnyId(null, "hfStartTimeClientID"),
        end: () => byAnyId(null, "hfEndTimeClientID"),
        lblSlot: () => byAnyId(null, "lblSelectedSlotClientID"),
        selectedDate: () => byAnyId("hfSelectedDate", "hfSelectedDateClientID"),
        selectedCourtID: () => byAnyId("hfSelectedCourtID", "hfSelectedCourtIDClientID"),
        rentalCart: () => byAnyId("hfRentalCart", "hfRentalCartClientID"),
        consumableCart: () => byAnyId("hfConsumableCart", "hfConsumableCartClientID")
    };

    const hfSelectedSport = () => byAnyId("hfSelectedSport", "hfSelectedSportClientID");

    let courts = [];
    let queues = [];
    let lastEquipmentRows = [];

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

    function rentalKey(type, spec) {
        return `${type}||${spec || ""}`;
    }

    function consumableKey(type, spec) {
        return `${type}||${spec || ""}`;
    }

    function displayName(type, spec) {
        return spec ? `${type} (${spec})` : type;
    }

    function escapeQuotes(s) {
        return String(s).replace(/\\/g, "\\\\").replace(/'/g, "\\'");
    }

    function getCourtTotal() {
        return getDurationHours() * 330;
    }

    function getRentalsTotalFromUI() {
        const t = $("rentalsTotal")?.innerText || "₱ 0";
        return Number((t.replace(/[^\d.]/g, "")) || 0);
    }

    function getConsumablesTotalFromUI() {
        const t = $("consumablesTotal")?.innerText || "₱ 0";
        return Number((t.replace(/[^\d.]/g, "")) || 0);
    }

    function getRentalCart() {
        try {
            return JSON.parse(SafeStore.get(KEY_RENTAL_CART) || "{}");
        } catch {
            return {};
        }
    }

    function setRentalCart(cart) {
        SafeStore.set(KEY_RENTAL_CART, JSON.stringify(cart || {}));
        const hf = HF.rentalCart();
        if (hf) hf.value = SafeStore.get(KEY_RENTAL_CART) || "{}";
    }

    function getConsumableCart() {
        try {
            return JSON.parse(SafeStore.get(KEY_CONSUMABLE_CART) || "{}");
        } catch {
            return {};
        }
    }

    function setConsumableCart(cart) {
        SafeStore.set(KEY_CONSUMABLE_CART, JSON.stringify(cart || {}));
        const hf = HF.consumableCart();
        if (hf) hf.value = SafeStore.get(KEY_CONSUMABLE_CART) || "{}";
    }

    function openSportPickerModal() {
        const modal = $("sportPickerModal");
        if (modal) modal.style.display = "block";
    }
    window.openSportPickerModal = openSportPickerModal;

    function closeSportPickerModal() {
        const modal = $("sportPickerModal");
        if (modal) modal.style.display = "none";
    }
    window.closeSportPickerModal = closeSportPickerModal;

    function makeCalendarDatesWhite() {
        document.querySelectorAll(".calendar-compact td").forEach(td => {
            td.style.backgroundColor = "#fff";
            td.style.color = "#000";
        });

        document.querySelectorAll(".calendar-compact td a").forEach(a => {
            a.style.color = "#000";
        });
    }
    function updateSelectedSportBadge() {
        const badge = $("selectedSportBadge");
        if (!badge) return;

        const sport = getSelectedSport();
        if (!sport) {
            badge.textContent = "No sport selected";
            badge.classList.remove("bg-success");
            badge.classList.add("bg-primary");
            return;
        }

        badge.textContent = sport.charAt(0).toUpperCase() + sport.slice(1);
    }

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

        if ($id("txtFirstname") && sel.firstname && !$id("txtFirstname").value) $id("txtFirstname").value = sel.firstname;
        if ($id("txtLastname") && sel.lastname && !$id("txtLastname").value) $id("txtLastname").value = sel.lastname;
        if ($id("txtEmail") && sel.email && !$id("txtEmail").value) $id("txtEmail").value = sel.email;
        if ($id("txtContact") && sel.contact && !$id("txtContact").value) $id("txtContact").value = sel.contact;

        const hfS = hfSelectedSport();
        if (hfS) hfS.value = getSelectedSport();
    }

    function saveState(step) {
        SafeStore.set(KEY_BOOKING, JSON.stringify({ ...getSelection(), step: String(step || "1") }));
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

    function showStep(step) {
        const s1 = $("dateSelectionSection");
        const s2 = $("rentalSelectionSection");
        const s3 = $("infoSection");

        if (s1) s1.style.display = step === 1 ? "flex" : "none";
        if (s2) s2.style.display = step === 2 ? "flex" : "none";
        if (s3) s3.style.display = step === 3 ? "flex" : "none";

        const steps = document.querySelectorAll(".step-item");
        steps.forEach(x => x.classList.remove("active"));
        if (steps[step - 1]) steps[step - 1].classList.add("active");
    }

    function showReservation(doScroll) {
        const currentSport = getSelectedSport();

        if (!currentSport) {
            openSportPickerModal();
            return;
        }

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
        updateSelectedSportBadge();

        if (doScroll) {
            resSection?.scrollIntoView({ behavior: "smooth", block: "start" });
        }
    }
    window.showReservation = showReservation;

    function getCurrentStep() {
        const s3 = $("infoSection");
        const s2 = $("rentalSelectionSection");
        if (s3 && s3.style.display !== "none") return 3;
        if (s2 && s2.style.display !== "none") return 2;
        return 1;
    }

    function setSlotLabel() {
        const lbl = HF.lblSlot();
        if (!lbl) return;

        const sport = getSelectedSport();
        const sel = getSelection();

        if (!sel.date && !sport) {
            lbl.textContent = "No date and sport selected.";
        } else if (!sel.date) {

            lbl.textContent = "No date selected.";
        } else if (!sport) {
            lbl.textContent = "No sport selected.";
        } else if (!sel.courtId || !sel.start) {
            lbl.textContent = "No slot selected.";
        }
    }

    function clearSlotVisuals() {
        document.querySelectorAll(".slot.selected, .slot.selected-range")
            .forEach(x => x.classList.remove("selected", "selected-range"));
    }

    function ensureTimeTableShell() {
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
            if (!el || el.classList.contains("sport-disabled")) return;

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

                if (!cell) return alert("That duration doesn't fit (missing slot).");
                if (!cell.classList.contains("reservable")) return alert("That duration overlaps blocked/queue.");
                if (cell.classList.contains("sport-disabled")) return alert("Sport mismatch.");

                rangeEls.push(cell);
            }

            const end = addMinutes(start, durH * 60);

            clearSlotVisuals();
            rangeEls.forEach((cell, idx) => cell.classList.add(idx === 0 ? "selected" : "selected-range"));

            if (HF.selectedDate()) HF.selectedDate().value = date;
            if (HF.resDate()) HF.resDate().value = date;

            if (HF.selectedCourtID()) HF.selectedCourtID().value = courtId;
            if (HF.courtID()) HF.courtID().value = courtId;

            if (HF.start()) HF.start().value = start;
            if (HF.end()) HF.end().value = end;

            setSelection({ ...getSelection(), date, courtId, start, end });

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

        const slotsNeeded = Math.max(1, getDurationHours() * 2);
        clearSlotVisuals();

        for (let i = 0; i < slotsNeeded; i++) {
            const t = addMinutes(cur.start, i * 30);
            const q = `.slot[data-court="${cur.courtId}"][data-date="${cur.date}"][data-start="${t}"]`;
            const cell = document.querySelector(q);
            if (!cell) break;

            cell.classList.add(i === 0 ? "selected" : "selected-range");
        }
    }

    function selectSportAndStart(sportValue) {
        const ddl = $id("ddlSport");
        const hfS = hfSelectedSport();
        if (!ddl) return;

        const oldSport = getSelectedSport();

        ddl.value = sportValue || "";
        if (hfS) hfS.value = ddl.value;

        if (oldSport !== ddl.value) {
            clearReservationSelectionForSportChange();
        }

        updateSelectedSportBadge();
        closeSportPickerModal();

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
        saveState(1);

        const tgt = ids.ddlSportUnique;
        if (typeof __doPostBack === "function" && tgt) __doPostBack(tgt, "");

        resSection?.scrollIntoView({ behavior: "smooth" });
    }
    window.selectSportAndStart = selectSportAndStart;

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

    function renderRentalProducts(rows) {
        const div = $("rentalContainer");
        if (!div) return;

        const cart = getRentalCart();

        if (!rows || rows.length === 0) {
            div.innerHTML = `<div class="text-muted small">No rental items available.</div>`;
            renderRentalCartUI(rows);
            return;
        }

        div.innerHTML = rows.map(r => {
            const key = rentalKey(r.EquipmentType, r.EquipmentSpec);
            const price = Number(r.UnitPrice || 0);
            const avail = Number(r.AvailableQty || 0);
            const name = displayName(r.EquipmentType, r.EquipmentSpec);
            const inCart = Number(cart[key] || 0);

            return `
                <div class="col-12 col-md-6">
                    <div class="rental-card">
                        <div class="rental-title">${name}</div>
                        <div class="rental-price">₱ ${price.toFixed(2)}</div>
                        <div class="rental-stock"><b>${avail}</b> available</div>
                        <div class="mt-2 d-flex gap-2">
                            <button
                                type="button"
                                class="btn btn-sm btn-outline-secondary"
                                onclick="addRentalToCart('${escapeQuotes(key)}', ${avail})"
                                ${avail <= 0 || inCart >= avail ? "disabled" : ""}>
                                Add Rental
                            </button>
                        </div>
                    </div>
                </div>
            `;
        }).join("");

        renderRentalCartUI(rows);
    }

    function renderRentalCartUI(stockRows) {
        const cartDiv = $("cartItems");
        const totalEl = $("rentalsTotal");
        if (!cartDiv || !totalEl) return;

        const cart = getRentalCart();
        let total = 0;

        const stockMap = {};
        (stockRows || []).forEach(r => {
            stockMap[rentalKey(r.EquipmentType, r.EquipmentSpec)] = r;
        });

        const keys = Object.keys(cart);

        if (keys.length === 0) {
            cartDiv.innerHTML = `<div class="text-muted">No rental items yet.</div>`;
            totalEl.innerText = "₱ 0.00";
            syncSummaries();
            return;
        }

        cartDiv.innerHTML = keys.map(k => {
            const qty = Number(cart[k] || 0);
            const row = stockMap[k];
            const price = Number(row?.UnitPrice || 0);
            const line = qty * price;
            total += line;

            const name = row ? displayName(row.EquipmentType, row.EquipmentSpec) : k;

            return `
                <div class="cart-line">
                    <div>
                        <div class="cart-name">${name}</div>
                        <div class="cart-sub">₱ ${price.toFixed(2)} x ${qty}</div>
                    </div>
                    <div class="cart-controls">
                        <button type="button" class="cart-btn" onclick="decRentalCart('${escapeQuotes(k)}')">−</button>
                        <div class="fw-bold" style="width:18px;text-align:center;">${qty}</div>
                        <button type="button" class="cart-btn" onclick="incRentalCart('${escapeQuotes(k)}')">+</button>
                        <button type="button" class="cart-x" onclick="removeRentalCart('${escapeQuotes(k)}')">X</button>
                    </div>
                </div>
            `;
        }).join("");

        totalEl.innerText = "₱ " + total.toFixed(2);
        syncSummaries();
    }

    window.addRentalToCart = function (key, avail) {
        const cart = getRentalCart();
        const qty = Number(cart[key] || 0);
        if (qty >= avail) return alert("No more rental stock available.");
        cart[key] = qty + 1;
        setRentalCart(cart);
        loadAndRenderEquipment();
    };

    window.incRentalCart = function (key) {
        const cart = getRentalCart();
        cart[key] = Number(cart[key] || 0) + 1;
        setRentalCart(cart);
        loadAndRenderEquipment();
    };

    window.decRentalCart = function (key) {
        const cart = getRentalCart();
        cart[key] = Number(cart[key] || 0) - 1;
        if (cart[key] <= 0) delete cart[key];
        setRentalCart(cart);
        loadAndRenderEquipment();
    };

    window.removeRentalCart = function (key) {
        const cart = getRentalCart();
        delete cart[key];
        setRentalCart(cart);
        loadAndRenderEquipment();
    };

    function renderConsumableProducts(rows) {
        const div = $("consumableContainer");
        if (!div) return;

        const cart = getConsumableCart();

        if (!rows || rows.length === 0) {
            div.innerHTML = `<div class="text-muted small">No consumable items available.</div>`;
            renderConsumableCartUI(rows);
            return;
        }

        div.innerHTML = rows.map(r => {
            const key = consumableKey(r.EquipmentType, r.EquipmentSpec);
            const price = Number(r.UnitPrice || 0);
            const avail = Number(r.AvailableQty || 0);
            const name = displayName(r.EquipmentType, r.EquipmentSpec);
            const inCart = Number(cart[key] || 0);

            return `
                <div class="col-12 col-md-6">
                    <div class="rental-card">
                        <div class="rental-title">${name}</div>
                        <div class="rental-price">₱ ${price.toFixed(2)}</div>
                        <div class="rental-stock"><b>${avail}</b> in stock</div>
                        <div class="mt-2 d-flex gap-2">
                            <button
                                type="button"
                                class="btn btn-sm btn-outline-secondary"
                                onclick="addConsumableToCart('${escapeQuotes(key)}', ${avail})"
                                ${avail <= 0 || inCart >= avail ? "disabled" : ""}>
                                Add Consumable
                            </button>
                        </div>
                    </div>
                </div>
            `;
        }).join("");

        renderConsumableCartUI(rows);
    }

    function renderConsumableCartUI(stockRows) {
        const cartDiv = $("consumableItems");
        const totalEl = $("consumablesTotal");
        if (!cartDiv || !totalEl) return;

        const cart = getConsumableCart();
        let total = 0;

        const stockMap = {};
        (stockRows || []).forEach(r => {
            stockMap[consumableKey(r.EquipmentType, r.EquipmentSpec)] = r;
        });

        const keys = Object.keys(cart);

        if (keys.length === 0) {
            cartDiv.innerHTML = `<div class="text-muted">No consumable items yet.</div>`;
            totalEl.innerText = "₱ 0.00";
            syncSummaries();
            return;
        }

        cartDiv.innerHTML = keys.map(k => {
            const qty = Number(cart[k] || 0);
            const row = stockMap[k];
            const price = Number(row?.UnitPrice || 0);
            const line = qty * price;
            total += line;

            const name = row ? displayName(row.EquipmentType, row.EquipmentSpec) : k;

            return `
                <div class="cart-line">
                    <div>
                        <div class="cart-name">${name}</div>
                        <div class="cart-sub">₱ ${price.toFixed(2)} x ${qty}</div>
                    </div>
                    <div class="cart-controls">
                        <button type="button" class="cart-btn" onclick="decConsumableCart('${escapeQuotes(k)}')">−</button>
                        <div class="fw-bold" style="width:18px;text-align:center;">${qty}</div>
                        <button type="button" class="cart-btn" onclick="incConsumableCart('${escapeQuotes(k)}')">+</button>
                        <button type="button" class="cart-x" onclick="removeConsumableCart('${escapeQuotes(k)}')">X</button>
                    </div>
                </div>
            `;
        }).join("");

        totalEl.innerText = "₱ " + total.toFixed(2);
        syncSummaries();
    }

    window.addConsumableToCart = function (key, avail) {
        const cart = getConsumableCart();
        const qty = Number(cart[key] || 0);
        if (qty >= avail) return alert("No more consumable stock available.");
        cart[key] = qty + 1;
        setConsumableCart(cart);
        loadAndRenderEquipment();
    };

    window.incConsumableCart = function (key) {
        const cart = getConsumableCart();
        cart[key] = Number(cart[key] || 0) + 1;
        setConsumableCart(cart);
        loadAndRenderEquipment();
    };

    window.decConsumableCart = function (key) {
        const cart = getConsumableCart();
        cart[key] = Number(cart[key] || 0) - 1;
        if (cart[key] <= 0) delete cart[key];
        setConsumableCart(cart);
        loadAndRenderEquipment();
    };

    window.removeConsumableCart = function (key) {
        const cart = getConsumableCart();
        delete cart[key];
        setConsumableCart(cart);
        loadAndRenderEquipment();
    };

    function loadAndRenderEquipment() {
        const sel = getSelection();

        if (!sel.date || !sel.start) {
            if ($("rentalContainer")) {
                $("rentalContainer").innerHTML = `<div class="text-muted small">Select a time slot first to load rental availability.</div>`;
            }
            if ($("consumableContainer")) {
                $("consumableContainer").innerHTML = `<div class="text-muted small">Select a time slot first to load consumables.</div>`;
            }
            renderRentalCartUI([]);
            renderConsumableCartUI([]);
            return;
        }

        fetchEquipmentAvailability(sel.date, sel.start, getDurationHours()).then(rows => {
            lastEquipmentRows = Array.isArray(rows) ? rows : [];
            renderRentalProducts(lastEquipmentRows.filter(x => x.ItemCategory === "Rental"));
            renderConsumableProducts(lastEquipmentRows.filter(x => x.ItemCategory === "Consumable"));
        });
    }
    window.loadAndRenderEquipment = loadAndRenderEquipment;

    function syncSummaries() {
        const sel = getSelection();
        const court = (courts || []).find(c => String(c.CourtID) === String(sel.courtId));
        const courtNum = court ? court.CourtNumber : "---";
        const sport = court ? court.SportName : (sel.sport || "---");
        const timeRange = sel.start && sel.end ? `${formatTime12Hour(sel.start)} - ${formatTime12Hour(sel.end)}` : "---";
        const durTxt = `${getDurationHours()} Hour${getDurationHours() > 1 ? "s" : ""}`;

        if ($("courtSummaryCourt")) $("courtSummaryCourt").innerText = sel.courtId ? `Court ${courtNum}` : "---";
        if ($("courtSummarySport")) $("courtSummarySport").innerText = sel.courtId ? sport : (sel.sport || "---");
        if ($("courtSummaryTime")) $("courtSummaryTime").innerText = sel.start ? timeRange : "---";
        if ($("courtSummaryDuration")) $("courtSummaryDuration").innerText = durTxt;

        if ($("summaryCourt")) $("summaryCourt").innerText = sel.courtId ? `Court ${courtNum}` : "---";
        if ($("summarySport")) $("summarySport").innerText = sel.courtId ? sport : (sel.sport || "---");
        if ($("summaryTime")) $("summaryTime").innerText = sel.start ? timeRange : "---";
        if ($("summaryDuration")) $("summaryDuration").innerText = durTxt;
        if ($("summaryPlayers")) $("summaryPlayers").innerText = sel.players || "1";

        const courtTotal = getCourtTotal();
        const rentalsTotal = getRentalsTotalFromUI();
        const consumablesTotal = getConsumablesTotalFromUI();
        const grandTotal = courtTotal + rentalsTotal + consumablesTotal;
        const payNowTotal = (courtTotal / 2) + rentalsTotal + consumablesTotal;

        if ($("totalPrice")) $("totalPrice").innerText = "₱ " + courtTotal.toFixed(2);
        if ($("totalFinal")) $("totalFinal").innerText = "₱ " + grandTotal.toFixed(2);
        if ($("summary-totalPrice")) $("summary-totalPrice").innerText = "₱ " + payNowTotal.toFixed(2);
        if ($("summary-totalPriceInfo")) $("summary-totalPriceInfo").innerText = "₱ " + payNowTotal.toFixed(2);

        if ($("summaryFirstname")) $("summaryFirstname").innerText = $id("txtFirstname")?.value || "------";
        if ($("summaryLastname")) $("summaryLastname").innerText = $id("txtLastname")?.value || "------";
        if ($("summaryEmail")) $("summaryEmail").innerText = $id("txtEmail")?.value || "------";
        if ($("summaryContact")) $("summaryContact").innerText = $id("txtContact")?.value || "------";

        const rentalHf = HF.rentalCart();
        if (rentalHf) rentalHf.value = SafeStore.get(KEY_RENTAL_CART) || "{}";

        const consumableHf = HF.consumableCart();
        if (consumableHf) consumableHf.value = SafeStore.get(KEY_CONSUMABLE_CART) || "{}";

        setSlotLabel();
        updateSelectedSportBadge();
    }

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

        const sel = getSelection();

        if (HF.selectedDate()) HF.selectedDate().value = sel.date || "";
        if (HF.resDate()) HF.resDate().value = sel.date || "";
        if (HF.selectedCourtID()) HF.selectedCourtID().value = sel.courtId || "";
        if (HF.courtID()) HF.courtID().value = sel.courtId || "";
        if (HF.start()) HF.start().value = sel.start || "";
        if (HF.end()) HF.end().value = sel.end || "";

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

    function fillUserInfoIfEmpty() {
        const fn = $id("txtFirstname");
        const ln = $id("txtLastname");
        const em = $id("txtEmail");
        const ph = $id("txtContact");

        if (fn && !fn.value) fn.value = sessionUser.firstname || "";
        if (ln && !ln.value) ln.value = sessionUser.lastname || "";
        if (em && !em.value) em.value = sessionUser.email || "";
        if (ph && !ph.value) ph.value = sessionUser.phone || "";
    }

    function bindInfoInputsOnce() {
        [$id("txtLastname"), $id("txtFirstname"), $id("txtEmail"), $id("txtContact")].forEach(i => {
            if (!i || i.dataset.bound === "1") return;
            i.dataset.bound = "1";
            i.addEventListener("input", () => {
                syncSummaries();
                saveState(3);
            });
        });
    }

    function renderPaymentRentals() {
        const listEl = $("pmRentals");
        const totalEl = $("pmRentalsTotal");
        if (!listEl || !totalEl) return;

        const cart = getRentalCart();
        const rows = lastEquipmentRows.filter(x => x.ItemCategory === "Rental");
        const stockMap = {};
        rows.forEach(r => stockMap[rentalKey(r.EquipmentType, r.EquipmentSpec)] = r);

        let total = 0;
        const keys = Object.keys(cart);

        if (keys.length === 0) {
            listEl.innerHTML = "No rental items yet.";
            totalEl.innerText = "₱ 0.00";
            return;
        }

        listEl.innerHTML = keys.map(k => {
            const qty = Number(cart[k] || 0);
            const row = stockMap[k];
            const price = Number(row?.UnitPrice || 0);
            const line = qty * price;
            total += line;
            const name = row ? displayName(row.EquipmentType, row.EquipmentSpec) : k;

            return `
                <div style="display:flex;justify-content:space-between;">
                    <span>${name} × ${qty}</span>
                    <span>₱ ${line.toFixed(2)}</span>
                </div>
            `;
        }).join("");

        totalEl.innerText = "₱ " + total.toFixed(2);
    }

    function renderPaymentConsumables() {
        const listEl = $("pmConsumables");
        const totalEl = $("pmConsumablesTotal");
        if (!listEl || !totalEl) return;

        const cart = getConsumableCart();
        const rows = lastEquipmentRows.filter(x => x.ItemCategory === "Consumable");
        const stockMap = {};
        rows.forEach(r => stockMap[consumableKey(r.EquipmentType, r.EquipmentSpec)] = r);

        let total = 0;
        const keys = Object.keys(cart);

        if (keys.length === 0) {
            listEl.innerHTML = "No consumable items yet.";
            totalEl.innerText = "₱ 0.00";
            return;
        }

        listEl.innerHTML = keys.map(k => {
            const qty = Number(cart[k] || 0);
            const row = stockMap[k];
            const price = Number(row?.UnitPrice || 0);
            const line = qty * price;
            total += line;
            const name = row ? displayName(row.EquipmentType, row.EquipmentSpec) : k;

            return `
                <div style="display:flex;justify-content:space-between;">
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
        renderPaymentConsumables();

        modal.style.display = "block";
    }
    window.openPaymentModal = openPaymentModal;

    window.closePaymentModal = function () {
        const modal = $("paymentModal");
        if (modal) modal.style.display = "none";
    };

    window.confirmAndPay = function () {
        const rentalHf = HF.rentalCart();
        if (rentalHf) rentalHf.value = SafeStore.get(KEY_RENTAL_CART) || "{}";

        const consumableHf = HF.consumableCart();
        if (consumableHf) consumableHf.value = SafeStore.get(KEY_CONSUMABLE_CART) || "{}";

        const sel = getSelection();
        if (!sel.date || !sel.courtId || !sel.start || !sel.duration) {
            alert("Please complete your slot details first.");
            return false;
        }

        saveState(3);
        window.closePaymentModal();

        setTimeout(function () {
            const btn = document.getElementById(window.resConfig.ids.btnSubmitReservation);
            if (btn) {
                btn.click();
            } else if (typeof __doPostBack === "function") {
                __doPostBack(window.resConfig.ids.btnSubmitReservationUnique, "");
            }
        }, 100);

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

    function wireUpdatePanelHookOnce() {
        if (!(window.Sys && Sys.WebForms && Sys.WebForms.PageRequestManager)) return;

        const prm = Sys.WebForms.PageRequestManager.getInstance();
        if (prm._reservationHooked) return;
        prm._reservationHooked = true;

        prm.add_endRequest(() => {
            try {
                courts = JSON.parse($("hfCourts")?.value || "[]");
            } catch {
                courts = [];
            }

            try {
                queues = JSON.parse($("hfQueues")?.value || "[]");
            } catch {
                queues = [];
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

    function resumeToSavedStep() {
        const data = restoreState();
        if (!data) return;

        showReservation(false);
        showStep(parseInt(data.step || "1", 10) || 1);

        applySportFilterToTimeTable();
        reselectSlotFromHidden();
        loadAndRenderEquipment();
        fillUserInfoIfEmpty();
        bindInfoInputsOnce();
        syncSummaries();
    }
    function clearReservationSelectionForSportChange() {
        setSelection({ ...getSelection(), courtId: "", start: "", end: "" });
        clearSlotVisuals();
        setRentalCart({});
        setConsumableCart({});

        if ($("rentalContainer")) {
            $("rentalContainer").innerHTML = `<div class="text-muted small">Select a time slot first to load rental availability.</div>`;
        }
        if ($("consumableContainer")) {
            $("consumableContainer").innerHTML = `<div class="text-muted small">Select a time slot first to load consumables.</div>`;
        }

        renderRentalCartUI([]);
        renderConsumableCartUI([]);
        syncSummaries();
        setSlotLabel();
    }

    function resetInitialNoPreselect() {
        const sport = $id("ddlSport");
        if (sport) sport.value = "";

        setSelection({ ...getSelection(), courtId: "", start: "", end: "" });
        clearSlotVisuals();
        setSlotLabel();

        if ($("rentalContainer")) {
            $("rentalContainer").innerHTML = `<div class="text-muted small">Select a time slot first to load rental availability.</div>`;
        }
        if ($("consumableContainer")) {
            $("consumableContainer").innerHTML = `<div class="text-muted small">Select a time slot first to load consumables.</div>`;
        }

        renderRentalCartUI([]);
        renderConsumableCartUI([]);
        updateSelectedSportBadge();
        syncSummaries();
    }

    window.resetReservationUI = function () {
        try {
            sessionStorage.removeItem(KEY_BOOKING);
            sessionStorage.removeItem(KEY_RENTAL_CART);
            sessionStorage.removeItem(KEY_CONSUMABLE_CART);
        } catch { }

        const section = $("reservationSection");
        if (section) section.style.display = "none";

        ["dateSelectionSection", "rentalSelectionSection", "infoSection"].forEach(id => {
            const el = $(id);
            if (el) el.style.display = "none";
        });

        document.querySelectorAll(".step-item").forEach(x => x.classList.remove("active"));
        document.querySelector(".step-item")?.classList.add("active");

        const clearById = (domId) => {
            const el = document.getElementById(domId);
            if (el) el.value = "";
        };

        if (ids.txtFirstname) clearById(ids.txtFirstname);
        if (ids.txtLastname) clearById(ids.txtLastname);
        if (ids.txtEmail) clearById(ids.txtEmail);
        if (ids.txtContact) clearById(ids.txtContact);

        if (ids.ddlSport && document.getElementById(ids.ddlSport)) {
            document.getElementById(ids.ddlSport).value = "";
        }

        const badge = $("selectedSportBadge");
        if (badge) badge.textContent = "No sport selected";

        if (ids.ddlDuration && document.getElementById(ids.ddlDuration)) {
            document.getElementById(ids.ddlDuration).value = "1";
        }

        const np = $("numPlayers");
        if (np) np.value = "1";

        const setText = (id, val) => {
            const el = $(id);
            if (el) el.textContent = val;
        };

        setText("courtSummaryCourt", "---");
        setText("courtSummarySport", "---");
        setText("courtSummaryTime", "---");
        setText("courtSummaryDuration", "---");
        setText("totalPrice", "₱ 0");
        setText("rentalsTotal", "₱ 0");
        setText("consumablesTotal", "₱ 0");
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

        if ($("cartItems")) $("cartItems").innerHTML = "";
        if ($("consumableItems")) $("consumableItems").innerHTML = "";

        const clearEl = (el) => {
            if (el) el.value = "";
        };

        clearEl(HF.selectedCourtID());
        clearEl(HF.selectedDate());
        clearEl(HF.courtID());
        clearEl(HF.resDate());
        clearEl(HF.start());
        clearEl(HF.end());
        clearEl(HF.rentalCart());
        clearEl(HF.consumableCart());
        clearEl(hfSelectedSport());

        const modal = $("paymentModal");
        if (modal) modal.style.display = "none";

        location.reload();
    };

    function initOnce() {
        if (window.__reservationInitDone) return;
        window.__reservationInitDone = true;

        try {
            courts = JSON.parse($("hfCourts")?.value || "[]");
        } catch {
            courts = [];
        }

        try {
            queues = JSON.parse($("hfQueues")?.value || "[]");
        } catch {
            queues = [];
        }

        wireUpdatePanelHookOnce();
        bindControlsOnce();

        if (isLoggedIn) {
            fillUserInfoIfEmpty();
            syncSummaries();
        }

        initTimeTableClicks();

        const rentalHf = HF.rentalCart();
        if (rentalHf) rentalHf.value = SafeStore.get(KEY_RENTAL_CART) || "{}";

        const consumableHf = HF.consumableCart();
        if (consumableHf) consumableHf.value = SafeStore.get(KEY_CONSUMABLE_CART) || "{}";

        const hfS = hfSelectedSport();
        if (hfS) hfS.value = getSelectedSport();

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
        try {
            initOnce();
        } catch (e) {
            console.error("Reservation init failed:", e);
        }
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