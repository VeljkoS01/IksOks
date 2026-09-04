const loginTab = document.querySelector("#login-tab");
const registerTab = document.querySelector("#register-tab");
const authForm = document.querySelector("#auth-form");
const usernameInput = document.querySelector("#username");
const passwordInput = document.querySelector("#password");
const submitButton = document.querySelector("#submit-button");
const message = document.querySelector("#message");
const authPage = document.querySelector("#auth-page");
const appPage = document.querySelector("#app-page");
const welcomeText = document.querySelector("#welcome-text");
const logoutButton = document.querySelector("#logout-button");
const createMatchForm = document.querySelector("#create-match-form");
const boardSizeInput = document.querySelector("#board-size");
const winLengthInput = document.querySelector("#win-length");
const matchMessage = document.querySelector("#match-message");
const matchesList = document.querySelector("#matches-list");
const refreshMatchesButton = document.querySelector("#refresh-matches-button");
const lobbyView = document.querySelector("#lobby-view");
const matchView = document.querySelector("#match-view");
const matchTitle = document.querySelector("#match-title");
const matchStatus = document.querySelector("#match-status");
const playerSymbol = document.querySelector("#player-symbol");
const gameBoard = document.querySelector("#game-board");
const gameMessage = document.querySelector("#game-message");
const backToLobbyButton = document.querySelector("#back-to-lobby-button");
const activeMatchesList = document.querySelector("#active-matches-list");
const matchHistoryList = document.querySelector("#match-history-list");
const refreshMyMatchesButton = document.querySelector("#refresh-my-matches-button");

let mode = "login";
let currentUser = null;
let activeMatchId = null;
let hubConnection = null;
let hubStartPromise = null;

loginTab.addEventListener("click", () => {
    setMode("login");
});

registerTab.addEventListener("click", () => {
    setMode("register");
});

function setMode(newMode) {
    mode = newMode;

    const isLogin = mode === "login";

    loginTab.classList.toggle("active", isLogin);
    registerTab.classList.toggle("active", !isLogin);

    submitButton.textContent = isLogin
        ? "Uloguj se"
        : "Registruj se";

    passwordInput.autocomplete = isLogin
        ? "current-password"
        : "new-password";

    clearMessage();
}

authForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    clearMessage();

    const userName = usernameInput.value.trim();
    const password = passwordInput.value;

    if (!userName || !password) {
        showMessage(
            "Unesite korisničko ime i lozinku.",
            "error");

        return;
    }

    if (mode === "register") {
        await register(userName, password);
        return;
    }

    await login(userName, password);
});

async function register(userName, password) {
    const response = await fetch("/api/auth/register", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            userName,
            password
        })
    });

    if (response.ok) {
        showMessage(
            "Registracija je uspešna. Možete da se prijavite.",
            "success");

        passwordInput.value = "";
        setMode("login");

        return;
    }

    const errorMessage = await readError(response);

    showMessage(errorMessage, "error");
}

async function login(userName, password) {
    const response = await fetch("/api/auth/login", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            userName,
            password
        })
    });

    if (!response.ok) {
        showMessage(
            "Pogrešno korisničko ime ili lozinka.",
            "error");

        return;
    }

    const user = await response.json();

    showAuthenticatedUser(user);
}

async function readError(response) {
    try {
        const body = await response.json();

        if (body.error) {
            return body.error;
        }
    } catch {
        //Response body nije JSON.
    }

    return "Došlo je do greške.";
}

function showMessage(text, type) {
    message.textContent = text;
    message.className = `message ${type}`;
}

function clearMessage() {
    message.textContent = "";
    message.className = "message";
}

checkCurrentUser();

async function checkCurrentUser() {
    const response = await fetch("/api/auth/me");

    if (!response.ok) {
        return;
    }

    const user = await response.json();

    showAuthenticatedUser(user);
}

function showAuthenticatedUser(user) {
    currentUser = user;

    authPage.classList.add("hidden");
    appPage.classList.remove("hidden");

    welcomeText.textContent =
        `Dobrodošli, ${user.userName}!`;

    clearMessage();

    void loadMatches();
    void loadMyMatches();
    void ensureRealtimeConnected();
}

function showAuthPage() {
    activeMatchId = null;
    currentUser = null;

    matchesList.replaceChildren();
    activeMatchesList.replaceChildren();
    matchHistoryList.replaceChildren();

    appPage.classList.add("hidden");
    authPage.classList.remove("hidden");

    welcomeText.textContent = "";

    authForm.reset();
    setMode("login");
}

logoutButton.addEventListener("click", async () => {
    const response = await fetch(
        "/api/auth/logout",
        {
            method: "POST"
        });

    if (!response.ok) {
        return;
    }

    await stopRealtimeConnection();
    showAuthPage();
});

boardSizeInput.addEventListener("input", () => {
    const boardSize = Number(boardSizeInput.value);

    winLengthInput.max = String(boardSize);

    if (Number(winLengthInput.value) > boardSize) {
        winLengthInput.value = String(boardSize);
    }
});

createMatchForm.addEventListener(
    "submit",
    async (event) => {
        event.preventDefault();

        const boardSize = Number(boardSizeInput.value);
        const winLength = Number(winLengthInput.value);

        const response = await fetch("/api/matches", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                boardSize,
                winLength
            })
        });

        if (!response.ok) {
            const error = await readError(response);

            showMatchMessage(error, "error");
            return;
        }

        const match = await response.json();

        showMatchMessage(
            "Meč je uspešno napravljen.",
            "success");

        await openMatch(match.id);
    });

refreshMatchesButton.addEventListener(
    "click",
    loadMatches);

async function loadMatches() {
    try {
        const response = await fetch("/api/matches");

        if (!response.ok) {
            return;
        }

        const matches = await response.json();

        renderMatches(matches);
    } catch {
        showMatchMessage(
            "Nije moguće učitati mečeve.",
            "error");
    }
}

function renderMatches(matches) {
    matchesList.replaceChildren();

    if (matches.length === 0) {
        const empty = document.createElement("p");

        empty.className = "empty-state";
        empty.textContent = "Trenutno nema dostupnih mečeva.";

        matchesList.append(empty);
        return;
    }

    for (const match of matches) {
        const card = document.createElement("article");
        card.className = "match-card";

        const info = document.createElement("div");
        info.className = "match-info";

        const owner = document.createElement("span");
        owner.className = "match-owner";
        owner.textContent = match.ownerUserName;

        const details = document.createElement("span");
        details.className = "match-details";
        details.textContent =
            `${match.boardSize}×${match.boardSize} · ` +
            `${match.winLength} za pobedu`;

        info.append(owner, details);

        const button = document.createElement("button");
        button.className = "secondary-button";
        button.type = "button";

        const isOwnMatch =
            currentUser &&
            match.ownerUserId === currentUser.id;

        if (isOwnMatch) {
            button.textContent = "Tvoj meč";
            button.disabled = true;
        } else {
            button.textContent = "Pridruži se";

            button.addEventListener("click", async () => {
               await joinMatch(match.id);
            });
        }

        card.append(info, button);
        matchesList.append(card);
    }
}

async function joinMatch(matchId) {
    try {
        const response = await fetch(
            `/api/matches/${matchId}/join`,
            {
                method: "POST"
            });

        if (!response.ok) {
            const error = await readError(response);

            showMatchMessage(error, "error");

            await loadMatches();
            return;
        }

        const match = await response.json();

        await openMatch(match.id);

    } catch {
        showMatchMessage(
            "Nije moguće pridružiti se meču.",
            "error");
    }
}

function showMatchMessage(text, type) {
    matchMessage.textContent = text;
    matchMessage.className = `message ${type}`;
}

async function openMatch(matchId) {
    activeMatchId = matchId;

    lobbyView.classList.add("hidden");
    matchView.classList.remove("hidden");

    backToLobbyButton.classList.remove("hidden");

    await joinMatchGroup(matchId);
    await loadActiveMatch();
}


async function loadActiveMatch() {
    if (!activeMatchId) {
        return;
    }

    try {
        const response = await fetch(
            `/api/matches/${activeMatchId}`);

        if (!response.ok) {
            gameMessage.textContent =
                "Nije moguće učitati meč.";

            return;
        }

        const match = await response.json();

        renderMatch(match);
    } catch {
        gameMessage.textContent =
            "Server trenutno nije dostupan.";
    }
}

function renderMatch(match) {
    const isOwner =
        currentUser.id === match.ownerUserId;

    const isOpponent =
        currentUser.id === match.opponentUserId;

    const mySymbol = isOwner
        ? "X"
        : isOpponent
            ? "O"
            : "-";

    playerSymbol.textContent = mySymbol;

    const opponentName = isOwner
        ? match.opponentUserName
        : match.ownerUserName;

    matchTitle.textContent = opponentName
        ? `Meč protiv ${opponentName}`
        : "Čekanje protivnika";

    renderBoard(match);

    updateMatchStatus(match);
}

function renderBoard(match) {
    gameBoard.replaceChildren();

    gameBoard.style.gridTemplateColumns =
        `repeat(${match.boardSize}, 1fr)`;

    const movesByPosition = new Map();

    for (const move of match.moves) {
        movesByPosition.set(
            `${move.row}:${move.column}`,
            move);
    }

    const isMyTurn =
        match.currentTurnUserId === currentUser.id;

    for (let row = 0; row < match.boardSize; row++) {
        for (
            let column = 0;
            column < match.boardSize;
            column++
        ) {
            const button = document.createElement("button");

            button.className = "board-cell";
            button.type = "button";

            const move =
                movesByPosition.get(`${row}:${column}`);

            if (move) {
                button.textContent = move.symbol;

                button.classList.add(
                    move.symbol === "X"
                        ? "symbol-x"
                        : "symbol-o");
            }

            const canPlay =
                match.status === "InProgress" &&
                isMyTurn &&
                !move;

            button.disabled = !canPlay;

            if (canPlay) {
                button.addEventListener(
                    "click",
                    () => makeMove(row, column));
            }

            gameBoard.append(button);
        }
    }
}

function updateMatchStatus(match) {
    gameMessage.className = "message";

    if (match.status === "WaitingForOpponent") {
        matchStatus.textContent =
            "Čeka se protivnik...";

        gameMessage.textContent =
            "Meč će početi kada se drugi igrač pridruži.";

        return;
    }

    if (match.status === "Finished") {
        matchStatus.textContent =
            "Meč je završen.";

        if (match.winnerUserId === null) {
            gameMessage.textContent =
                "Partija je završena nerešeno.";
        } else if (
            match.winnerUserId === currentUser.id
        ) {
            gameMessage.textContent =
                "Pobedili ste!";
            gameMessage.classList.add("success");
        } else {
            gameMessage.textContent =
                `${match.winnerUserName} je pobedio.`;
        }

        return;
    }

    if (
        match.currentTurnUserId === currentUser.id
    ) {
        matchStatus.textContent =
            "Tvoj potez.";

        gameMessage.textContent =
            "Izaberi slobodno polje.";
    } else {
        matchStatus.textContent =
            "Protivnikov potez.";

        gameMessage.textContent =
            "Sačekajte da protivnik odigra.";
    }
}

async function makeMove(row, column) {
    if (!activeMatchId) {
        return;
    }

    try {
        const response = await fetch(
            `/api/matches/${activeMatchId}/moves`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    row,
                    column
                })
            });

        if (!response.ok) {
            const error = await readError(response);

            gameMessage.textContent = error;

            await loadActiveMatch();

            return;
        }

        await loadActiveMatch();
    } catch {
        gameMessage.textContent =
            "Potez nije moguće odigrati.";
    }
}

backToLobbyButton.addEventListener(
    "click",
    async () => {
        await closeMatch();
    });

async function closeMatch() {
    const matchId = activeMatchId;

    activeMatchId = null;

    if (matchId) {
        await leaveMatchGroup(matchId);
    }

    gameBoard.replaceChildren();

    matchView.classList.add("hidden");
    lobbyView.classList.remove("hidden");

    await Promise.all([
        loadMatches(),
        loadMyMatches()
    ]);
}

function createHubConnection() {
    if (hubConnection !== null) {
        return;
    }

    hubConnection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/match")
        .withAutomaticReconnect()
        .build();

    hubConnection.on(
        "MatchUpdated",
        async matchId => {
            if (!activeMatchId) {
                return;
            }

            const currentId =
                activeMatchId.toLowerCase();

            const updatedId =
                String(matchId).toLowerCase();

            if (currentId !== updatedId) {
                return;
            }

            await loadActiveMatch();
        });

    hubConnection.on(
        "LobbyUpdated",
        async () => {
            if (!currentUser) {
                return;
            }

            if (
                lobbyView.classList.contains("hidden")
            ) {
                return;
            }

            await Promise.all([
                loadMatches(),
                loadMyMatches()
            ]);
        });

    hubConnection.onreconnected(
        async () => {
            if (activeMatchId) {
                await joinMatchGroup(
                    activeMatchId);
            } else if (currentUser) {
                await loadMatches();
            }
        });

    hubConnection.onreconnecting(
        () => {
            if (!activeMatchId) {
                return;
            }

            gameMessage.textContent =
                "Live veza se ponovo uspostavlja...";
        });

    hubConnection.onclose(
        () => {
            if (!activeMatchId) {
                return;
            }

            gameMessage.textContent =
                "Live veza trenutno nije dostupna.";
        });
}

async function ensureRealtimeConnected() {
    createHubConnection();

    if (
        hubConnection.state ===
        signalR.HubConnectionState.Connected
    ) {
        return true;
    }

    if (
        hubConnection.state ===
        signalR.HubConnectionState.Reconnecting
    ) {
        return false;
    }

    if (hubStartPromise !== null) {
        try {
            await hubStartPromise;

            return (
                hubConnection.state ===
                signalR.HubConnectionState.Connected
            );
        } catch {
            return false;
        }
    }

    hubStartPromise = hubConnection.start();

    try {
        await hubStartPromise;

        return true;
    } catch (error) {
        console.error(
            "SignalR connection failed:",
            error);

        return false;
    } finally {
        hubStartPromise = null;
    }
}

async function joinMatchGroup(matchId) {
    const connected =
        await ensureRealtimeConnected();

    if (!connected) {
        return;
    }

    try {
        await hubConnection.invoke(
            "JoinMatch",
            matchId);
    } catch (error) {
        console.error(
            "Could not join SignalR match group:",
            error);
    }
}
async function leaveMatchGroup(matchId) {
    if (
        !hubConnection ||
        hubConnection.state !==
        signalR.HubConnectionState.Connected
    ) {
        return;
    }

    try {
        await hubConnection.invoke(
            "LeaveMatch",
            matchId);
    } catch (error) {
        console.error(
            "Could not leave SignalR match group:",
            error);
    }
}
async function stopRealtimeConnection() {
    if (hubConnection === null) {
        return;
    }

    try {
        await hubConnection.stop();
    } catch (error) {
        console.error(
            "Could not stop SignalR connection:",
            error);
    }

    hubConnection = null;
    hubStartPromise = null;
}

async function loadMyMatches() {
    try {
        const [
            activeResponse,
            historyResponse
        ] = await Promise.all([
            fetch("/api/matches/mine/active"),
            fetch("/api/matches/mine/history")
        ]);

        if (activeResponse.ok) {
            const activeMatches =
                await activeResponse.json();

            renderActiveMatches(activeMatches);
        } else {
            activeMatchesList.replaceChildren();

            const error =
                document.createElement("p");

            error.className = "empty-state";
            error.textContent =
                "Nije moguće učitati aktivne mečeve.";

            activeMatchesList.append(error);
        }

        if (historyResponse.ok) {
            const historyMatches =
                await historyResponse.json();

            renderMatchHistory(historyMatches);
        } else {
            matchHistoryList.replaceChildren();

            const error =
                document.createElement("p");

            error.className = "empty-state";
            error.textContent =
                "Nije moguće učitati istoriju mečeva.";

            matchHistoryList.append(error);
        }
    } catch (error) {
        console.error(
            "Could not load user matches:",
            error);

        activeMatchesList.replaceChildren();
        matchHistoryList.replaceChildren();

        const message =
            document.createElement("p");

        message.className = "empty-state";
        message.textContent =
            "Nije moguće učitati vaše mečeve.";

        activeMatchesList.append(message);
    }
}

function renderActiveMatches(matches) {
    activeMatchesList.replaceChildren();

    if (matches.length === 0) {
        const empty =
            document.createElement("p");

        empty.className = "empty-state";
        empty.textContent =
            "Nemate aktivne mečeve.";

        activeMatchesList.append(empty);
        return;
    }

    for (const match of matches) {
        const card =
            document.createElement("article");

        card.className = "match-card";

        const info =
            document.createElement("div");

        info.className = "match-info";

        const isOwner =
            match.ownerUserId === currentUser.id;

        const opponentName = isOwner
            ? match.opponentUserName
            : match.ownerUserName;

        const title =
            document.createElement("span");

        title.className = "match-owner";

        title.textContent = opponentName
            ? `Protiv ${opponentName}`
            : "Čeka se protivnik";

        const details =
            document.createElement("span");

        details.className = "match-details";

        const statusText =
            match.status === "InProgress"
                ? "U toku"
                : "Čeka protivnika";

        details.textContent =
            `${match.boardSize}×${match.boardSize}` +
            ` · ${match.winLength} za pobedu` +
            ` · ${statusText}`;

        info.append(title, details);

        const button =
            document.createElement("button");

        button.className = "secondary-button";
        button.type = "button";

        button.textContent =
            match.status === "InProgress"
                ? "Nastavi"
                : "Otvori";

        button.addEventListener(
            "click",
            async () => {
                await openMatch(match.id);
            });

        card.append(info, button);

        activeMatchesList.append(card);
    }
}

function renderMatchHistory(matches) {
    matchHistoryList.replaceChildren();

    if (matches.length === 0) {
        const empty =
            document.createElement("p");

        empty.className = "empty-state";
        empty.textContent =
            "Još nemate završenih mečeva.";

        matchHistoryList.append(empty);
        return;
    }

    for (const match of matches) {
        const card =
            document.createElement("article");

        card.className = "match-card";

        const info =
            document.createElement("div");

        info.className = "match-info";

        const isOwner =
            match.ownerUserId === currentUser.id;

        const opponentName = isOwner
            ? match.opponentUserName
            : match.ownerUserName;

        const title =
            document.createElement("span");

        title.className = "match-owner";

        title.textContent =
            `Protiv ${opponentName ?? "nepoznatog igrača"}`;

        const result =
            document.createElement("span");

        result.className =
            "personal-match-result";

        if (match.winnerUserId === null) {
            result.textContent = "Nerešeno";
        } else if (
            match.winnerUserId === currentUser.id
        ) {
            result.textContent = "Pobeda";
        } else {
            result.textContent = "Poraz";
        }

        const details =
            document.createElement("span");

        details.className = "match-details";

        details.textContent =
            `${match.boardSize}×${match.boardSize}` +
            ` · ${match.winLength} za pobedu`;

        const date =
            document.createElement("span");

        date.className = "personal-match-date";

        const finishedAt =
            match.finishedAt ?? match.createdAt;

        date.textContent =
            new Date(finishedAt)
                .toLocaleString("sr-RS");

        info.append(
            title,
            result,
            details,
            date);

        const button =
            document.createElement("button");

        button.className = "secondary-button";
        button.type = "button";
        button.textContent = "Pogledaj";

        button.addEventListener(
            "click",
            async () => {
                await openMatch(match.id);
            });

        card.append(info, button);

        matchHistoryList.append(card);
    }
}

refreshMyMatchesButton.addEventListener(
    "click",
    loadMyMatches);