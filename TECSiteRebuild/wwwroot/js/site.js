// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function applyCursorRippleEffect(e) {
    const ripple = document.createElement("div");

    ripple.className = "ripple";
    document.body.appendChild(ripple);

    ripple.style.left = `${e.clientX}px`;
    ripple.style.top = `${e.clientY}px`;

    ripple.style.animation = "ripple-effect .4s  ease-out";
    ripple.onanimationend = () => document.body.removeChild(ripple);

}

const wrapper = document.getElementById("bubble-wrapper");
const animateBubble = e => {
    const x = e.clientX;

    const p = e.clientY / window.innerHeight * 100;
    const body = document.getElementById("main-body")

    body.style.backgroundPositionY = `${p}%`;

    const bubble = document.createElement("div");

    bubble.className = "bubble";

    bubble.style.left = `${x}px`;

    wrapper.appendChild(bubble);

    setTimeout(() => wrapper.removeChild(bubble), 2000);
}

const ButtonWait = () => {
    preventDefault();

    setTimeout(() => {
        return;
    }, 2000);
};

window.onclick = () => applyCursorRippleEffect(event);
window.onmousemove = e => animateBubble(e);
window.ontouchmove = e => animateBubble(e.touches[0]);
navigator.serviceWorker && navigator.serviceWorker.register('./sw.js').then(function(registration) {  console.log('Excellent, registered with scope: ', registration.scope);});