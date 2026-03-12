document.addEventListener("DOMContentLoaded", function () {
    // 1. Get the current page from the browser URL
    let currentPath = window.location.pathname.toLowerCase();
    let currentPage = currentPath.substring(currentPath.lastIndexOf('/') + 1);

    // 2. Default to Home if on the root domain
    if (currentPage === "" || currentPage === "homepage" || currentPage === "default.aspx") {
        currentPage = "home.aspx";
    }

    // 3. Select all links
    const navLinks = document.querySelectorAll('.site-nav ul li a');
    let matchFound = false;

    // 4. Loop through and apply the active class
    navLinks.forEach(link => {
        let linkPath = link.href.toLowerCase();
        let linkPage = linkPath.substring(linkPath.lastIndexOf('/') + 1);

        if (linkPage === currentPage) {
            link.classList.add('active');
            matchFound = true;
        } else {
            link.classList.remove('active');
        }
    });

    // 5. Fallback if no match was found (e.g., custom error page)
    if (!matchFound) {
        const homeLink = document.querySelector('.site-nav ul li a[href*="home"]');
        if (homeLink) {
            homeLink.classList.add('active');
        }
    }
});