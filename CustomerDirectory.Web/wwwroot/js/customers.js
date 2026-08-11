(function () {
    const state = { search: "", sortBy: "fullname", descending: false, page: 1, pageSize: 10 };
    let debounceTimer = null;

    const rowsEl = document.getElementById("customerRows");
    const loadingEl = document.getElementById("gridLoading");
    const errorEl = document.getElementById("gridError");
    const emptyEl = document.getElementById("emptyState");
    const pagerEl = document.getElementById("pager");

    function antiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]').value;
    }

    const modalEl = document.getElementById("customerModal");
    const modal = new bootstrap.Modal(modalEl);
    const form = document.getElementById("customerForm");
    const toastEl = document.getElementById("toast");
    const toast = new bootstrap.Toast(toastEl);
    let isSubmitting = false;

    document.getElementById("btnNewCustomer").addEventListener("click", () => openModal(null));

    rowsEl.addEventListener("click", async (e) => {
        const editBtn = e.target.closest(".btn-edit");
        const deleteBtn = e.target.closest(".btn-delete");
        if (editBtn) await openModal(editBtn.dataset.id);
        if (deleteBtn) await handleDelete(deleteBtn.dataset.id, deleteBtn.dataset.name);
    });

    async function openModal(id) {
        clearErrors();
        form.reset();
        document.getElementById("customerId").value = id ?? "";
        document.getElementById("statusField").style.display = id ? "block" : "none";
        document.getElementById("customerModalTitle").textContent = id ? "Edit Customer" : "New Customer";

        if (id) {
            const res = await fetch(`/api/customers/${id}`);
            if (res.ok) {
                const c = await res.json();
                document.getElementById("fullName").value = c.fullName;
                document.getElementById("email").value = c.email;
                document.getElementById("phone").value = c.phone;
                document.getElementById("address").value = c.address ?? "";
                document.getElementById("status").value = c.status;
            } else if (res.status === 404) {
                showToast("That customer no longer exists.", true);
                CustomerGrid.refresh();
                return;
            }
        }
        modal.show();
    }

    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        if (isSubmitting) return; // prevents double submission
        isSubmitting = true;
        document.getElementById("btnSave").disabled = true;
        clearErrors();

        const id = document.getElementById("customerId").value;
        const payload = {
            fullName: document.getElementById("fullName").value,
            email: document.getElementById("email").value,
            phone: document.getElementById("phone").value,
            address: document.getElementById("address").value || null,
            ...(id ? { status: document.getElementById("status").value } : {})
        };

        try {
            const res = await fetch(id ? `/api/customers/${id}` : "/api/customers", {
                method: id ? "PUT" : "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": antiForgeryToken()
                },
                body: JSON.stringify(payload)
            });

            if (res.status === 200 || res.status === 201) {
                modal.hide();
                showToast(id ? "Customer updated." : "Customer created.");
                CustomerGrid.refresh();
            } else if (res.status === 400) {
                const problem = await res.json();
                renderFieldErrors(problem.errors);
            } else if (res.status === 404) {
                showToast("That customer no longer exists.", true);
                modal.hide();
                CustomerGrid.refresh();
            } else if (res.status === 409) {
                const problem = await res.json();
                renderFieldErrors({ Email: [problem.message] });
            } else {
                showToast("Something went wrong. Please try again.", true);
            }
        } catch {
            showToast("Network error. Please try again.", true);
        } finally {
            isSubmitting = false;
            document.getElementById("btnSave").disabled = false;
        }
    });

    async function handleDelete(id, name) {
        if (!confirm(`Delete customer "${name}"? This cannot be undone.`)) return;

        try {
            const res = await fetch(`/api/customers/${id}`, {
                method: "DELETE",
                headers: { "RequestVerificationToken": antiForgeryToken() }
            });

            if (res.status === 204) {
                showToast("Customer deleted.");
                CustomerGrid.refresh();
            } else if (res.status === 404) {
                showToast("Customer was already removed.", true);
                CustomerGrid.refresh();
            } else {
                showToast("Could not delete customer.", true);
            }
        } catch {
            showToast("Network error. Please try again.", true);
        }
    }

    function renderFieldErrors(errors) {
        for (const [field, messages] of Object.entries(errors || {})) {
            const el = document.getElementById(`err-${field}`);
            const input = document.getElementById(field.charAt(0).toLowerCase() + field.slice(1));
            if (el) { el.textContent = messages.join(" "); el.style.display = "block"; }
            if (input) input.classList.add("is-invalid");
        }
    }

    function clearErrors() {
        document.querySelectorAll(".invalid-feedback").forEach(el => { el.textContent = ""; el.style.display = "none"; });
        document.querySelectorAll(".is-invalid").forEach(el => el.classList.remove("is-invalid"));
    }

    function showToast(message, isError = false) {
        document.getElementById("toastBody").textContent = message;
        toastEl.classList.toggle("text-bg-success", !isError);
        toastEl.classList.toggle("text-bg-danger", isError);
        toast.show();
    }

    async function fetchCustomers() {
        loadingEl.classList.remove("d-none");
        errorEl.classList.add("d-none");
        emptyEl.classList.add("d-none");

        const params = new URLSearchParams({
            search: state.search, sortBy: state.sortBy, descending: state.descending,
            page: state.page, pageSize: state.pageSize
        });

        try {
            const res = await fetch(`/api/customers?${params}`);
            if (!res.ok) throw new Error(`Request failed (${res.status})`);
            const data = await res.json();
            renderRows(data.items);
            renderPager(data.totalCount);
            emptyEl.classList.toggle("d-none", data.items.length > 0);
        } catch (err) {
            errorEl.textContent = "Could not load customers. Please try again.";
            errorEl.classList.remove("d-none");
        } finally {
            loadingEl.classList.add("d-none");
        }
    }

    function renderRows(items) {
        rowsEl.innerHTML = "";
        for (const c of items) {
            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td>${escapeHtml(c.customerNumber)}</td>
                <td>${escapeHtml(c.fullName)}</td>
                <td>${escapeHtml(c.email)}</td>
                <td>${escapeHtml(c.phone)}</td>
                <td><span class="badge ${c.status === 'Active' ? 'bg-success' : 'bg-secondary'}">${c.status}</span></td>
                <td>${new Date(c.updatedAtUtc).toLocaleString()}</td>
                <td>
                    <button class="btn btn-sm btn-outline-primary btn-edit" data-id="${c.id}">Edit</button>
                    <button class="btn btn-sm btn-outline-danger btn-delete" data-id="${c.id}" data-name="${escapeHtml(c.fullName)}">Delete</button>
                </td>`;
            rowsEl.appendChild(tr);
        }
    }

    // Never insert raw user input as HTML — this is the "encode displayed values" requirement.
    function escapeHtml(str) {
        const div = document.createElement("div");
        div.textContent = str ?? "";
        return div.innerHTML;
    }

    function renderPager(totalCount) {
        pagerEl.innerHTML = "";
        const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize));
        for (let p = 1; p <= totalPages; p++) {
            const btn = document.createElement("button");
            btn.className = `btn btn-sm ${p === state.page ? "btn-primary" : "btn-outline-primary"}`;
            btn.textContent = p;
            btn.onclick = () => { state.page = p; fetchCustomers(); };
            pagerEl.appendChild(btn);
        }
    }

    document.getElementById("searchBox").addEventListener("input", (e) => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            state.search = e.target.value;
            state.page = 1;
            fetchCustomers();
        }, 300);
    });

    document.querySelectorAll("th[data-sort]").forEach(th => {
        th.addEventListener("click", () => {
            const col = th.dataset.sort;
            state.descending = state.sortBy === col ? !state.descending : false;
            state.sortBy = col;
            fetchCustomers();
        });
    });

    fetchCustomers();

    window.CustomerGrid = { refresh: fetchCustomers, state };
})();