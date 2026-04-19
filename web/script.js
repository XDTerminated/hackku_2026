(() => {
    const ENDPOINT_KEY = "hackku.leaderboard.endpoint";
    const REFRESH_MS = 5000;
    const LIMIT = 50;

    const endpointInput = document.getElementById("endpoint");
    const statusDot = document.getElementById("status-dot");
    const statusText = document.getElementById("status-text");
    const refreshBtn = document.getElementById("refresh");
    const podium = document.getElementById("podium");
    const tbody = document.getElementById("board-body");
    const empty = document.getElementById("empty");
    const updated = document.getElementById("updated");

    endpointInput.value = localStorage.getItem(ENDPOINT_KEY) || "http://localhost:3000";

    endpointInput.addEventListener("change", () => {
        localStorage.setItem(ENDPOINT_KEY, endpointInput.value.trim());
        fetchBoard();
    });
    refreshBtn.addEventListener("click", fetchBoard);

    let inFlight = false;

    async function fetchBoard() {
        if (inFlight) return;
        inFlight = true;
        setStatus("loading", "loading…");
        try {
            const base = endpointInput.value.trim().replace(/\/+$/, "");
            const res = await fetch(`${base}/api/leaderboard?limit=${LIMIT}`, {
                headers: { accept: "application/json" },
            });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const data = await res.json();
            const entries = (data && data.entries) || [];
            render(entries);
            setStatus("ok", `live · ${entries.length} player${entries.length === 1 ? "" : "s"}`);
            updated.textContent = "updated " + new Date().toLocaleTimeString();
        } catch (err) {
            setStatus("err", "offline");
            updated.textContent = "last error: " + err.message;
        } finally {
            inFlight = false;
        }
    }

    function setStatus(kind, label) {
        statusDot.classList.remove("ok", "err");
        if (kind === "ok") statusDot.classList.add("ok");
        if (kind === "err") statusDot.classList.add("err");
        statusText.textContent = label;
    }

    function render(entries) {
        renderPodium(entries.slice(0, 3));
        renderTable(entries);
        empty.classList.toggle("hidden", entries.length > 0);
    }

    function renderPodium(top) {
        podium.innerHTML = "";
        if (top.length === 0) return;
        const order = [1, 0, 2].filter((i) => i < top.length);
        for (const i of order) {
            const e = top[i];
            const rank = i + 1;
            const card = document.createElement("div");
            card.className = `card rank-${rank}`;
            card.innerHTML = `
                <div class="medal">${medalFor(rank)}</div>
                <div class="name" title="${escapeHtml(e.display_name)}">${escapeHtml(e.display_name)}</div>
                <div class="score-row">
                    <div class="score">${clampInt(e.composite_score)}</div>
                    <div class="score-label">composite · yr ${e.year ?? 0}</div>
                </div>
            `;
            podium.appendChild(card);
        }
    }

    function renderTable(entries) {
        tbody.innerHTML = "";
        entries.forEach((e, i) => {
            const rank = e.rank ?? i + 1;
            const row = document.createElement("tr");
            row.innerHTML = `
                <td class="col-rank">${rankPill(rank)}</td>
                <td class="col-name">
                    <div class="player-cell">
                        <div class="avatar">${initials(e.display_name)}</div>
                        <div class="player-name" title="${escapeHtml(e.display_name)}">${escapeHtml(e.display_name)}</div>
                    </div>
                </td>
                <td class="col-score">
                    <div class="score-cell">
                        <span class="score-num">${clampInt(e.composite_score)}</span>
                        ${bar(e.composite_score)}
                    </div>
                </td>
                <td>${statCell(e.happiness)}</td>
                <td>${statCell(e.hunger)}</td>
                <td>${statCell(e.hygiene)}</td>
                <td>${statCell(debtPaidPct(e))}</td>
                <td class="col-money"><span class="money">${money(e.money)}</span></td>
                <td class="col-money"><span class="money">${money(e.invested)}</span></td>
                <td>${e.year ?? 0}</td>
            `;
            tbody.appendChild(row);
        });
    }

    function rankPill(rank) {
        const cls = rank === 1 ? "gold" : rank === 2 ? "silver" : rank === 3 ? "bronze" : "";
        return `<span class="rank-pill ${cls}">${rank}</span>`;
    }

    function statCell(valRaw) {
        const v = clampInt(valRaw);
        return `
            <div class="stat-cell">
                <span class="val">${v}</span>
                ${bar(v)}
            </div>
        `;
    }

    function bar(valRaw) {
        const v = Math.max(0, Math.min(100, Number(valRaw) || 0));
        const danger = v < 30 ? " danger" : "";
        return `<span class="bar${danger}"><span style="width:${v}%"></span></span>`;
    }

    function debtPaidPct(e) {
        const start = Number(e.starting_debt) || 0;
        const debt = Number(e.debt) || 0;
        if (start <= 0) return 100;
        return Math.max(0, Math.min(100, Math.round((1 - debt / start) * 100)));
    }

    function money(n) {
        const v = Number(n) || 0;
        const sign = v < 0 ? "-" : "";
        const abs = Math.abs(v);
        return `${sign}$${abs.toLocaleString()}`;
    }

    function medalFor(rank) {
        return rank === 1 ? "01" : rank === 2 ? "02" : "03";
    }

    function initials(name) {
        if (!name) return "?";
        const parts = String(name).trim().split(/\s+/);
        const a = parts[0]?.[0] ?? "";
        const b = parts.length > 1 ? parts[parts.length - 1][0] : parts[0]?.[1] ?? "";
        return (a + b).toUpperCase();
    }

    function clampInt(n) {
        const v = Math.round(Number(n) || 0);
        return v;
    }

    function escapeHtml(s) {
        return String(s ?? "").replace(/[&<>"']/g, (c) => ({
            "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;",
        }[c]));
    }

    fetchBoard();
    setInterval(fetchBoard, REFRESH_MS);
})();
