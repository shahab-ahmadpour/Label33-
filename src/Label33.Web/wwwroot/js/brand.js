(() => {
  const intro = document.getElementById('site-intro');
  const skip = document.getElementById('intro-skip');
  const nav = document.getElementById('site-nav');
  const burger = document.getElementById('nav-burger');

  function finishIntro() {
    if (!intro) return;
    intro.classList.add('is-done');
    window.setTimeout(() => intro.remove(), 1000);
  }

  if (intro) {
    const navEntries = performance.getEntriesByType('navigation');
    const navType = navEntries.length ? navEntries[0].type : 'navigate';
    // Replay on full reload; skip replay only for back_forward within session flag
    if (navType === 'back_forward' && sessionStorage.getItem('label33.intro.played') === '1') {
      intro.remove();
    } else {
      sessionStorage.setItem('label33.intro.played', '1');
      window.setTimeout(finishIntro, 5600);
      skip?.addEventListener('click', finishIntro);
    }
  }

  burger?.addEventListener('click', () => nav?.classList.toggle('is-open'));
})();
