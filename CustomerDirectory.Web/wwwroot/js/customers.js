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