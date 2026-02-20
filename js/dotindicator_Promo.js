document.addEventListener("DOMContentLoaded", () => {
    const promoWrapper = document.getElementById('promoSliderWrapper');
    const slides = document.querySelectorAll('.promo-slide');
    const dotsContainer = document.getElementById('promoDotsContainer');
    let currentPromo = 0;
    const totalPromos = slides.length;
    let promoInterval;

    if (totalPromos === 0) return;

    // 1. Generate dots dynamically based on number of announcements
    for (let i = 0; i < totalPromos; i++) {
        const dot = document.createElement('button');
        dot.type = "button";
        dot.classList.add('dot_promo');
        if (i === 0) dot.classList.add('active');
        dot.addEventListener('click', (e) => {
            e.preventDefault();
            goToPromo(i);
        });
        dotsContainer.appendChild(dot);
    }

    const promoDots = document.querySelectorAll('.dot_promo');

    function goToPromo(index) {
        currentPromo = index;
        promoWrapper.style.transform = `translateX(-${currentPromo * 100}%)`;

        promoDots.forEach((dot, idx) => {
            dot.classList.toggle('active', idx === currentPromo);
        });
        resetPromoTimer();
    }

    function startPromoTimer() {
        if (totalPromos > 1) {
            promoInterval = setInterval(() => {
                currentPromo = (currentPromo + 1) % totalPromos;
                goToPromo(currentPromo);
            }, 5000);
        }
    }

    function resetPromoTimer() {
        clearInterval(promoInterval);
        startPromoTimer();
    }

    startPromoTimer();
});