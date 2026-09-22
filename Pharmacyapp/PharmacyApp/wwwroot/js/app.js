(function () {
    "use strict";

    const api = {
        async request(url, options) {
            const response = await fetch(url, options);
            const text = await response.text();
            const payload = text ? JSON.parse(text) : null;
            if (!response.ok) {
                throw new Error(extractError(payload) || "Request failed.");
            }
            return payload;
        },
        getMedicines(search) {
            const query = search ? "?search=" + encodeURIComponent(search) : "";
            return api.request("/api/medicines" + query);
        },
        addMedicine(medicine) {
            return api.request("/api/medicines", jsonBody(medicine));
        },
        getSales() {
            return api.request("/api/sales");
        },
        addSale(sale) {
            return api.request("/api/sales", jsonBody(sale));
        },
        getEvents() {
            return api.request("/api/events?take=100");
        }
    };

    function jsonBody(data) {
        return {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(data)
        };
    }

    function extractError(payload) {
        if (!payload) { return null; }
        if (payload.message) { return payload.message; }
        if (payload.errors) {
            return Object.values(payload.errors).flat().join(" ");
        }
        return payload.title || null;
    }

    let medicines = [];

    const el = {
        message: document.getElementById("message"),
        search: document.getElementById("search"),
        medicineRows: document.getElementById("medicine-rows"),
        saleRows: document.getElementById("sale-rows"),
        eventRows: document.getElementById("event-rows"),
        medicineForm: document.getElementById("medicine-form"),
        saleForm: document.getElementById("sale-form"),
        medicineSelect: document.querySelector("#sale-form select[name=medicineId]")
    };

    function showMessage(text, isError) {
        el.message.textContent = text;
        el.message.classList.toggle("error", !!isError);
        el.message.classList.remove("hidden");
        window.clearTimeout(showMessage.timer);
        showMessage.timer = window.setTimeout(function () {
            el.message.classList.add("hidden");
        }, 4000);
    }

    function cell(row, text, className) {
        const td = document.createElement("td");
        td.textContent = text;
        if (className) { td.className = className; }
        row.appendChild(td);
        return td;
    }

    function daysUntil(dateString) {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const expiry = new Date(dateString + "T00:00:00");
        return Math.round((expiry - today) / 86400000);
    }

    function formatMoney(value) {
        return Number(value).toFixed(2);
    }

    function rowClass(medicine) {
        // Expiry warning takes precedence over the low-stock warning.
        if (daysUntil(medicine.expiryDate) < 30) { return "expiring"; }
        if (medicine.quantity < 10) { return "low-stock"; }
        return "";
    }

    function renderMedicines() {
        el.medicineRows.textContent = "";
        if (medicines.length === 0) {
            const row = el.medicineRows.insertRow();
            const td = cell(row, "No medicines found.");
            td.colSpan = 6;
            return;
        }

        medicines.forEach(function (medicine) {
            const row = el.medicineRows.insertRow();
            row.className = rowClass(medicine);
            cell(row, medicine.name);
            cell(row, medicine.brand);
            cell(row, medicine.expiryDate);
            cell(row, medicine.quantity, "num");
            cell(row, formatMoney(medicine.price), "num");

            const actionCell = row.insertCell();
            const button = document.createElement("button");
            button.type = "button";
            button.textContent = "Sell";
            button.disabled = medicine.quantity < 1;
            button.addEventListener("click", function () {
                switchView("sales");
                el.medicineSelect.value = String(medicine.id);
            });
            actionCell.appendChild(button);
        });
    }

    function renderMedicineOptions() {
        el.medicineSelect.textContent = "";
        medicines.forEach(function (medicine) {
            const option = document.createElement("option");
            option.value = medicine.id;
            option.textContent = medicine.name + " (" + medicine.quantity + " in stock)";
            el.medicineSelect.appendChild(option);
        });
    }

    function renderSales(sales) {
        el.saleRows.textContent = "";
        if (sales.length === 0) {
            const row = el.saleRows.insertRow();
            cell(row, "No sales recorded yet.").colSpan = 6;
            return;
        }

        sales.forEach(function (sale) {
            const row = el.saleRows.insertRow();
            cell(row, new Date(sale.soldOn).toLocaleString());
            cell(row, sale.medicineName);
            cell(row, sale.quantity, "num");
            cell(row, formatMoney(sale.unitPrice), "num");
            cell(row, formatMoney(sale.totalAmount), "num");
            cell(row, sale.customerName || "-");
        });
    }

    async function loadMedicines() {
        try {
            medicines = await api.getMedicines(el.search.value);
            renderMedicines();
            renderMedicineOptions();
        } catch (error) {
            showMessage(error.message, true);
        }
    }

    async function loadSales() {
        try {
            renderSales(await api.getSales());
        } catch (error) {
            showMessage(error.message, true);
        }
    }

    async function loadEvents() {
        try {
            const events = await api.getEvents();
            el.eventRows.textContent = "";
            if (events.length === 0) {
                cell(el.eventRows.insertRow(), "No events logged yet.").colSpan = 4;
                return;
            }
            events.forEach(function (entry) {
                const row = el.eventRows.insertRow();
                cell(row, new Date(entry.timestampUtc).toLocaleString());
                cell(row, entry.level);
                cell(row, entry.eventType);
                cell(row, entry.message);
            });
        } catch (error) {
            showMessage(error.message, true);
        }
    }

    function switchView(name) {
        document.querySelectorAll(".view").forEach(function (view) {
            view.classList.toggle("hidden", view.id !== "view-" + name);
        });
        document.querySelectorAll(".tab").forEach(function (tab) {
            tab.classList.toggle("active", tab.dataset.view === name);
        });
        if (name === "sales") { loadSales(); }
        if (name === "events") { loadEvents(); }
    }

    document.querySelectorAll(".tab").forEach(function (tab) {
        tab.addEventListener("click", function () {
            switchView(tab.dataset.view);
        });
    });

    let searchTimer;
    el.search.addEventListener("input", function () {
        window.clearTimeout(searchTimer);
        searchTimer = window.setTimeout(loadMedicines, 250);
    });

    el.medicineForm.addEventListener("submit", async function (event) {
        event.preventDefault();
        const data = new FormData(el.medicineForm);
        const medicine = {
            name: data.get("name").trim(),
            brand: data.get("brand").trim(),
            expiryDate: data.get("expiryDate"),
            quantity: Number(data.get("quantity")),
            price: Number(data.get("price")),
            notes: data.get("notes").trim()
        };

        if (!medicine.name || !medicine.brand || !medicine.expiryDate) {
            showMessage("Name, brand and expiry date are required.", true);
            return;
        }

        try {
            await api.addMedicine(medicine);
            el.medicineForm.reset();
            showMessage(medicine.name + " added.");
            await loadMedicines();
            switchView("inventory");
        } catch (error) {
            showMessage(error.message, true);
        }
    });

    el.saleForm.addEventListener("submit", async function (event) {
        event.preventDefault();
        const data = new FormData(el.saleForm);
        if (!data.get("medicineId")) {
            showMessage("Add a medicine before recording a sale.", true);
            return;
        }

        try {
            const sale = await api.addSale({
                medicineId: Number(data.get("medicineId")),
                quantity: Number(data.get("quantity")),
                customerName: data.get("customerName").trim()
            });
            showMessage("Sale recorded for " + sale.medicineName + ".");
            el.saleForm.reset();
            await loadMedicines();
            await loadSales();
        } catch (error) {
            showMessage(error.message, true);
        }
    });

    loadMedicines();
})();
