(() => {
  const intro = document.getElementById('site-intro');
  const skip = document.getElementById('intro-skip');
  const nav = document.getElementById('site-nav');
  const burger = document.getElementById('nav-burger');

  const FRAME_COUNT = 4;
  const FRAME_POSITIONS = ['0% 0', '33.333% 0', '66.666% 0', '100% 0'];

  function clamp(v, a, b) {
    return Math.max(a, Math.min(b, v));
  }

  function easeInOutCubic(x) {
    return x < 0.5 ? 4 * x * x * x : 1 - Math.pow(-2 * x + 2, 3) / 2;
  }

  function setFlapFrame(el, frameIndex) {
    if (!el) return;
    const i = ((frameIndex % FRAME_COUNT) + FRAME_COUNT) % FRAME_COUNT;
    el.style.backgroundPosition = FRAME_POSITIONS[i];
    el.dataset.frame = String(i + 1);
  }

  function runSpriteFlap(el, { fps = 8 } = {}) {
    if (!el) return () => {};
    let frame = 0;
    let last = performance.now();
    let raf = 0;
    let stopped = false;
    const interval = 1000 / fps;

    setFlapFrame(el, 0);

    function tick(now) {
      if (stopped) return;
      if (now - last >= interval) {
        last = now;
        frame = (frame + 1) % FRAME_COUNT;
        setFlapFrame(el, frame);
      }
      raf = requestAnimationFrame(tick);
    }

    raf = requestAnimationFrame(tick);
    return () => {
      stopped = true;
      cancelAnimationFrame(raf);
    };
  }

  function runIntroFlight(birdEl, onDone) {
    const sprite = birdEl.querySelector('.diyar-flap');
    const duration = 5600;
    const start = performance.now();
    let raf = 0;
    let stopped = false;
    let frame = 0;
    let lastFrameAt = start;

    setFlapFrame(sprite, 0);

    function tick(now) {
      if (stopped) return;
      const raw = clamp((now - start) / duration, 0, 1);
      const e = easeInOutCubic(raw);

      const frameInterval = 110 - Math.sin(e * Math.PI) * 40;
      if (now - lastFrameAt >= frameInterval) {
        lastFrameAt = now;
        frame = (frame + 1) % FRAME_COUNT;
        setFlapFrame(sprite, frame);
      }

      const bob = frame === 2 ? -3.2 : frame === 1 || frame === 3 ? -1.2 : 0.8;
      const x = -52 + e * 164;
      const y = 58 - e * 36 + bob;
      const tilt = -8 + e * 16 + (frame === 0 ? -3 : frame === 2 ? 4 : 0);
      const scale = 0.84 + Math.sin(e * Math.PI) * 0.18;

      let opacity = 1;
      if (raw < 0.06) opacity = raw / 0.06;
      else if (raw > 0.9) opacity = (1 - raw) / 0.1;

      birdEl.style.left = `${x}%`;
      birdEl.style.top = `${y}%`;
      birdEl.style.opacity = String(clamp(opacity, 0, 1));
      birdEl.style.transform = `translate(-50%, -50%) rotate(${tilt}deg) scale(${scale})`;

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
    root.querySelectorAll('.diyar-flap[data-flap="idle"]').forEach((el) => {
      runSpriteFlap(el, { fps: 8 });
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
    if (navType === 'back_forward' && sessionStorage.getItem('label33.intro.played') === '1') {
      intro.remove();
    } else {
      sessionStorage.setItem('label33.intro.played', '1');
      const bird = intro.querySelector('.intro__bird');
      let stopFlight = null;
      if (bird) {
        stopFlight = runIntroFlight(bird, () => finishIntro(stopFlight));
      } else {
        window.setTimeout(() => finishIntro(null), 6000);
      }
      skip?.addEventListener('click', () => finishIntro(stopFlight));
    }
  }

  idleFlaps(document);
  burger?.addEventListener('click', () => nav?.classList.toggle('is-open'));
})();
