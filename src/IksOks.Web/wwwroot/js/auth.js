const loginTab = document.querySelector("#login-tab");
const registerTab = document.querySelector("#register-tab");
const authForm = document.querySelector("#auth-form");
const usernameInput = document.querySelector("#username");
const passwordInput = document.querySelector("#password");
const submitButton = document.querySelector("#submit-button");
const message = document.querySelector("#message");

let mode = "login";

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

    showMessage(
        `Uspešno ste prijavljeni kao ${user.userName}.`,
        "success");

    authForm.reset();
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

    showMessage(
        `Već ste prijavljeni kao ${user.userName}.`,
        "success");
}