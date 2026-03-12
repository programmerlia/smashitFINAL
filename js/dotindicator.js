document.addEventListener("DOMContentLoaded", () => {
    let currentSlide = 0;
    const totalSlides = 2;
    const wrapper = document.getElementById('sliderWrapper');
    const dots = document.querySelectorAll('.dot');
    let slideInterval;

    function goToSlide(index) {
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