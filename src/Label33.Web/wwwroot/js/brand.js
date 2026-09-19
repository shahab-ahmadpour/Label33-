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

  /**
   * Asymmetric wing beat — fast powerful downstroke, slower recovery.
   * Returns degrees for the wing layer around the shoulder pivot.
   */
  function wingStrokeAngle(phase01) {
    const p = ((phase01 % 1) + 1) % 1;
    if (p < 0.38) {
      const u = easeOutQuad(p / 0.38);
      return lerp(34, -36, u);
    }
    const u = easeInOutSine((p - 0.38) / 0.62);
    return lerp(-36, 34, u);
  }

  /**
   * Continuous glide path shaped for a logo mark, not a sprite character:
   * rise in → soft soar above the mark → peel away.
   */
  function flightPose(t) {
    if (t < 0.34) {
      const u = easeInOutCubic(t / 0.34);
      return {
        x: -42 + u * 80,
        y: 58 - u * 34,
        flapHz: 3.2,
        flapAmp: 1,
        bank: lerp(-18, -4, u),
        scale: lerp(0.78, 1.02, u),
        phase: 'enter',
      };
    }
    if (t < 0.58) {
      const u = (t - 0.34) / 0.24;
      const drift = Math.sin(u * Math.PI) * 1.4;
      const breath = Math.sin(u * Math.PI * 2) * 0.55;
      return {
        x: 38 + drift,
        y: 24 + breath,
        flapHz: 1.15,
        flapAmp: 0.28,
        bank: -2 + Math.sin(u * Math.PI * 2) * 2.2,
        scale: 1.05,
        phase: 'soar',
      };
    }
    const u = easeInOutCubic((t - 0.58) / 0.42);
    return {
      x: 38 + u * 72,
      y: 24 - u * 30,
      flapHz: lerp(2.4, 3.6, u),
      flapAmp: lerp(0.7, 1, u),
      bank: lerp(2, 16, u),
      scale: lerp(1.02, 0.88, u),
      phase: 'exit',
    };
  }

  function getWing(root) {
    return root?.querySelector?.('.diyar-fly__wing') || null;
  }

  function setWingAngle(root, deg) {
    const wing = getWing(root);
    if (!wing) return;
    wing.style.transform = `rotate(${deg}deg)`;
  }

  function runLiveFlap(root, { hz = 2.2, amp = 1 } = {}) {
    if (!root || !getWing(root)) return () => {};
    let raf = 0;
    let stopped = false;
    const start = performance.now();

    function tick(now) {
      if (stopped) return;
      const elapsed = (now - start) / 1000;
      const phase = elapsed * hz;
      setWingAngle(root, wingStrokeAngle(phase) * amp);
      raf = requestAnimationFrame(tick);
    }

    raf = requestAnimationFrame(tick);
    return () => {
      stopped = true;
      cancelAnimationFrame(raf);
    };
  }

  function runIntroFlight(birdEl, onDone) {
    const root = birdEl.querySelector('[data-fly-root]') || birdEl.querySelector('.diyar-fly');
    const duration = 7800;
    const start = performance.now();
    let raf = 0;
    let stopped = false;
    let prevX = null;
    let prevY = null;
    let smoothBank = -14;
    let flapClock = 0;
    let lastNow = start;

    function tick(now) {
      if (stopped) return;
      const dt = Math.min((now - lastNow) / 1000, 0.05);
      lastNow = now;

      const raw = clamp((now - start) / duration, 0, 1);
      const pose = flightPose(raw);

      flapClock += dt * pose.flapHz;
      const wingDeg = wingStrokeAngle(flapClock) * pose.flapAmp;
      setWingAngle(root, wingDeg);

      // Lift follows downstroke: body rises as wing sweeps down
      const stroke = ((flapClock % 1) + 1) % 1;
      const downForce = stroke < 0.38 ? easeOutQuad(stroke / 0.38) : 0;
      const lift = -downForce * 1.35 * pose.flapAmp;

      // Smooth bank toward travel direction
      if (prevX != null) {
        const vx = pose.x - prevX;
        const vy = pose.y - prevY;
        const travelBank = clamp(vx * 0.55 + vy * 0.35, -22, 22);
        smoothBank = lerp(smoothBank, pose.bank * 0.55 + travelBank * 0.45, 0.12);
      } else {
        smoothBank = pose.bank;
      }
      prevX = pose.x;
      prevY = pose.y;

      let opacity = 1;
      if (raw < 0.06) opacity = raw / 0.06;
      else if (raw > 0.88) opacity = (1 - raw) / 0.12;

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

  function idleFlaps(root) {
    root.querySelectorAll('.diyar-fly[data-flap="idle"]').forEach((el) => {
      runLiveFlap(el, { hz: 1.65, amp: 0.72 });
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
        window.setTimeout(() => finishIntro(null), 7800);
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
