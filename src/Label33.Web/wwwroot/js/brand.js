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

  function easeInOutSine(x) {
    return -(Math.cos(Math.PI * x) - 1) / 2;
  }

  function getFrames(root) {
    return root ? Array.from(root.querySelectorAll('.diyar-flap__frame')) : [];
  }

  /**
   * Soft crossfade between neighboring flap frames.
   * phase: continuous index in [0, frameCount-1].
   */
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
      img.classList.toggle('is-active', op > 0.45);
    });
    root.dataset.frame = String(i + 1);
  }

  /**
   * Frames are ordered down → up (01 lowest … 04 = reference wings-up).
   * Fast downstroke (up→down), slower recovery (down→up).
   */
  function flapPhase(elapsedSec, hz, frameCount) {
    const n = Math.max(frameCount, 2);
    const max = n - 1;
    const linear = ((elapsedSec * hz) % 1 + 1) % 1;
    if (linear < 0.36) {
      const t = easeOutQuad(linear / 0.36);
      return lerp(max, 0, t); // up → down
    }
    const t = easeInOutSine((linear - 0.36) / 0.64);
    return lerp(0, max, t); // down → up
  }

  /** Calm mark flight: climb → soft soar → exit */
  function flightPose(t) {
    if (t < 0.36) {
      const u = easeInOutCubic(t / 0.36);
      return {
        x: -40 + u * 78,
        y: 56 - u * 32,
        flapHz: 1.9,
        bank: lerp(-12, -3, u),
        scale: lerp(0.84, 1.04, u),
      };
    }
    if (t < 0.6) {
      const u = (t - 0.36) / 0.24;
      const breath = Math.sin(u * Math.PI * 2) * 0.4;
      return {
        x: 38 + Math.sin(u * Math.PI) * 1.0,
        y: 24 + breath,
        flapHz: 0.85,
        bank: -1.2 + Math.sin(u * Math.PI * 2) * 1.4,
        scale: 1.06,
      };
    }
    const u = easeInOutCubic((t - 0.6) / 0.4);
    return {
      x: 38 + u * 70,
      y: 24 - u * 28,
      flapHz: lerp(1.55, 2.15, u),
      bank: lerp(2, 11, u),
      scale: lerp(1.03, 0.9, u),
    };
  }

  function runSpriteFlap(root, { hz = 1.45 } = {}) {
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
      setFlapBlend(root, flapPhase(elapsed, hz, frames.length));
      raf = requestAnimationFrame(tick);
    }

    raf = requestAnimationFrame(tick);
    return () => {
      stopped = true;
      cancelAnimationFrame(raf);
    };
  }

  function runIntroFlight(birdEl, onDone) {
    const root = birdEl.querySelector('[data-flap-root]') || birdEl.querySelector('.diyar-flap');
    const frames = getFrames(root);
    const frameCount = Math.max(frames.length, 1);
    const duration = 8200;
    const start = performance.now();
    let raf = 0;
    let stopped = false;
    let flapClock = 0;
    let lastNow = start;
    let smoothBank = -10;

    frames.forEach((img) => {
      img.style.opacity = '0';
    });

    function tick(now) {
      if (stopped) return;
      const dt = Math.min((now - lastNow) / 1000, 0.05);
      lastNow = now;

      const raw = clamp((now - start) / duration, 0, 1);
      const pose = flightPose(raw);

      flapClock += dt * pose.flapHz;
      const phase = flapPhase(flapClock, 1, frameCount);
      setFlapBlend(root, phase);

      // Lift on downstroke (phase moving toward frame 0)
      const max = Math.max(frameCount - 1, 1);
      const downAmount = 1 - phase / max; // 1 at down, 0 at up
      const lift = -downAmount * 1.05;

      smoothBank = lerp(smoothBank, pose.bank, 0.1);

      let opacity = 1;
      if (raw < 0.06) opacity = raw / 0.06;
      else if (raw > 0.9) opacity = (1 - raw) / 0.1;

      birdEl.style.left = `${pose.x}%`;
      birdEl.style.top = `${pose.y + lift}%`;
      birdEl.style.opacity = String(clamp(opacity, 0, 1));
      birdEl.style.transform = `translate(-50%, -50%) rotate(${smoothBank}deg) scale(${pose.scale})`;

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

  function idleFlaps(doc) {
    doc.querySelectorAll('.diyar-flap[data-flap="idle"]').forEach((el) => {
      runSpriteFlap(el, { hz: 1.35 });
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
        window.setTimeout(() => finishIntro(null), 8200);
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
