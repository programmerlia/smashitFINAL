
$(document).ready(function () {


    $('.hero-slider').slick({
        dots: true,
        arrows: false,
        infinite: true,
        speed: 1000,
        fade: true,
        cssEase: 'linear',
        autoplay: true,
        autoplaySpeed: 5000
    });

    $('.promos-slider').slick({
        dots: true,
        arrows: false,
        infinite: true,
        speed: 800,
        slidesToShow: 1,
        adaptiveHeight: true,
        autoplay: false
    });

    $('.back-to-top').click(function (e) {
        e.preventDefault();
        $('html, body').animate({ scrollTop: 0 }, 800);
    });
});


const adminBtn = document.getElementById('adminBtn');
const closeModal = document.getElementById('closeModal');

const navLinks = document.querySelectorAll('ul li a');


adminBtn.addEventListener('click', () => {
    adminModal.classList.remove('opacity-0', 'pointer-events-none');
    adminModal.classList.add('opacity-100', 'items-center');

    const modalContent = adminModal.querySelector('div');
    modalContent.classList.remove('translate-y-20');
    modalContent.classList.add('translate-y-0');
});

closeModal.addEventListener('click', () => {
    const modalContent = adminModal.querySelector('div');
    modalContent.classList.remove('translate-y-0');
    modalContent.classList.add('translate-y-20');

    adminModal.classList.add('opacity-0', 'pointer-events-none');
    adminModal.classList.remove('opacity-100', 'items-center');
});

adminModal.addEventListener('click', (e) => {
    if (e.target === adminModal) {
        closeModal.click();
    }
});

$(document).ready(function () {
    // Persistent Underline Logic
    var currentPath = window.location.pathname.toLowerCase();

    // Loop through each nav link
    $("#mainNav a").each(function () {
        var href = $(this).attr("href").toLowerCase();

        // Check if the current URL contains the link's destination
        if (currentPath.indexOf(href) > -1 && href !== "/") {
            $(this).addClass("active");
        }
        // Specific check for Home
        else if (currentPath.endsWith("home.aspx") && href.endsWith("home.aspx")) {
            $(this).addClass("active");
        }
    });
});