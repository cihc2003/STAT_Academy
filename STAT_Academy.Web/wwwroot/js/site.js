const navToggle = document.querySelector('.nav-toggle');
const navLinks = document.querySelector('.nav-links');

if (navToggle && navLinks) {
    navToggle.addEventListener('click', () => {
        const open = navLinks.classList.toggle('open');
        navToggle.setAttribute('aria-expanded', open.toString());
    });
}

const courseSearch = document.querySelector('#courseSearch');
const courses = document.querySelectorAll('[data-course]');

if (courseSearch && courses.length) {
    courseSearch.addEventListener('input', () => {
        const term = courseSearch.value.trim().toLowerCase();

        courses.forEach((course) => {
            const content = `${course.dataset.course} ${course.textContent}`.toLowerCase();
            course.style.display = content.includes(term) ? '' : 'none';
        });
    });
}

