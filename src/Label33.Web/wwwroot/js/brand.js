(() => {
  const intro = document.getElementById('site-intro');
  const skip = document.getElementById('intro-skip');
  const nav = document.getElementById('site-nav');
  const burger = document.getElementById('nav-burger');

  const FRAME_COUNT = 4;

  function clamp(v, a, b) {
    return Math.max(a, Math.min(b, v));
  }

  function easeInOutCubic(x) {
    return x < 0.5 ? 4 * x * x * x : 1 - Math.pow(-2 * x + 2, 3) / 2;
  }

  function getFrames(root) {
    return root ? Array.from(root.querySelectorAll('.diyar-flap__frame')) : [];
  }

  function setFlapFrame(root, frameIndex) {
    const frames = getFrames(root);
    if (!frames.length) return;
    const i = ((frameIndex % FRAME_COUNT) + FRAME_COUNT) % FRAME_COUNT;
    frames.forEach((img, idx) => {
      img.classList.toggle('is-active', idx === i);
    });
    root.dataset.frame = String(i + 1);
  }

  function runSpriteFlap(root, { fps = 6 } = {}) {
    if (!root) return () => {};
    let frame = 0;
    let last = performance.now();
    let raf = 0;
    let stopped = false;
    const interval = 1000 / fps;

    setFlapFrame(root, 0);

    function tick(now) {
      if (stopped) return;
      if (now - last >= interval) {
        last = now;
        frame = (frame + 1) % FRAME_COUNT;
        setFlapFrame(root, frame);
      }
      raf = requestAnimationFrame(tick);
    }

    raf = requestAnimationFrame(tick);
    return () => {
      stopped = true;
      cancelAnimationFrame(raf);
    };
  }

  /**
   * Path with a deliberate pause near center:
   * enter → hold → exit. Flap slows during the pause.
   */
  function flightProgress(t) {
    // t in 0..1 over full intro
    if (t < 0.30) {
      // approach center — higher altitude so bird clears the 33 mark
      const u = easeInOutCubic(t / 0.30);
      return { x: -48 + u * 86, y: 38 - u * 14, coast: false, phase: 'enter' };
    }
    if (t < 0.55) {
      // soft mid-screen pause with tiny hover (~2.2s), above the logo
      const u = (t - 0.30) / 0.25;
      const hover = Math.sin(u * Math.PI * 2) * 1.0;
      return { x: 38 + hover * 0.35, y: 24 + hover, coast: true, phase: 'pause' };
    }
    // exit higher toward top-right
    const u = easeInOutCubic((t - 0.55) / 0.45);
    return { x: 38 + u * 78, y: 24 - u * 18, coast: false, phase: 'exit' };
  }

  function runIntroFlight(birdEl, onDone) {
    const root = birdEl.querySelector('[data-flap-root]') || birdEl.querySelector('.diyar-flap');
    const duration = 9000;
    const start = performance.now();
    let raf = 0;
    let stopped = false;
    let frame = 0;
    let lastFrameAt = start;

    setFlapFrame(root, 0);

    function tick(now) {
      if (stopped) return;
      const raw = clamp((now - start) / duration, 0, 1);
      const path = flightProgress(raw);

      const frameInterval = path.coast ? 180 : 140;
      if (now - lastFrameAt >= frameInterval) {
        lastFrameAt = now;
        frame = (frame + 1) % FRAME_COUNT;
        setFlapFrame(root, frame);
      }

      const bob = frame === 2 ? -1.6 : frame === 0 ? 0.9 : -0.4;
      const tilt =
        path.phase === 'enter' ? -6 + raw * 10 :
        path.phase === 'pause' ? 2 + Math.sin(now / 280) * 1.5 :
        4 + (raw - 0.55) * 12;
      const scale = path.phase === 'pause' ? 1.06 : 0.92 + (path.phase === 'enter' ? raw * 0.2 : 0.12);

      let opacity = 1;
      if (raw < 0.05) opacity = raw / 0.05;
      else if (raw > 0.9) opacity = (1 - raw) / 0.1;

      birdEl.style.left = `${path.x}%`;
      birdEl.style.top = `${path.y + bob}%`;
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
      runSpriteFlap(el, { fps: 5 });
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
        window.setTimeout(() => finishIntro(null), 8500);
      }
      skip?.addEventListener('click', () => finishIntro(stopFlight));
    }
  }

  idleFlaps(document);
  burger?.addEventListener('click', () => nav?.classList.toggle('is-open'));
})();
