document.addEventListener("DOMContentLoaded", () => {
    let currentSlide = 0;

    const wrapper = document.getElementById('sliderWrapper');
    const dots = document.querySelectorAll('.dot');

    // ✅ If slider is not on this page, don't run slider code
    if (!wrapper || dots.length === 0) {
        console.warn("Slider not initialized: wrapper or dots not found", { wrapper, dotsCount: dots.length });
        return;
    }

    const totalSlides = dots.length; // ✅ use actual dots count
    let slideInterval;

    function goToSlide(index) {
        // ✅ clamp index
        if (index < 0 || index >= totalSlides) return;

        console.log("Moving to slide index:", index);
        currentSlide = index;

        wrapper.style.transform = `translateX(-${currentSlide * 100}%)`;

        dots.forEach(dot => dot.classList.remove('active'));
        dots[currentSlide].classList.add('active');

        resetTimer();
    }

    dots.forEach((dot, index) => {
        dot.addEventListener('click', (event) => {
            event.preventDefault();
            console.log("Dot clicked:", index);
            goToSlide(index);
        });
    });

    function startTimer() {
        slideInterval = setInterval(() => {
            let nextSlide = (currentSlide + 1) % totalSlides;
            goToSlide(nextSlide);
        }, 5000);
    }

    function resetTimer() {
        clearInterval(slideInterval);
        startTimer();
    }

    startTimer();
});