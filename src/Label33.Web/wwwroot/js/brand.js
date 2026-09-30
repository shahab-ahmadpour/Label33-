(() => {
  const intro = document.getElementById('site-intro');
  const skip = document.getElementById('intro-skip');
  const nav = document.getElementById('site-nav');
  const burger = document.getElementById('nav-burger');

  function clamp(v, a, b) {
    return Math.max(a, Math.min(b, v));
  }

  function lerp(a, b, t) {
    return a + (b - a) * t;
  }

  function easeInOutCubic(x) {
    return x < 0.5 ? 4 * x * x * x : 1 - Math.pow(-2 * x + 2, 3) / 2;
  }

  function easeOutQuad(x) {
    return 1 - (1 - x) * (1 - x);
  }

  /**
   * Unified-mark flight (v3):
   * One intact Diyar silhouette — no cut wing layers.
   * Motion comes from path, bank, lift pulse, and a soft whole-body beat.
   */
  function flightPose(t, now) {
    const beat = Math.sin(now / 180);          // ~5.5 Hz visual pulse is too fast — use slower below
    const slowBeat = Math.sin(now / 280);      // soft body “breath”
    const power = Math.max(0, Math.sin(now / 220)); // downstroke-like lift bias

    if (t < 0.38) {
      const u = easeInOutCubic(t / 0.38);
      return {
        x: -36 + u * 74,
        y: 62 - u * 38 + slowBeat * 0.6 - power * 1.2,
        bank: lerp(-10, -2, u) + slowBeat * 1.2,
        scale: lerp(0.86, 1.05, u) * (1 + power * 0.02),
        squash: 1 - power * 0.035,
      };
    }

    if (t < 0.62) {
      const u = (t - 0.38) / 0.24;
      return {
        x: 38 + Math.sin(u * Math.PI) * 1.6,
        y: 24 + slowBeat * 0.9 - power * 0.7,
        bank: -1 + slowBeat * 2.0,
        scale: 1.08 * (1 + power * 0.015),
        squash: 1 - power * 0.02,
      };
    }

    const u = easeInOutCubic((t - 0.62) / 0.38);
    return {
      x: 38 + u * 68,
      y: 24 - u * 34 + slowBeat * 0.5 - power * 1.0,
      bank: lerp(2, 14, u) + slowBeat * 1.0,
      scale: lerp(1.04, 0.88, u) * (1 + power * 0.02),
      squash: 1 - power * 0.03,
    };
  }

  function runIntroFlight(birdEl, onDone) {
    const mark = birdEl.querySelector('.diyar-intro-mark');
    const duration = 7600;
    const start = performance.now();
    let raf = 0;
    let stopped = false;
    let smoothBank = -8;

    function tick(now) {
      if (stopped) return;
      const raw = clamp((now - start) / duration, 0, 1);
      const pose = flightPose(raw, now);
      smoothBank = lerp(smoothBank, pose.bank, 0.12);

      let opacity = 1;
      if (raw < 0.07) opacity = easeOutQuad(raw / 0.07);
      else if (raw > 0.88) opacity = (1 - raw) / 0.12;

      birdEl.style.left = `${pose.x}%`;
      birdEl.style.top = `${pose.y}%`;
      birdEl.style.opacity = String(clamp(opacity, 0, 1));
      birdEl.style.transform = `translate(-50%, -50%) rotate(${smoothBank}deg) scale(${pose.scale})`;

      // Soft whole-body beat on the mark itself (keeps silhouette intact)
      if (mark) {
        mark.style.transform = `scaleY(${pose.squash})`;
      }

      if (raw < 1) {
        raf = requestAnimationFrame(tick);
      } else if (onDone) {
        onDone();
      }
    }

    raf = requestAnimationFrame(tick);
    return () => {
      stopped = true;
      cancelAnimationFrame(raf);
    };
  }

  // Legacy idle sprite support on Diyar page (if present)
  function getFrames(root) {
    return root ? Array.from(root.querySelectorAll('.diyar-flap__frame')) : [];
  }

  function setFlapBlend(root, phase) {
    const frames = getFrames(root);
    if (!frames.length) return;
    const n = frames.length;
    const max = n - 1;
    const p = clamp(phase, 0, max);
    const i = Math.floor(p);
    const j = Math.min(i + 1, max);
    const t = p - i;
    const s = t * t * (3 - 2 * t);
    frames.forEach((img, idx) => {
      let op = 0;
      if (idx === i && idx === j) op = 1;
      else if (idx === i) op = 1 - s;
      else if (idx === j) op = s;
      img.style.opacity = String(op);
    });
  }

  function runSpriteFlap(root, { hz = 1.2 } = {}) {
    const frames = getFrames(root);
    if (!root || frames.length <= 1) {
      if (frames[0]) frames[0].style.opacity = '1';
      return () => {};
    }
    let raf = 0;
    let stopped = false;
    const start = performance.now();
    frames.forEach((img) => {
      img.style.opacity = '0';
    });
    function tick(now) {
      if (stopped) return;
      const elapsed = (now - start) / 1000;
      const linear = (elapsed * hz) % 1;
      // ping-pong 0..max
      const max = frames.length - 1;
      const phase = linear < 0.5 ? (linear / 0.5) * max : (1 - (linear - 0.5) / 0.5) * max;
      setFlapBlend(root, phase);
      raf = requestAnimationFrame(tick);
    }
    raf = requestAnimationFrame(tick);
    return () => {
      stopped = true;
      cancelAnimationFrame(raf);
    };
  }

  function idleFlaps(doc) {
    doc.querySelectorAll('.diyar-flap[data-flap="idle"]').forEach((el) => {
      runSpriteFlap(el, { hz: 1.15 });
    });
  }

  function finishIntro(stopFlight) {
    if (!intro) return;
    stopFlight?.();
    intro.classList.add('is-done');
    window.setTimeout(() => intro.remove(), 1000);
  }

  if (intro) {
    const navEntries = performance.getEntriesByType('navigation');
    const navType = navEntries.length ? navEntries[0].type : 'navigate';
    const played = sessionStorage.getItem('label33.intro.played') === '1';
    const shouldPlay = navType === 'reload' || !played;
    if (!shouldPlay) {
      intro.remove();
    } else {
      sessionStorage.setItem('label33.intro.played', '1');
      const bird = intro.querySelector('.intro__bird');
      let stopFlight = null;
      if (bird) {
        stopFlight = runIntroFlight(bird, () => finishIntro(stopFlight));
      } else {
        window.setTimeout(() => finishIntro(null), 7600);
      }
      skip?.addEventListener('click', () => finishIntro(stopFlight));
    }
  }

  const searchToggle = document.getElementById('search-toggle');
  const searchPanel = document.getElementById('search-panel');
  const searchInput = document.getElementById('search-input');
  const searchClose = document.getElementById('search-close');

  function openSearch() {
    if (!searchPanel) return;
    searchPanel.classList.add('is-open');
    searchPanel.setAttribute('aria-hidden', 'false');
    window.setTimeout(() => searchInput?.focus(), 50);
  }

  function closeSearch() {
    if (!searchPanel) return;
    searchPanel.classList.remove('is-open');
    searchPanel.setAttribute('aria-hidden', 'true');
  }

  searchToggle?.addEventListener('click', (e) => {
    e.preventDefault();
    if (searchPanel?.classList.contains('is-open')) closeSearch();
    else openSearch();
  });
  searchClose?.addEventListener('click', closeSearch);
  searchPanel?.addEventListener('click', (e) => {
    if (e.target === searchPanel) closeSearch();
  });
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') closeSearch();
  });

  idleFlaps(document);
  burger?.addEventListener('click', () => nav?.classList.toggle('is-open'));
})();
