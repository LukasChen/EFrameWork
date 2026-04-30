document.addEventListener("DOMContentLoaded", () => {
    const nav = document.querySelector(".site-nav");
    const toggle = document.querySelector(".nav-toggle");

    if (nav) {
        const defaultOpen = window.innerWidth > 980;
        nav.setAttribute("data-open", defaultOpen ? "true" : "false");
    }

    if (toggle && nav) {
        toggle.addEventListener("click", () => {
            const current = nav.getAttribute("data-open") === "true";
            nav.setAttribute("data-open", current ? "false" : "true");
        });
    }

    const pagePath = window.location.pathname.replace(/\\/g, "/");
    const links = document.querySelectorAll(".nav-links a");
    links.forEach((link) => {
        const href = link.getAttribute("href");
        if (!href) {
            return;
        }

        const normalized = new URL(href, window.location.href).pathname.replace(/\\/g, "/");
        if (normalized === pagePath || (pagePath.endsWith("/") && normalized === `${pagePath}index.html`)) {
            link.classList.add("is-active");
        }
    });
});