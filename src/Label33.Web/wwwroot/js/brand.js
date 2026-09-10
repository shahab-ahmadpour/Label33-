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

  /**
   * One wing group rotated around the shoulder.
   * Angle goes from raised (~-42°) to lowered (~+38°) — clearly visible flap.
   */
  function flapWings(svg, phase) {
    if (!svg) return;
    const pivot = svg.querySelector('.diyar-svg__wing-pivot');
    if (!pivot) return;

    // Asymmetric flap: faster downstroke feel via skewed sine
    const s = Math.sin(phase);
    const angle = s * 40; // -40° (up) … +40° (down)
    const squash = 1 - Math.abs(s) * 0.12; // foreshorten at mid-stroke

    pivot.setAttribute(
      'transform',
      `rotate(${angle.toFixed(2)} 348 185) translate(348 185) scale(1 ${squash.toFixed(3)}) translate(-348 -185)`
    );
  }

  function runIntroFlight(birdEl, onDone) {
    const svg = birdEl.querySelector('.diyar-svg');
    const duration = 5600;
    const start = performance.now();
    let phase = 0;
    let raf = 0;
    let stopped = false;

    function tick(now) {
      if (stopped) return;
      const raw = clamp((now - start) / duration, 0, 1);
      const e = easeInOutCubic(raw);

      // ~2.2 flaps/sec mid-flight, a bit slower at ends
      const flapSpeed = 0.28 + Math.sin(e * Math.PI) * 0.16;
      phase += flapSpeed;

      // Lift on downstroke (positive sin = wing down)
      const wingDown = (Math.sin(phase) + 1) / 2;
      const bob = (wingDown - 0.5) * -5.5;

      const x = -48 + e * 158;
      const y = 60 - e * 40 + bob;
      const tilt = -12 + e * 22 + Math.sin(phase) * 5;
      const scale = 0.86 + Math.sin(e * Math.PI) * 0.16;

      let opacity = 1;
      if (raw < 0.06) opacity = raw / 0.06;
      else if (raw > 0.9) opacity = (1 - raw) / 0.1;

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
      phase += 0.14;
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
        window.setTimeout(() => finishIntro(null), 6000);
      }
      skip?.addEventListener('click', () => finishIntro(stopFlight));
    }
  }

  idleFlap(document);
  burger?.addEventListener('click', () => nav?.classList.toggle('is-open'));
})();
