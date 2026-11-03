/* Pointer capture keeps mouse, pen and touch releases on one transaction. */
window.ShopSellDrag = {
  attach({root, bench, area, zone, quote, canStart, onStart, onFinish}) {
    let drag = null;
    let suppressClickUntil = 0;
    const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)').matches;

    function contains(x, y) {
      const r = area.getBoundingClientRect();
      return x >= r.left && x <= r.right && y >= r.top && y <= r.bottom;
    }

    function position(x, y) {
      drag.x = x;
      drag.y = y;
      if (drag.ghost) {
        drag.ghost.style.left = x - drag.width / 2 + 'px';
        drag.ghost.style.top = y - drag.height / 2 + 'px';
      }
      const inside = contains(x, y);
      zone.classList.toggle('accepting', inside);
      const instruction = document.getElementById('sell-instruction');
      const message = inside ? '松开出售' : '拖到此处出售';
      if (instruction.textContent !== message) instruction.textContent = message;
    }

    function activate() {
      drag.active = true;
      onStart();
      root.classList.add('drag-selling');
      drag.source.classList.add('drag-source');
      const r = area.getBoundingClientRect();
      const parent = root.getBoundingClientRect();
      Object.assign(zone.style, {left:r.left-parent.left+'px', top:r.top-parent.top+'px', width:r.width+'px', height:r.height+'px'});
      document.getElementById('sell-role').textContent = drag.quote.name + ' · ' + drag.quote.star + '星';
      document.getElementById('sell-price').textContent = '+' + drag.quote.value;
      zone.hidden = false;
      const ghost = drag.source.cloneNode(true);
      ghost.removeAttribute('id');
      ghost.removeAttribute('data-instance');
      ghost.removeAttribute('aria-label');
      ghost.setAttribute('aria-hidden', 'true');
      ghost.disabled = true;
      ghost.tabIndex = -1;
      ghost.className = 'bench-slot drag-piece';
      ghost.dataset.sale = '+' + drag.quote.value + ' 源石';
      drag.width = Math.min(drag.origin.width, 155);
      drag.height = drag.origin.height;
      Object.assign(ghost.style, {width:drag.width+'px', height:drag.height+'px'});
      document.body.appendChild(ghost);
      drag.ghost = ghost;
    }

    function finish(commit = false) {
      if (!drag) return false;
      const done = drag;
      drag = null; // Clear first: releasing capture may dispatch lostpointercapture.
      if (done.source.hasPointerCapture(done.pointerId)) done.source.releasePointerCapture(done.pointerId);
      done.source.classList.remove('drag-source');
      root.classList.remove('drag-selling');
      zone.hidden = true;
      zone.classList.remove('accepting');
      if (!done.active) return false;
      suppressClickUntil = performance.now() + 350;
      if (!commit && done.ghost && !reducedMotion) {
        const current = done.ghost.getBoundingClientRect();
        const dx = done.origin.left + done.origin.width/2 - (current.left + current.width/2);
        const dy = done.origin.top + done.origin.height/2 - (current.top + current.height/2);
        done.ghost.animate([{transform:'translate(0,0) scale(1.06)',opacity:.9},{transform:`translate(${dx}px,${dy}px) scale(1)`,opacity:0}], {duration:170,easing:'ease-out'}).finished.finally(()=>done.ghost.remove());
      } else if (done.ghost) {
        done.ghost.remove();
      }
      onFinish({instance:done.instance, commit, x:done.x, y:done.y});
      return true;
    }

    bench.addEventListener('pointerdown', e => {
      if (drag || !canStart() || !e.isPrimary || e.button !== 0) return;
      const source = e.target.closest('.bench-slot[data-instance]');
      if (!source || source.disabled) return;
      const instance = Number(source.dataset.instance);
      const sale = quote(instance);
      if (!sale.ok) return;
      drag = {pointerId:e.pointerId, instance, quote:sale, source, origin:source.getBoundingClientRect(), startX:e.clientX, startY:e.clientY, x:e.clientX, y:e.clientY, active:false, ghost:null};
      source.setPointerCapture(e.pointerId);
    });

    bench.addEventListener('pointermove', e => {
      if (!drag || e.pointerId !== drag.pointerId) return;
      if (!drag.active && Math.hypot(e.clientX-drag.startX, e.clientY-drag.startY) < 8) return;
      if (!drag.active) activate();
      e.preventDefault();
      position(e.clientX, e.clientY);
    });

    bench.addEventListener('pointerup', e => {
      if (!drag || e.pointerId !== drag.pointerId) return;
      if (drag.active) {
        position(e.clientX, e.clientY);
        e.preventDefault();
      }
      finish(drag.active && contains(e.clientX, e.clientY));
    });
    bench.addEventListener('pointercancel', e => {if (drag && e.pointerId === drag.pointerId) finish(false);});
    bench.addEventListener('lostpointercapture', e => {if (drag && e.pointerId === drag.pointerId) finish(false);});
    bench.addEventListener('dragstart', e => e.preventDefault());
    root.addEventListener('click', e => {
      if (performance.now() < suppressClickUntil) {e.preventDefault();e.stopImmediatePropagation();}
    }, true);
    window.addEventListener('blur', () => finish(false));
    window.addEventListener('resize', () => finish(false));
    document.addEventListener('visibilitychange', () => {if (document.hidden) finish(false);});
    document.addEventListener('keydown', e => {
      if (e.key === 'Escape' && drag) {finish(false);e.preventDefault();e.stopImmediatePropagation();}
    }, true);
    return {cancel:()=>finish(false), active:()=>!!drag?.active};
  }
};
