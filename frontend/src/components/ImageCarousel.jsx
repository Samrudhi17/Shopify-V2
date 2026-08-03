// Horizontal image strip for product cards. Swiping is native scroll with CSS
// scroll-snap, so touch devices get it for free; the arrows exist for mouse
// users, and the dots show position and jump to a slide.
import { useCallback, useRef, useState } from "react";

export default function ImageCarousel({ images = [], alt = "", overlay = null, aspect = "1 / 1" }) {
  const trackRef = useRef(null);
  const [index, setIndex] = useState(0);

  const count = images.length;

  // Derive the active slide from scroll position rather than tracking it
  // separately, so dragging, arrows and dots can never disagree.
  const onScroll = useCallback(() => {
    const el = trackRef.current;
    if (!el) return;
    const width = el.clientWidth || 1;
    setIndex(Math.round(el.scrollLeft / width));
  }, []);

  const goTo = (i, e) => {
    // These sit inside a clickable card, so don't let the click navigate.
    e?.preventDefault();
    e?.stopPropagation();
    const el = trackRef.current;
    if (!el) return;
    const next = Math.max(0, Math.min(count - 1, i));
    el.scrollTo({ left: next * el.clientWidth, behavior: "smooth" });
  };

  if (count === 0) {
    return (
      <div className="carousel" style={{ aspectRatio: aspect }}>
        <div className="carousel-empty">No image</div>
        {overlay}
      </div>
    );
  }

  return (
    <div className="carousel" style={{ aspectRatio: aspect }}>
      <div className="carousel-track" ref={trackRef} onScroll={onScroll}>
        {images.map((src, i) => (
          <div className="carousel-slide" key={src + i}>
            <img src={src} alt={count > 1 ? `${alt} — image ${i + 1} of ${count}` : alt} loading="lazy" />
          </div>
        ))}
      </div>

      {count > 1 && (
        <>
          <button
            type="button" className="carousel-arrow carousel-prev"
            onClick={(e) => goTo(index - 1, e)} disabled={index === 0} aria-label="Previous image"
          >‹</button>
          <button
            type="button" className="carousel-arrow carousel-next"
            onClick={(e) => goTo(index + 1, e)} disabled={index === count - 1} aria-label="Next image"
          >›</button>

          <div className="carousel-dots">
            {images.map((src, i) => (
              <button
                type="button" key={src + i}
                className={"carousel-dot" + (i === index ? " is-active" : "")}
                onClick={(e) => goTo(i, e)}
                aria-label={`Go to image ${i + 1}`}
                aria-current={i === index}
              />
            ))}
          </div>

          <span className="carousel-count">{index + 1}/{count}</span>
        </>
      )}

      {overlay}
    </div>
  );
}
