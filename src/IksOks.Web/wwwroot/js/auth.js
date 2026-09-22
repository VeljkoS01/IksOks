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
const profileSummary = document.querySelector("#profile-summary");
const refreshProfileButton = document.querySelector("#refresh-profile-button");
const leaderboardList = document.querySelector("#leaderboard-list");
const refreshLeaderboardButton = document.querySelector("#refresh-leaderboard-button");
const tokenBalance = document.querySelector("#token-balance");
const storeList = document.querySelector("#store-list");
const refreshStoreButton = document.querySelector("#refresh-store-button");
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
const liveMatchesList = document.querySelector("#live-matches-list");
const refreshLiveMatchesButton = document.querySelector("#refresh-live-matches-button");
const matchModeInput = document.querySelector("#match-mode");
const matchVisibilityInput = document.querySelector("#match-visibility");
const privateMatchCodeInput = document.querySelector("#private-match-code");
const joinPrivateMatchButton = document.querySelector("#join-private-match-button");
const privateMatchMessage = document.querySelector("#private-match-message");
const matchJoinCode = document.querySelector("#match-join-code");
const profileCard = document.querySelector("#profile-card");
const playerBadge = document.querySelector("#player-badge");
const matchControls = document.querySelector("#match-controls");
const turnTimer = document.querySelector("#turn-timer");
const emojiChatMessages = document.querySelector("#emoji-chat-messages");
const emojiChatActions = document.querySelector("#emoji-chat-actions");
const clearBorderButton = document.querySelector("#clear-border-button");

const basicEmojis = [
    "😀",
    "😂",
    "😎",
    "🔥",
    "👏",
    "🤔",
    "😢",
    "😡"
];

let availableEmojis =
    [...basicEmojis];

let mode = "login";
let currentUser = null;
let activeMatchId = null;
let hubConnection = null;
let hubStartPromise = null;
let timerIntervalId = null;
let currentDeadline = null;

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

function resetMatchView() {
    stopTurnTimer();

    activeMatchId = null;

    turnTimer.textContent = "";
    matchControls.replaceChildren();
    gameBoard.replaceChildren();
    emojiChatMessages.replaceChildren();
    emojiChatActions.replaceChildren();

    matchTitle.textContent = "";
    matchStatus.textContent = "";
    playerSymbol.textContent = "";
    gameMessage.textContent = "";

    matchView.classList.add("hidden");
    lobbyView.classList.remove("hidden");
}

function showAuthenticatedUser(user) {
    currentUser = user;

    resetMatchView();

    authPage.classList.add("hidden");
    appPage.classList.remove("hidden");

    welcomeText.textContent =
        `Dobrodošli, ${user.userName}!`;

    clearMessage();

    void loadMatches();
    void loadMyMatches();
    void loadLiveMatches();
    void loadProfile();
    void loadStore();
    void loadLeaderboard();
    void ensureRealtimeConnected();
}

function showAuthPage() {
    stopTurnTimer();
    activeMatchId = null;
    currentUser = null;

    matchesList.replaceChildren();
    activeMatchesList.replaceChildren();
    liveMatchesList.replaceChildren();
    matchHistoryList.replaceChildren();
    profileSummary.replaceChildren();
    leaderboardList.replaceChildren();
    storeList.replaceChildren();
    tokenBalance.textContent =
        "Tokeni: -";

    availableEmojis =
        [...basicEmojis];

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

        const mode = matchModeInput.value;
        const visibility = matchVisibilityInput.value;
        const boardSize = Number(boardSizeInput.value);
        const winLength = Number(winLengthInput.value);

        const response = await fetch("/api/matches", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                mode,
                visibility,
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

joinPrivateMatchButton.addEventListener(
    "click",
    joinPrivateMatch);

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

        const modeText =
            match.mode === "Classic"
                ? "Classic"
                : "Connect-K";

        details.textContent =
            `${modeText} · ` +
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

async function joinPrivateMatch() {
    const joinCode =
        privateMatchCodeInput.value
            .trim()
            .toUpperCase();

    if (!joinCode) {
        privateMatchMessage.textContent =
            "Unesite kod privatnog meča.";

        return;
    }

    try {
        const response = await fetch(
            "/api/matches/join-private",
            {
                method: "POST",
                headers: {
                    "Content-Type":
                        "application/json"
                },
                body: JSON.stringify({
                    joinCode
                })
            });

        if (!response.ok) {
            const error =
                await readError(response);

            privateMatchMessage.textContent =
                error;

            return;
        }

        const match =
            await response.json();

        privateMatchCodeInput.value = "";
        privateMatchMessage.textContent = "";

        await openMatch(match.id);
    } catch {
        privateMatchMessage.textContent =
            "Nije moguće pridružiti se privatnom meču.";
    }
}

function showMatchMessage(text, type) {
    matchMessage.textContent = text;
    matchMessage.className = `message ${type}`;
}

async function openMatch(matchId) {

    emojiChatMessages.replaceChildren();
    emojiChatActions.replaceChildren();

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
            : "Gledalac";

    playerSymbol.textContent = mySymbol;

    const opponentName = isOwner
        ? match.opponentUserName
        : match.ownerUserName;

    const modeText =
        match.mode === "Classic"
            ? "Classic"
            : "Connect-K";

    matchTitle.textContent = opponentName
        ? `${modeText} protiv ${opponentName}`
        : `${modeText} · čekanje protivnika`;

    if (
        match.visibility === "Private" &&
        match.joinCode
    ) {
        matchJoinCode.textContent =
            `Kod privatnog meča: ${match.joinCode}`;

        matchJoinCode.classList.remove(
            "hidden");
    } else {
        matchJoinCode.textContent = "";

        matchJoinCode.classList.add(
            "hidden");
    }

    renderBoard(match);

    updateMatchStatus(match);
    updateTurnTimer(match);

    renderMatchControls(match);
    renderEmojiChatControls(match);
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

    const isParticipant = match.ownerUserId === currentUser.id || match.opponentUserId === currentUser.id;

    if (match.status === "WaitingForOpponent") {
        matchStatus.textContent =
            "Čeka se protivnik...";

        gameMessage.textContent =
            "Meč će početi kada se drugi igrač pridruži.";

        return;
    }

    if (match.status === "Paused") {

        matchStatus.textContent =
            "Meč je pauziran.";

        if (!isParticipant) {
            gameMessage.textContent =
                "Posmatrate pauziranu partiju.";

            return;
        }

        if (match.ownerUserId === currentUser.id) {
            gameMessage.textContent =
                "Možete nastaviti meč kada budete spremni.";
        } else {
            gameMessage.textContent =
                "Sačekajte da vlasnik meča nastavi partiju.";
        }

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

    if (!isParticipant) {
        matchStatus.textContent =
            "Meč je u toku.";

        gameMessage.textContent =
            "Posmatrate partiju uživo.";

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
    stopTurnTimer();
    const matchId = activeMatchId;

    activeMatchId = null;

    if (matchId) {
        await leaveMatchGroup(matchId);
    }

    gameBoard.replaceChildren();
    emojiChatMessages.replaceChildren();
    emojiChatActions.replaceChildren();

    matchView.classList.add("hidden");
    lobbyView.classList.remove("hidden");

    await Promise.all([
        loadMatches(),
        loadMyMatches(),
        loadLiveMatches(),
        loadProfile(),
        loadLeaderboard()
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
        "EmojiReceived",
        chatMessage => {
            if (!activeMatchId) {
                return;
            }

            const currentId =
                activeMatchId.toLowerCase();

            const messageMatchId =
                String(
                    chatMessage.matchId)
                    .toLowerCase();

            if (
                currentId !==
                messageMatchId
            ) {
                return;
            }

            appendEmojiMessage(
                chatMessage);
        });

    hubConnection.on(
        "BalanceUpdated",
        async () => {
            if (!currentUser) {
                return;
            }

            await Promise.all([
                loadProfile(),
                loadStore()
            ]);
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
                loadMyMatches(),
                loadLiveMatches(),
                loadProfile(),
                loadLeaderboard()
            ]);
        });

    hubConnection.onreconnected(
        async () => {
            if (activeMatchId) {
                await joinMatchGroup(
                    activeMatchId);
            } else if (currentUser) {
                await Promise.all([
                    loadMatches(),
                    loadMyMatches(),
                    loadLiveMatches(),
                    loadProfile(),
                    loadLeaderboard()
                ]);
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

async function loadStore() {
    try {
        const response =
            await fetch("/api/store");

        if (!response.ok) {
            return;
        }

        const store =
            await response.json();

        renderStore(store);

        applyActiveBorder(store.activeBorderKey);

        const ownedPremiumEmojis =
            store.items
                .filter(item =>
                    item.type === "Emoji" &&
                    item.isOwned)
                .map(item =>
                    item.value);

        availableEmojis = [
            ...basicEmojis,
            ...ownedPremiumEmojis
        ];
    } catch (error) {
        console.error(
            "Could not load store:",
            error);

        storeList.replaceChildren();

        const message =
            document.createElement("p");

        message.className =
            "empty-state";

        message.textContent =
            "Nije moguće učitati prodavnicu.";

        storeList.append(message);
    }
}

function renderStore(store) {
    tokenBalance.textContent =
        `Tokeni: ${store.tokenBalance}`;

    storeList.replaceChildren();

    for (const item of store.items) {
        const card =
            document.createElement("article");

        card.className = "store-item";

        if (item.isOwned) {
            card.classList.add(
                "store-item-owned");
        }

        const preview =
            document.createElement("div");

        preview.className =
            "store-item-preview";

        if (item.type === "Emoji") {
            preview.textContent =
                item.value;
        } else {
            preview.textContent = "XO";

            preview.classList.add(
                "border-preview",
                item.value);
        }

        const name =
            document.createElement("div");

        name.className =
            "store-item-name";

        name.textContent =
            item.name;

        const details =
            document.createElement("div");

        details.className =
            "store-item-details";

        details.textContent =
            `${item.type} · ${item.price} tokena`;

        const button =
            document.createElement("button");

        button.type = "button";
        button.className =
            "secondary-button";

        if (!item.isOwned) {
            button.textContent = "Kupi";

            button.addEventListener(
                "click",
                async () => {
                    await purchaseStoreItem(
                        item.key);
                });
        } else if (
            item.type === "Border" &&
            item.isActive
        ) {
            button.textContent = "Aktivan";
            button.disabled = true;
        } else if (
            item.type === "Border"
        ) {
            button.textContent = "Aktiviraj";

            button.addEventListener(
                "click",
                async () => {
                    await activateBorder(
                        item.key);
                });
        } else {
            button.textContent = "Kupljeno";
            button.disabled = true;
        }

        card.append(
            preview,
            name,
            details,
            button);

        storeList.append(card);
    }
}

async function purchaseStoreItem(
    itemKey) {
    try {
        const response =
            await fetch(
                `/api/store/${itemKey}/purchase`,
                {
                    method: "POST"
                });

        if (!response.ok) {
            const error =
                await readError(response);

            showMatchMessage(
                error,
                "error");

            return;
        }

        await Promise.all([
            loadStore(),
            loadProfile()
        ]);
    } catch (error) {
        console.error(
            "Could not purchase store item:",
            error);
    }
}

async function activateBorder(
    itemKey) {
    const response = await fetch(
        `/api/store/${itemKey}/activate`,
        {
            method: "POST"
        });

    if (!response.ok) {
        const error =
            await readError(response);

        showMatchMessage(
            error,
            "error");

        return;
    }

    await loadStore();
}

function applyActiveBorder(
    activeBorderKey) {
    const borderClasses = [
        "border-gold",
        "border-neon"
    ];

    for (const className of borderClasses) {
        profileCard.classList.remove(
            className);

        playerBadge.classList.remove(
            className);
    }

    if (!activeBorderKey) {
        return;
    }

    profileCard.classList.add(
        activeBorderKey);

    playerBadge.classList.add(
        activeBorderKey);
}

async function loadProfile() {
    try {
        const response =
            await fetch("/api/users/me/profile");

        if (!response.ok) {
            return;
        }

        const profile =
            await response.json();

        renderProfile(profile);
    } catch (error) {
        console.error(
            "Could not load profile:",
            error);

        profileSummary.replaceChildren();

        const message =
            document.createElement("p");

        message.className = "empty-state";
        message.textContent =
            "Nije moguće učitati profil.";

        profileSummary.append(message);
    }
}

function renderProfile(profile) {
    profileSummary.replaceChildren();

    const userName =
        document.createElement("p");

    userName.className =
        "profile-user-name";

    userName.textContent =
        profile.userName;

    const memberSince =
        document.createElement("p");

    memberSince.className =
        "profile-member-since";

    memberSince.textContent =
        "Član od: " +
        new Date(profile.memberSince)
            .toLocaleDateString("sr-RS");

    const stats =
        document.createElement("div");

    stats.className =
        "profile-stats-grid";

    const values = [
        ["Tokeni", profile.tokenBalance],
        ["Odigrano", profile.matchesPlayed],
        ["Pobede", profile.wins],
        ["Nerešeno", profile.draws],
        ["Porazi", profile.losses],
        ["Poeni", profile.points],
        ["Win rate", `${profile.winRate}%`]
    ];

    for (const [label, value] of values) {
        const stat =
            document.createElement("div");

        stat.className =
            "profile-stat";

        const strong =
            document.createElement("strong");

        strong.textContent = value;

        const text =
            document.createElement("span");

        text.textContent = label;

        stat.append(
            strong,
            text);

        stats.append(stat);
    }

    profileSummary.append(
        userName,
        memberSince,
        stats);
}

async function loadLeaderboard() {
    try {
        const response =
            await fetch("/api/users/leaderboard");

        if (!response.ok) {
            return;
        }

        const entries =
            await response.json();

        renderLeaderboard(entries);
    } catch (error) {
        console.error(
            "Could not load leaderboard:",
            error);

        leaderboardList.replaceChildren();

        const message =
            document.createElement("p");

        message.className = "empty-state";
        message.textContent =
            "Nije moguće učitati rang listu.";

        leaderboardList.append(message);
    }
}

function renderLeaderboard(entries) {
    leaderboardList.replaceChildren();

    if (entries.length === 0) {
        const empty =
            document.createElement("p");

        empty.className = "empty-state";
        empty.textContent =
            "Još nema rezultata za rang listu.";

        leaderboardList.append(empty);
        return;
    }

    for (const entry of entries) {
        const row =
            document.createElement("div");

        row.className =
            "leaderboard-row";

        if (
            currentUser &&
            entry.userId === currentUser.id
        ) {
            row.classList.add(
                "leaderboard-self");
        }

        const rank =
            document.createElement("span");

        rank.className =
            "leaderboard-rank";

        rank.textContent =
            `#${entry.rank}`;

        const player =
            document.createElement("div");

        player.className =
            "leaderboard-player";

        const name =
            document.createElement("strong");

        name.textContent =
            entry.userName;

        const details =
            document.createElement("span");

        details.className =
            "leaderboard-details";

        details.textContent =
            `${entry.wins}P · ` +
            `${entry.draws}N · ` +
            `${entry.losses}I · ` +
            `${entry.matchesPlayed} mečeva`;

        player.append(
            name,
            details);

        const points =
            document.createElement("span");

        points.className =
            "leaderboard-points";

        points.textContent =
            `${entry.points} poena`;

        row.append(
            rank,
            player,
            points);

        leaderboardList.append(row);
    }
}

async function loadLiveMatches() {
    try {
        const response =
            await fetch("/api/matches/live");

        if (!response.ok) {
            return;
        }

        const matches =
            await response.json();

        renderLiveMatches(matches);
    } catch (error) {
        console.error(
            "Could not load live matches:",
            error);

        liveMatchesList.replaceChildren();

        const message =
            document.createElement("p");

        message.className = "empty-state";
        message.textContent =
            "Nije moguće učitati mečeve uživo.";

        liveMatchesList.append(message);
    }
}

function renderLiveMatches(matches) {
    liveMatchesList.replaceChildren();

    if (matches.length === 0) {
        const empty =
            document.createElement("p");

        empty.className = "empty-state";
        empty.textContent =
            "Trenutno nema mečeva uživo.";

        liveMatchesList.append(empty);
        return;
    }

    for (const match of matches) {
        const card =
            document.createElement("article");

        card.className = "match-card";

        const info =
            document.createElement("div");

        info.className = "match-info";

        const title =
            document.createElement("span");

        title.className = "match-owner";
        title.textContent =
            `${match.ownerUserName} protiv ` +
            `${match.opponentUserName}`;

        const details =
            document.createElement("span");

        details.className = "match-details";

        const modeText =
            match.mode === "Classic"
                ? "Classic"
                : "Connect-K";

        const statusText =
            match.status === "Paused"
                ? "Pauziran"
                : "U toku";

        details.textContent =
            `${modeText} · ` +
            `${match.boardSize}×${match.boardSize}` +
            ` · ${match.winLength} za pobedu` +
            ` · ${statusText}`;

        info.append(
            title,
            details);

        const button =
            document.createElement("button");

        button.type = "button";
        button.className =
            "secondary-button";
        button.textContent = "Gledaj";

        button.addEventListener(
            "click",
            async () => {
                await openMatch(match.id);
            });

        card.append(
            info,
            button);

        liveMatchesList.append(card);
    }
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

        let statusText;

        if (match.status === "InProgress") {
            statusText = "U toku";
        } else if (match.status === "Paused") {
            statusText = "Pauziran";
        } else {
            statusText = "Čeka protivnika";
        }

        const modeText =
            match.mode === "Classic"
                ? "Classic"
                : "Connect-K";

        const visibilityText =
            match.visibility === "Private"
                ? "Privatan"
                : "Javan";

        details.textContent =
            `${modeText} · ` +
            `${match.boardSize}×${match.boardSize}` +
            ` · ${match.winLength} za pobedu` +
            ` · ${statusText}` +
            ` · ${visibilityText}`;

        info.append(title, details);

        const button =
            document.createElement("button");

        button.type = "button";
        button.className = "secondary-button";

        if (match.status === "WaitingForOpponent") {
            button.textContent = "Otvori meč";
        } else if (match.status === "Paused") {
            button.textContent = "Otvori meč";
        } else {
            button.textContent = "Nastavi meč";
        }

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

        const modeText =
            match.mode === "Classic"
                ? "Classic"
                : "Connect-K";

        const visibilityText =
            match.visibility === "Private"
                ? "Privatan"
                : "Javan";

        details.textContent =
            `${modeText} · ` +
            `${match.boardSize}×${match.boardSize}` +
            ` · ${match.winLength} za pobedu` +
            ` · ${visibilityText}`;

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

refreshLiveMatchesButton.addEventListener(
    "click",
    loadLiveMatches);

refreshProfileButton.addEventListener(
    "click",
    loadProfile);

refreshLeaderboardButton.addEventListener(
    "click",
    loadLeaderboard);

refreshStoreButton.addEventListener(
    "click",
    loadStore);

function updateMatchModeFields() {
    const mode = matchModeInput.value;

    if (mode === "Classic") {
        boardSizeInput.value = "3";
        boardSizeInput.min = "3";
        boardSizeInput.max = "3";
        boardSizeInput.disabled = true;

        winLengthInput.value = "3";
        winLengthInput.min = "3";
        winLengthInput.max = "3";
        winLengthInput.disabled = true;

        return;
    }

    boardSizeInput.disabled = false;
    winLengthInput.disabled = false;

    boardSizeInput.min = "3";
    boardSizeInput.max = "10";

    if (Number(boardSizeInput.value) < 3) {
        boardSizeInput.value = "3";
    }

    winLengthInput.min = "3";
    winLengthInput.max =
        boardSizeInput.value;

    if (
        Number(winLengthInput.value) >
        Number(boardSizeInput.value)
    ) {
        winLengthInput.value =
            boardSizeInput.value;
    }
}

matchModeInput.addEventListener(
    "change",
    updateMatchModeFields);

clearBorderButton.addEventListener(
    "click",
    async () => {
        const response = await fetch(
            "/api/store/active-border",
            {
                method: "DELETE"
            });

        if (response.ok) {
            await loadStore();
        }
    });

updateMatchModeFields();

function renderEmojiChatControls(match) {
    emojiChatActions.replaceChildren();

    const isParticipant =
        match.ownerUserId === currentUser.id ||
        match.opponentUserId === currentUser.id;

    const canSend =
        isParticipant &&
        (
            match.status === "InProgress" ||
            match.status === "Paused"
        );

    if (!canSend) {
        return;
    }

    for (const emoji of availableEmojis) {
        const button =
            document.createElement("button");

        button.type = "button";
        button.className =
            "emoji-chat-button";

        button.textContent = emoji;

        button.addEventListener(
            "click",
            async () => {
                await sendEmoji(emoji);
            });

        emojiChatActions.append(button);
    }
}

async function sendEmoji(emoji) {
    if (!activeMatchId) {
        return;
    }

    const connected =
        await ensureRealtimeConnected();

    if (!connected) {
        return;
    }

    try {
        await hubConnection.invoke(
            "SendEmoji",
            activeMatchId,
            emoji);
    } catch (error) {
        console.error(
            "Could not send emoji:",
            error);
    }
}

function appendEmojiMessage(chatMessage) {
    const row =
        document.createElement("div");

    row.className =
        "emoji-chat-message";

    if (
        chatMessage.userId ===
        currentUser.id
    ) {
        row.classList.add(
            "emoji-chat-message-self");
    }

    const userName =
        document.createElement("span");

    userName.textContent =
        chatMessage.userId === currentUser.id
            ? "Vi"
            : chatMessage.userName;

    const emoji =
        document.createElement("span");

    emoji.className =
        "emoji-chat-emoji";

    emoji.textContent =
        chatMessage.emoji;

    const time =
        document.createElement("span");

    time.className =
        "emoji-chat-time";

    time.textContent =
        new Date(chatMessage.sentAt)
            .toLocaleTimeString(
                "sr-RS",
                {
                    hour: "2-digit",
                    minute: "2-digit"
                });

    row.append(
        userName,
        emoji,
        time);

    emojiChatMessages.append(row);

    emojiChatMessages.scrollTop =
        emojiChatMessages.scrollHeight;
}

function renderMatchControls(match) {
    matchControls.replaceChildren();

    const isOwner =
        match.ownerUserId === currentUser.id;

    const isOpponent =
        match.opponentUserId === currentUser.id;

    if (match.status === "InProgress") {
        if (isOwner) {
            renderOwnerInProgressControls(match);
            return;
        }

        if (isOpponent) {
            renderOpponentInProgressControls(match);
        }

        return;
    }

    if (match.status === "Paused") {
        if (isOwner) {
            renderOwnerPausedControls(match);
            return;
        }

        if (isOpponent) {
            renderOpponentPausedControls(match);
        }

        return;
    }
}

function createControlButton(
    text,
    action) {
    const button =
        document.createElement("button");

    button.type = "button";
    button.className = "secondary-button";
    button.textContent = text;

    button.addEventListener(
        "click",
        async () => {
            button.disabled = true;

            try {
                await action();
            } finally {
                button.disabled = false;
            }
        });

    return button;
}

function renderOwnerInProgressControls(match) {
    if (match.pauseRequestedByUserId) {
        const requestText =
            document.createElement("p");

        requestText.className =
            "pause-request-text";

        requestText.textContent =
            `${match.pauseRequestedByUserName}` +
            " je zatražio pauzu.";

        const actions =
            document.createElement("div");

        actions.className =
            "match-control-actions";

        const acceptButton =
            createControlButton(
                "Prihvati pauzu",
                pauseMatch);

        const rejectButton =
            createControlButton(
                "Odbij zahtev",
                rejectPauseRequest);

        actions.append(
            acceptButton,
            rejectButton);

        matchControls.append(
            requestText,
            actions);

        return;
    }

    const pauseButton =
        createControlButton(
            "Pauziraj meč",
            pauseMatch);

    matchControls.append(
        pauseButton);
}

function renderOpponentInProgressControls(match) {
    if (
        match.pauseRequestedByUserId ===
        currentUser.id
    ) {
        const text =
            document.createElement("p");

        text.className =
            "pause-request-text";

        text.textContent =
            "Zahtev za pauzu je poslat.";

        matchControls.append(text);
        return;
    }

    const requestButton =
        createControlButton(
            "Zatraži pauzu",
            requestPause);

    matchControls.append(
        requestButton);
}

function renderOwnerPausedControls(match) {
    if (match.resumeRequestedByUserId) {
        const text =
            document.createElement("p");

        text.className =
            "pause-request-text";

        text.textContent =
            `${match.resumeRequestedByUserName}` +
            " je zatražio nastavak.";

        const actions =
            document.createElement("div");

        actions.className =
            "match-control-actions";

        const acceptButton =
            createControlButton(
                "Prihvati nastavak",
                resumeMatch);

        const rejectButton =
            createControlButton(
                "Odbij zahtev",
                rejectResumeRequest);

        actions.append(
            acceptButton,
            rejectButton);

        matchControls.append(
            text,
            actions);

        return;
    }

    matchControls.append(
        createControlButton(
            "Nastavi meč",
            resumeMatch));
}

function renderOpponentPausedControls(match) {
    if (
        match.resumeRequestedByUserId ===
        currentUser.id
    ) {
        const text =
            document.createElement("p");

        text.className =
            "pause-request-text";

        text.textContent =
            "Zahtev za nastavak je poslat.";

        matchControls.append(text);
        return;
    }

    matchControls.append(
        createControlButton(
            "Zatraži nastavak",
            requestResume));
}

function updateTurnTimer(match) {
    stopTurnTimer();

    if (
        match.status !== "InProgress" ||
        !match.turnDeadlineAt
    ) {
        turnTimer.textContent = "";
        return;
    }

    currentDeadline =
        new Date(match.turnDeadlineAt);

    renderRemainingTime();

    timerIntervalId =
        window.setInterval(
            renderRemainingTime,
            250);
}

function renderRemainingTime() {
    if (!currentDeadline) {
        return;
    }

    const milliseconds =
        currentDeadline.getTime() -
        Date.now();

    const seconds =
        Math.max(
            0,
            Math.ceil(
                milliseconds / 1000));

    turnTimer.textContent =
        `Vreme: ${seconds}s`;

    if (seconds === 0) {
        stopTurnTimer();
    }
}

function stopTurnTimer() {
    if (timerIntervalId !== null) {
        window.clearInterval(
            timerIntervalId);

        timerIntervalId = null;
    }

    currentDeadline = null;
}

async function postMatchControl(path) {
    if (!activeMatchId) {
        return;
    }

    const response = await fetch(
        `/api/matches/${activeMatchId}/${path}`,
        {
            method: "POST"
        });

    if (!response.ok) {
        const error =
            await readError(response);

        gameMessage.textContent = error;

        await loadActiveMatch();

        return;
    }

    await loadActiveMatch();
}

async function requestPause() {
    await postMatchControl(
        "pause-request");
}

async function pauseMatch() {
    await postMatchControl(
        "pause");
}

async function rejectPauseRequest() {
    await postMatchControl(
        "pause-request/reject");
}

async function resumeMatch() {
    await postMatchControl(
        "resume");
}

async function requestResume() {
    await postMatchControl(
        "resume-request");
}

async function rejectResumeRequest() {
    await postMatchControl(
        "resume-request/reject");
}

