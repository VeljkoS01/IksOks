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

let mode = "login";
let currentUser = null;

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

    loadMatches();
}

function showAuthPage() {
    currentUser = null;
    matchesList.replaceChildren();

    appPage.classList.add("hidden");
    authPage.classList.remove("hidden");

    welcomeText.textContent = "";

    authForm.reset();
    setMode("login");
}

logoutButton.addEventListener("click", async () => {
    try {
        const response = await fetch("/api/auth/logout", {
            method: "POST"
        });

        if (!response.ok) {
            return;
        }

        showAuthPage();
    } catch {
        //Ako server nije dostupan, ostavljamo trenutni ekran.
    }
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

        showMatchMessage(
            "Meč je uspešno napravljen.",
            "success");

        await loadMatches();
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

            button.addEventListener("click", () => {
                joinMatch(match.id);
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

        showMatchMessage(
            `Pridružili ste se meču protiv ` +
            `${match.ownerUserName}.`,
            "success");

        await loadMatches();
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