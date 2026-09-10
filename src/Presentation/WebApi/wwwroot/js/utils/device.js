export function isMobileDevice() {
    return window.innerWidth <= 1024
        || /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini|Mobile/i.test(navigator.userAgent);
}
