(() => {
  const intro = document.getElementById('site-intro');
  const skip = document.getElementById('intro-skip');
  const nav = document.getElementById('site-nav');
  const burger = document.getElementById('nav-burger');

  function clamp(v, a, b) {
    return Math.max(a, Math.min(b, v));
  }

  function easeInOutCubic(x) {
    return x < 0.5 ? 4 * x * x * x : 1 - Math.pow(-2 * x + 2, 3) / 2;
  }

  /** Crossfade + rotate wings so flap is obviously visible. */
  function flapWings(svg, phase) {
    if (!svg) return;
    const up = svg.querySelector('.diyar-svg__wing--up');
    const down = svg.querySelector('.diyar-svg__wing--down');
    if (!up || !down) return;

    // 0 = fully up, 1 = fully down
    const t = (Math.sin(phase) + 1) / 2;
    const upAngle = -38 + t * 52;
    const downAngle = -18 + t * 48;

    up.setAttribute('transform', `rotate(${upAngle} 348 182)`);
    down.setAttribute('transform', `rotate(${downAngle} 348 188)`);
    up.setAttribute('opacity', String((1 - t) * 0.92 + 0.08));
    down.setAttribute('opacity', String(t * 0.92 + 0.08));
  }

  function runIntroFlight(birdEl, onDone) {
    const svg = birdEl.querySelector('.diyar-svg');
    const duration = 5200;
    const start = performance.now();
    let phase = 0;
    let raf = 0;
    let stopped = false;

    birdEl.classList.add('is-js-flight');

    function tick(now) {
      if (stopped) return;
      const raw = clamp((now - start) / duration, 0, 1);
      const e = easeInOutCubic(raw);

      // Flap faster in the middle of the crossing
      const flapSpeed = 0.22 + Math.sin(e * Math.PI) * 0.18;
      phase += flapSpeed;

      // Vertical bob locked to downstroke (lift when wings go down)
      const downstroke = (Math.sin(phase) + 1) / 2;
      const bob = (downstroke - 0.5) * -4.2;

      const x = -42 + e * 148; // %
      const y = 58 - e * 38 + bob; // %
      const tilt = -10 + e * 20 + Math.sin(phase) * 4;
      const scale = 0.88 + Math.sin(e * Math.PI) * 0.14;

      let opacity = 1;
      if (raw < 0.07) opacity = raw / 0.07;
      else if (raw > 0.88) opacity = (1 - raw) / 0.12;

      birdEl.style.left = `${x}%`;
      birdEl.style.top = `${y}%`;
      birdEl.style.opacity = String(clamp(opacity, 0, 1));
      birdEl.style.transform = `translate(-50%, -50%) rotate(${tilt}deg) scale(${scale})`;

      flapWings(svg, phase);

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

  function idleFlap(root) {
    const svgs = root.querySelectorAll('.diyar-svg[data-flap="idle"]');
    if (!svgs.length) return;
    let phase = 0;
    function tick() {
      phase += 0.12;
      svgs.forEach((svg) => flapWings(svg, phase));
      requestAnimationFrame(tick);
    }
    requestAnimationFrame(tick);
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
        window.setTimeout(() => finishIntro(null), 5600);
      }
      skip?.addEventListener('click', () => finishIntro(stopFlight));
    }
  }

  idleFlap(document);
  burger?.addEventListener('click', () => nav?.classList.toggle('is-open'));
})();
